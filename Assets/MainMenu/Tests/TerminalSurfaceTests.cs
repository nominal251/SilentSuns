using System.Collections;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SilentSuns.Menu.Tests
{
    public partial class TerminalMenuTests
    {
        private static Component SurfacePlayer() => (Component)Object.FindFirstObjectByType(GameplayType("PlayerMovement"));
        private static Component SurfaceMap() => (Component)Object.FindFirstObjectByType(GameplayType("Minimap"));
        private static float SuitRange(Component player) => (float)ReadMember(player, "OxygenRemaining");
        private static void HoldPlayer(Component player)
        {
            var body = player.GetComponent<Rigidbody>();
            // These tests move to exact positions; do not let physics move them back between steps.
            if (!body.isKinematic) body.linearVelocity = Vector3.zero;
            body.isKinematic = true;
        }
        private IEnumerator LoadSurface(string scene = "ObjectiveCheck")
        {
            yield return SceneManager.LoadSceneAsync(scene);
            yield return null;
            yield return null;
            Assert.That(TerminalGameSession.Manager.Phase, Is.EqualTo(GameManager.GamePhase.PlanetaryExploration));
            Assert.That(SurfacePlayer(), Is.Not.Null);
            Assert.That(SurfaceMap(), Is.Not.Null);
            HoldPlayer(SurfacePlayer());
        }

        [UnityTest, Category("Menu"), Category("Surface")]
        public IEnumerator MinimapStaysOutOfBootAndShipMenus()
        {
            var map = SurfaceMap();
            Assert.That(map.GetComponent<Canvas>().enabled, Is.False);
            Assert.That(map.GetComponentsInChildren<Camera>(true), Is.Empty);
            float oxygen = TerminalGameSession.Manager.Resources.OxygenSeconds;
            yield return ReadyMain();
            Click("Main Menu/Begin Survey");
            yield return WaitForTransition();
            Assert.That(map.GetComponent<Canvas>().enabled, Is.False, "Ship movement has no on-foot minimap.");
            Assert.That(TerminalGameSession.Manager.Resources.OxygenSeconds, Is.EqualTo(oxygen));
            var pause = Object.FindFirstObjectByType<TerminalPause>();
            pause.Pause();
            yield return null;
            Assert.That(map.GetComponent<Canvas>().enabled, Is.False);
        }

        [UnityTest, Category("Menu"), Category("Surface")]
        public IEnumerator SurfaceMapHidesForSettingsAndCalibration()
        {
            yield return LoadSurface();
            var map = SurfaceMap();
            Assert.That(map.GetComponent<Canvas>().enabled, Is.True);
            Assert.That(Camera.main.name, Is.EqualTo("Player Camera"));
            Assert.That(Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Count(c => c.name == "MinimapCamera"), Is.Zero);
            Assert.That(map.GetComponentsInChildren<Graphic>(true).All(g => !g.raycastTarget ||
                g.GetComponentInParent<CanvasGroup>()?.blocksRaycasts == false), Is.True,
                "Navigation and any generated text meshes must let clicks through.");
            Assert.That(map.GetComponent<GraphicRaycaster>(), Is.Null);
            Assert.That(map.GetComponentInChildren<RectMask2D>(), Is.Null);
            GamePreferences.SetOption("SafeArea", .12f);
            yield return null;
            var image = (RawImage)ReadMember(map, "mapImage");
            Assert.That(image.texture, Is.Null, "Navigation must not render terrain.");
            var corners = new Vector3[4];
            image.rectTransform.GetWorldCorners(corners);
            Assert.That(corners.All(c => c.x >= 0 && c.x <= Screen.width && c.y >= 0 && c.y <= Screen.height), Is.True);
            Object.Destroy(CaptureGameplay("surface-minimap"));
            var pause = Object.FindFirstObjectByType<TerminalPause>();
            pause.Pause();
            yield return new WaitForSecondsRealtime(.3f);
            Assert.That(map.GetComponent<Canvas>().enabled, Is.False);
            Assert.That(map.GetComponentsInChildren<Camera>(true), Is.Empty);
            pause.OpenSettings();
            yield return null;
            TerminalCalibration.Display();
            yield return new WaitForSecondsRealtime(.2f);
            var slider = TerminalModal.Current.GetComponentsInChildren<Slider>().First();
            yield return DragModalSlider(slider);
            Assert.That(map.GetComponent<Canvas>().enabled, Is.False);
            Object.Destroy(CaptureGameplay("surface-calibration"));
            TerminalModal.Current.Close();
            Object.FindFirstObjectByType<TerminalSettings>().DiscardChanges();
            pause.CloseSettings();
            pause.Resume();
            yield return new WaitForSecondsRealtime(.4f);
            Assert.That(map.GetComponent<Canvas>().enabled, Is.True);
        }

        [UnityTest, Category("Menu"), Category("Surface")]
        public IEnumerator NavigationUsesHeadingAndCircularEdgesWithoutChangingObjectives()
        {
            yield return LoadSurface();
            var player = SurfacePlayer();
            // Control the test pose without the movement script turning it back.
            ((Behaviour)player).enabled = false;
            player.transform.rotation = Quaternion.identity;
            var system = (Component)Object.FindFirstObjectByType(GameplayType("ObjectiveSystem"));
            var objectives = SurveyObjectives(system);
            objectives[0].transform.position = player.transform.position + new Vector3(100, 0, 100);
            objectives[1].transform.position = player.transform.position + Vector3.forward * 12;
            var map = SurfaceMap();
            var destination = new GameObject("Test return point");
            destination.transform.position = player.transform.position + Vector3.left * 90;
            Call(map, "SetSources", player, system, destination.transform);
            yield return null;
            yield return null;
            var markers = map.GetComponentsInChildren<Image>().Where(i => i.name.StartsWith("ObjectiveIcon_")).ToArray();
            Assert.That(markers.Length, Is.EqualTo(2));
            Assert.That(markers.All(i => Vector2.Distance(i.rectTransform.anchoredPosition, new Vector2(0, 10)) <= 152.01f), Is.True);
            var far = markers.OrderByDescending(i => i.rectTransform.anchoredPosition.x).First();
            Assert.That(far.rectTransform.anchoredPosition.x, Is.GreaterThan(40));
            Assert.That(((TMP_Text)ReadMember(map, "targetDetail")).text, Is.EqualTo("12 m"));
            Assert.That(((TMP_Text)ReadMember(map, "returnText")).text, Is.EqualTo("RETURN  90m"));
            foreach (var compass in map.GetComponentsInChildren<TMP_Text>().Where(t => t.name.StartsWith("Compass ")))
            {
                compass.ForceMeshUpdate();
                var glyph = compass.textInfo.characterInfo[0];
                Vector3 visibleCenter = (glyph.vertex_BL.position + glyph.vertex_TR.position) * .5f;
                Assert.That(visibleCenter.magnitude, Is.LessThan(.1f), compass.name + " must center the visible letter on its bearing.");
            }
            Object.Destroy(CaptureGameplay("navigation-north-aligned"));
            player.transform.rotation = Quaternion.Euler(0, 90, 0);
            yield return null;
            yield return null;
            Assert.That(far.rectTransform.anchoredPosition.x, Is.LessThan(-40));
            var north = map.GetComponentsInChildren<TMP_Text>().First(t => t.name == "Compass N");
            Assert.That(north.rectTransform.anchoredPosition.x, Is.LessThan(-160));
            Assert.That(objectives.All(o => !(bool)ReadMember(o, "IsComplete")), Is.True);
            Object.Destroy(CaptureGameplay("navigation-objectives"));
            Object.Destroy(destination);
        }

        [UnityTest, Category("Menu"), Category("Surface")]
        public IEnumerator NavigationMarkerStylesDoNotChangeObjectiveProgress()
        {
            yield return LoadSurface();
            var player = SurfacePlayer();
            ((Behaviour)player).enabled = false;
            player.transform.rotation = Quaternion.identity;
            var map = SurfaceMap();
            var system = (Component)Object.FindFirstObjectByType(GameplayType("ObjectiveSystem"));
            var objectives = SurveyObjectives(system);
            objectives[0].transform.position = player.transform.position + new Vector3(-20, 0, 10);
            objectives[1].transform.position = player.transform.position + new Vector3(18, 0, -15);
            var styles = (IList)ReadMember(map, "markerStyles");
            var styleType = map.GetType().GetNestedType("MarkerStyle");
            var kindType = map.GetType().GetNestedType("MarkerKind");
            for (int i = 0; i < 2; i++)
            {
                var style = System.Activator.CreateInstance(styleType);
                styleType.GetField("objective").SetValue(style, objectives[i]);
                styleType.GetField("kind").SetValue(style, System.Enum.Parse(kindType, i == 0 ? "Anomaly" : "Signal"));
                styles.Add(style);
            }
            yield return null;
            yield return null;
            var markers = map.GetComponentsInChildren<Image>().Where(i => i.name.StartsWith("ObjectiveIcon_")).ToArray();
            Assert.That(markers.Any(i => i.sprite == (Sprite)ReadMember(map, "diamond") && i.color.r > .8f && i.color.g < .3f), Is.True);
            Assert.That(markers.Any(i => i.sprite == (Sprite)ReadMember(map, "signal")), Is.True);
            Assert.That(objectives.All(o => !(bool)ReadMember(o, "IsComplete")), Is.True);
            Object.Destroy(CaptureGameplay("navigation-marker-types"));
            styles.Clear();
            yield return null;
            Assert.That(markers.All(i => i.sprite == null && i.color.g > .6f), Is.True);
        }

        [UnityTest, Category("Menu"), Category("Surface")]
        public IEnumerator OxygenDialShowsFullHalfLowAndCriticalSupply()
        {
            yield return LoadSurface();
            var player = SurfacePlayer();
            player.GetComponent<Rigidbody>().detectCollisions = false;
            ((Behaviour)player).enabled = false;
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            var map = SurfaceMap();
            var resources = TerminalGameSession.Manager.Resources;
            var needle = (RectTransform)ReadMember(map, "oxygenNeedle");
            foreach (float fraction in new[] { 1f, .5f, .25f, .1f })
            {
                resources.AddOxygen(resources.OxygenCapacity - resources.OxygenSeconds);
                resources.DrainOxygen(resources.OxygenCapacity * (1 - fraction));
                yield return null;
                yield return null;
                Assert.That(Mathf.DeltaAngle(needle.localEulerAngles.z, 135 - fraction * 270), Is.EqualTo(0).Within(.2f));
                Assert.That(((TMP_Text)ReadMember(map, "oxygenText")).text, Is.EqualTo($"{fraction * 100:0}%"));
                Assert.That(((TMP_Text)ReadMember(map, "oxygenState")).text,
                    Is.EqualTo(fraction <= .15f ? "CRITICAL" : fraction <= .3f ? "LOW O2" : ""));
                Assert.That(resources.OxygenSeconds, Is.EqualTo(resources.OxygenCapacity * fraction).Within(.01f));
                Object.Destroy(CaptureGameplay("navigation-oxygen-" + Mathf.RoundToInt(fraction * 100)));
            }
        }

        [UnityTest, Category("Menu"), Category("Surface")]
        public IEnumerator TravelSpendsOxygenOnceAndPauseDoesNotCatchUp()
        {
            yield return LoadSurface();
            var player = SurfacePlayer();
            var body = player.GetComponent<Rigidbody>();
            var resources = TerminalGameSession.Manager.Resources;
            // Keep this exact-distance check away from collision correction and refill triggers.
            body.detectCollisions = false;
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            float range = SuitRange(player);
            float before = resources.OxygenSeconds;
            body.position += Vector3.right * 5;
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Assert.That(SuitRange(player), Is.EqualTo(range - 5).Within(.02f));
            Assert.That(resources.OxygenSeconds, Is.EqualTo(before - before * 5 / range).Within(.05f));
            var pause = Object.FindFirstObjectByType<TerminalPause>();
            pause.Pause();
            before = resources.OxygenSeconds;
            body.position += Vector3.forward * 10;
            yield return null;
            yield return null;
            pause.Resume();
            yield return new WaitForSecondsRealtime(.4f);
            Assert.That(resources.OxygenSeconds, Is.EqualTo(before).Within(.05f));
            Call(player, "SetGravity", false);
            body.position += Vector3.right * 10;
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Assert.That(resources.OxygenSeconds, Is.EqualTo(before).Within(.05f));
            Assert.That(SurfaceMap().GetComponent<Canvas>().enabled, Is.False);
        }

        [UnityTest, Category("Menu"), Category("Surface")]
        public IEnumerator RefillTriggersUpdateTheSameOxygenAndDial()
        {
            yield return LoadSurface();
            var player = SurfacePlayer();
            var body = player.GetComponent<Rigidbody>();
            var resources = TerminalGameSession.Manager.Resources;
            var stations = Object.FindObjectsByType(GameplayType("ResetOxygen"), FindObjectsSortMode.None).Cast<Component>().ToArray();
            Assert.That(stations.Length, Is.EqualTo(1));
            var secondStation = Object.Instantiate(stations[0].gameObject);
            secondStation.transform.position += Vector3.right * 20;
            stations = new[] { stations[0], secondStation.GetComponent(GameplayType("ResetOxygen")) };
            resources.DrainOxygen(resources.OxygenCapacity * .5f);
            float before = resources.OxygenSeconds;
            // Real trigger overlap, using the actual player collider.
            Call(player, "SetGravity", false);
            body.position = stations[0].GetComponent<Collider>().bounds.center;
            Call(player, "SetGravity", true);
            HoldPlayer(player);
            for (int i = 0; i < 8; i++) yield return new WaitForFixedUpdate();
            yield return null;
            Assert.That(resources.OxygenSeconds, Is.GreaterThan(before).And.LessThanOrEqualTo(resources.OxygenCapacity));
            Assert.That(ReadMember(player, "CurrentOxygenStation"), Is.EqualTo(stations[0].transform));
            var map = SurfaceMap();
            var needle = (RectTransform)ReadMember(map, "oxygenNeedle");
            float fraction = resources.OxygenSeconds / resources.OxygenCapacity;
            Assert.That(Mathf.DeltaAngle(needle.localEulerAngles.z, 135 - fraction * 270), Is.EqualTo(0).Within(.2f));
            Assert.That(((Image)ReadMember(map, "returnImage")).gameObject.activeSelf, Is.True);
            Call(player, "SetGravity", false);
            body.position = stations[1].GetComponent<Collider>().bounds.center;
            Call(player, "SetGravity", true);
            HoldPlayer(player);
            for (int i = 0; i < 4; i++) yield return new WaitForFixedUpdate();
            Assert.That(ReadMember(player, "CurrentOxygenStation"), Is.EqualTo(stations[1].transform));
            resources.AddOxygen(resources.OxygenCapacity - resources.OxygenSeconds);
            for (int i = 0; i < 4; i++) yield return new WaitForFixedUpdate();
            Assert.That(resources.OxygenSeconds, Is.EqualTo(resources.OxygenCapacity).Within(.01f));
            var pause = Object.FindFirstObjectByType<TerminalPause>();
            pause.Pause();
            resources.DrainOxygen(10);
            before = resources.OxygenSeconds;
            Call(player, "ResetDistance");
            Assert.That(resources.OxygenSeconds, Is.EqualTo(before));
            var visitor = new GameObject("Player Body", typeof(BoxCollider));
            stations[0].SendMessage("OnTriggerStay", visitor.GetComponent<Collider>());
            Object.Destroy(visitor);
            Assert.That(resources.OxygenSeconds, Is.EqualTo(before));
        }

        [UnityTest, Category("Menu"), Category("Surface")]
        public IEnumerator MapMarkersFollowCompletionRemovalAndReload()
        {
            yield return LoadSurface();
            var map = SurfaceMap();
            var system = (Component)Object.FindFirstObjectByType(GameplayType("ObjectiveSystem"));
            var objectives = SurveyObjectives(system);
            Assert.That(objectives.Length, Is.EqualTo(2), "Both example scripts must load, including the renamed one.");
            for (int phase = 0; phase < 10 && !(bool)ReadMember(objectives[0], "IsComplete"); phase++)
                Call(objectives[0], "AddProgress", 100);
            Assert.That(ReadMember(objectives[0], "IsComplete"), Is.True);
            yield return null;
            yield return null;
            Assert.That(map.GetComponentsInChildren<Image>().Count(i => i.name.StartsWith("ObjectiveIcon_") && i.gameObject.activeInHierarchy), Is.EqualTo(2));
            Assert.That(map.GetComponentsInChildren<Image>().Count(i => i.name.StartsWith("ObjectiveIcon_") && i.color.a < .8f), Is.EqualTo(1));
            Object.Destroy(objectives[1].gameObject);
            yield return null;
            var replacement = new GameObject("Replacement objective");
            var objective = replacement.AddComponent(GameplayType("CollectItemsObjective"));
            Call(system, "Register", objective);
            yield return null;
            yield return null;
            Assert.That(map.GetComponentsInChildren<Image>(true).Count(i => i.name.StartsWith("ObjectiveIcon_")), Is.EqualTo(2));
            var oldFace = (Sprite)ReadMember(map, "disc");
            var oldTexture = oldFace.texture;
            Object.Destroy(SurfacePlayer().gameObject);
            yield return null;
            yield return null;
            Assert.That(map.GetComponent<Canvas>().enabled, Is.False);
            yield return LoadSurface();
            Assert.That(oldTexture == null && oldFace == null, Is.True);
            Assert.That(Object.FindObjectsByType(GameplayType("Minimap"), FindObjectsSortMode.None).Length, Is.EqualTo(1));
        }

        [UnityTest, Category("Menu"), Category("Surface")]
        public IEnumerator SurfaceOxygenFailureRestartsTheSurface()
        {
            yield return LoadSurface();
            var player = SurfacePlayer();
            player.GetComponent<Rigidbody>().position += Vector3.right * (SuitRange(player) + 1);
            float deadline = Time.realtimeSinceStartup + 10;
            while (SceneManager.GetActiveScene().name != "GameOverScene" && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("GameOverScene"));
            Assert.That(TerminalGameSession.Manager.Resources.OxygenSeconds, Is.Zero);
            Assert.That(Object.FindObjectsByType(GameplayType("Minimap"), FindObjectsSortMode.None).Length, Is.Zero);
            yield return WaitForFailureChoices();
            var restart = Object.FindObjectsByType<TerminalButton>(FindObjectsSortMode.None).First(b => b.name.Contains("RESTART SURVEY"));
            restart.onClick.Invoke();
            yield return new WaitForSecondsRealtime(2.5f);
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("ObjectiveCheck"));
            Assert.That(TerminalGameSession.Manager.Phase, Is.EqualTo(GameManager.GamePhase.PlanetaryExploration));
            Assert.That(TerminalShipIntro.IsActive, Is.False);
            Assert.That(TerminalGameSession.Manager.Resources.OxygenSeconds, Is.GreaterThan(299));
            var station = (Transform)ReadMember(SurfacePlayer(), "CurrentOxygenStation");
            Assert.That(station == null || station.gameObject.scene == SceneManager.GetActiveScene(), Is.True,
                "Any refill contact after restart must belong to the new scene.");
        }

        [UnityTest, Category("Menu"), Category("Surface")]
        public IEnumerator SurfaceNewRunClearsOxygenStationAndObjectives()
        {
            yield return LoadSurface();
            var player = SurfacePlayer();
            var system = (Component)Object.FindFirstObjectByType(GameplayType("ObjectiveSystem"));
            Call(player, "SetOxygenStation", player.transform);
            Call(SurveyObjectives(system)[0], "AddProgress", 100);
            TerminalGameSession.Manager.Resources.DrainOxygen(100);
            TerminalGameSession.StartNewRun();
            Assert.That(ReadMember(player, "CurrentOxygenStation"), Is.Null);
            Assert.That(TerminalGameSession.Manager.Resources.OxygenSeconds, Is.EqualTo(300));
            Assert.That(SurveyObjectives(system).All(o => !(bool)ReadMember(o, "IsComplete")), Is.True);
            yield return null;
            Assert.That(SurfaceMap().GetComponent<Canvas>().enabled, Is.False);
            TerminalGameSession.ShipReady();
            TerminalGameSession.Manager.EnterPlanetaryExploration();
            yield return null;
            yield return null;
            Assert.That(SurfaceMap().GetComponent<Canvas>().enabled, Is.True);
        }

        [UnityTest, Category("Menu"), Category("Surface")]
        public IEnumerator PlanetSurfaceSceneWorksWithoutAnObjectiveList()
        {
            yield return LoadSurface("PlanetSurfaceTest");
            Assert.That(Object.FindFirstObjectByType(GameplayType("ObjectiveSystem")), Is.Null);
            Assert.That(SurfaceMap().GetComponent<Canvas>().enabled, Is.True);
            Assert.That(Camera.main.name, Is.EqualTo("Player Camera"));
            Assert.That(Object.FindFirstObjectByType<Terrain>().gameObject.layer, Is.EqualTo(LayerMask.NameToLayer("Geometry")));
            var pause = Object.FindFirstObjectByType<TerminalPause>();
            pause.Pause();
            yield return null;
            Assert.That(SurfaceMap().GetComponent<Canvas>().enabled, Is.False);
            Object.Destroy(CaptureGameplay("surface-terrain-pause"));
        }
    }
}
