using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SilentSuns.Menu.Tests
{
    public partial class TerminalMenuTests
    {
        private static void ChooseTracker(Component map, string mode) => Call(map, "SelectTracker",
            System.Enum.Parse(map.GetType().GetNestedType("TrackerMode"), mode));

        private static void StyleTracker(Component map, Component objective, string kind)
        {
            var styleType = map.GetType().GetNestedType("MarkerStyle");
            var style = System.Activator.CreateInstance(styleType);
            styleType.GetField("objective").SetValue(style, objective);
            styleType.GetField("kind").SetValue(style, System.Enum.Parse(map.GetType().GetNestedType("MarkerKind"), kind));
            ((IList)ReadMember(map, "markerStyles")).Add(style);
        }

        [UnityTest, Category("Menu"), Category("Surface"), Category("Tracker")]
        public IEnumerator TrackerRowsReceiveClicksAndPauseRestoresGameplayInput()
        {
            yield return LoadSurface();
            var map = SurfaceMap();
            var player = SurfacePlayer();
            var system = (Component)Object.FindFirstObjectByType(GameplayType("ObjectiveSystem"));
            StyleTracker(map, SurveyObjectives(system)[0], "Anomaly");
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            yield return Press(Key.M);
            // An unfocused batch editor may refuse cursor locking. Restore what it actually had.
            var savedLock = (CursorLockMode)typeof(TerminalToolInput).GetField("previousLock",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic).GetValue(null);
            yield return new WaitForSecondsRealtime(.2f);
            Assert.That((bool)ReadMember(map, "TrackerOpen"), Is.True);
            Assert.That(Cursor.lockState, Is.EqualTo(CursorLockMode.None));
            Assert.That(Cursor.visible, Is.True);
            Assert.That(Time.timeScale, Is.EqualTo(1));
            Assert.That(map.GetComponent<Canvas>().enabled, Is.True);
            Assert.That(TerminalToolInput.BlocksInput, Is.True);
            player.GetComponent<Rigidbody>().detectCollisions = false;
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            float oxygenBefore = SuitRange(player);
            player.GetComponent<Rigidbody>().position += Vector3.forward * 2;
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Assert.That(SuitRange(player), Is.EqualTo(oxygenBefore - 2).Within(.03f),
                "The selector holds controls, not world movement or its oxygen cost.");
            var panel = (GameObject)ReadMember(map, "trackerPanel");
            var buttons = panel.GetComponentsInChildren<Button>();
            Assert.That(buttons.Length, Is.EqualTo(4));
            Assert.That(panel.GetComponent<RectTransform>().rect.size, Is.EqualTo(new Vector2(640, 480)));
            foreach (var label in panel.GetComponentsInChildren<TMP_Text>())
            {
                label.ForceMeshUpdate();
                Assert.That(label.isTextTruncated, Is.False, label.name);
                Assert.That(label.font, Is.SameAs(TerminalUIAssets.Current.font));
                Assert.That(label.text, Is.EqualTo(label.text.ToUpperInvariant()));
            }
            Object.Destroy(CaptureGameplay("tracker-selector"));
            Quaternion before = player.transform.rotation;
            InputSystem.QueueStateEvent(mouse, new MouseState { delta = new Vector2(100, 30) });
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W));
            yield return null;
            yield return null;
            Assert.That(Quaternion.Angle(before, player.transform.rotation), Is.LessThan(.01f));
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            // Click near the right edge, away from the label and icon: the whole row works.
            var button = buttons.First(b => b.name == "Track Anomaly");
            var rect = (RectTransform)button.transform;
            Canvas.ForceUpdateCanvases();
            Vector2 point = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(new Vector3(240, 0, 0)));
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = point }, hits);
            Assert.That(hits.Count, Is.GreaterThan(0));
            Assert.That(hits[0].gameObject.GetComponentInParent<Button>(), Is.SameAs(button));
            InputSystem.QueueStateEvent(mouse, new MouseState { position = point });
            yield return null;
            InputSystem.QueueStateEvent(mouse, new MouseState { position = point }.WithButton(MouseButton.Left));
            yield return null;
            InputSystem.QueueStateEvent(mouse, new MouseState { position = point });
            yield return null;
            yield return null;
            Assert.That((bool)ReadMember(map, "TrackerOpen"), Is.False);
            Assert.That(ReadMember(map, "SelectedTracker").ToString(), Is.EqualTo("Anomaly"));
            Assert.That(Cursor.lockState, Is.EqualTo(savedLock));
            Assert.That(Cursor.visible, Is.False);
            Assert.That(map.GetComponent<GraphicRaycaster>().enabled, Is.False);
            Assert.That(TerminalToolInput.BlocksInput, Is.False);
            Object.Destroy(CaptureGameplay("tracker-anomaly"));
            yield return Press(Key.M);
            yield return Press(Key.M);
            Assert.That((bool)ReadMember(map, "TrackerOpen"), Is.False);
            Assert.That(ReadMember(map, "SelectedTracker").ToString(), Is.EqualTo("Anomaly"));
            yield return Press(Key.M);
            yield return Press(Key.Escape);
            var pause = Object.FindFirstObjectByType<TerminalPause>();
            Assert.That(pause.IsPaused, Is.True);
            Assert.That((bool)ReadMember(map, "TrackerOpen"), Is.False);
            Assert.That(map.GetComponent<Canvas>().enabled, Is.False);
            yield return Press(Key.M);
            Assert.That((bool)ReadMember(map, "TrackerOpen"), Is.False, "M cannot open behind pause/settings.");
            pause.Resume();
            yield return new WaitForSecondsRealtime(.4f);
            Assert.That(Cursor.lockState, Is.EqualTo(savedLock));
            Assert.That(Cursor.visible, Is.False);
            Assert.That(map.GetComponent<Canvas>().enabled, Is.True);
        }

        [UnityTest, Category("Menu"), Category("Surface"), Category("Tracker")]
        public IEnumerator TrackerChangesCategoryAndNearestWithoutHidingOtherMarkers()
        {
            yield return LoadSurface();
            var map = SurfaceMap();
            var player = SurfacePlayer();
            ((Behaviour)player).enabled = false;
            var system = (Component)Object.FindFirstObjectByType(GameplayType("ObjectiveSystem"));
            var objectives = SurveyObjectives(system);
            objectives[0].transform.position = player.transform.position + Vector3.left * 12;
            objectives[1].transform.position = player.transform.position + Vector3.right * 30;
            StyleTracker(map, objectives[0], "Anomaly");
            StyleTracker(map, objectives[1], "Signal");
            foreach (string mode in new[] { "Anomaly", "Signal", "Objective" })
            {
                ChooseTracker(map, mode);
                yield return null;
                yield return null;
                Assert.That(((TMP_Text)ReadMember(map, "objectiveHeading")).text, Is.EqualTo("NEAREST " + mode.ToUpperInvariant()));
                var title = (TMP_Text)ReadMember(map, "targetText");
                var detail = (TMP_Text)ReadMember(map, "targetDetail");
                Assert.That(map.GetComponentsInChildren<Image>().Count(i => i.name.StartsWith("ObjectiveIcon_")), Is.EqualTo(2));
                if (mode == "Objective")
                {
                    Assert.That(title.text, Is.EqualTo("NO ACTIVE OBJECTIVES"));
                    Assert.That(detail.text, Is.Empty);
                    Assert.That(((RectTransform)ReadMember(map, "objectiveDirection")).gameObject.activeSelf, Is.False);
                }
                else
                {
                    int index = mode == "Anomaly" ? 0 : 1;
                    Assert.That(title.text, Is.EqualTo(((string)ReadMember(objectives[index], "Title")).ToUpperInvariant()));
                    Assert.That(detail.text, Is.EqualTo(index == 0 ? "12 M" : "30 M"));
                    Assert.That(detail.color, Is.EqualTo(title.color));
                    if (index == 0) Assert.That(title.color.r, Is.GreaterThan(title.color.g * 2));
                }
                Object.Destroy(CaptureGameplay("tracker-mode-" + mode.ToLowerInvariant()));
            }
            ((IList)ReadMember(map, "markerStyles")).Clear();
            StyleTracker(map, objectives[0], "Anomaly");
            StyleTracker(map, objectives[1], "Anomaly");
            ChooseTracker(map, "Anomaly");
            yield return null;
            yield return null;
            Assert.That(((TMP_Text)ReadMember(map, "targetDetail")).text, Is.EqualTo("12 M"));
            player.transform.position = objectives[1].transform.position - Vector3.right * 3;
            yield return new WaitForSecondsRealtime(.3f);
            Assert.That(((TMP_Text)ReadMember(map, "targetDetail")).text, Is.EqualTo("3 M"));
            objectives[1].gameObject.SetActive(false);
            yield return null;
            Assert.That(((TMP_Text)ReadMember(map, "targetDetail")).text, Is.EqualTo("39 M"));
            objectives[0].gameObject.SetActive(false);
            yield return null;
            Assert.That(((TMP_Text)ReadMember(map, "targetText")).text, Is.EqualTo("NO ANOMALY FOUND"));
        }

        [UnityTest, Category("Menu"), Category("Surface"), Category("Tracker")]
        public IEnumerator RefillTrackerUsesRealStationsWithoutChangingReturnThreshold()
        {
            yield return LoadSurface();
            var map = SurfaceMap();
            var player = SurfacePlayer();
            ((Behaviour)player).enabled = false;
            player.GetComponent<Rigidbody>().detectCollisions = false;
            var system = (Component)Object.FindFirstObjectByType(GameplayType("ObjectiveSystem"));
            var station = (Component)Object.FindFirstObjectByType(GameplayType("ResetOxygen"));
            station.transform.position = player.transform.position + Vector3.forward * 20;
            var second = Object.Instantiate(station.gameObject);
            second.transform.position = player.transform.position + Vector3.left * 7;
            var ship = new GameObject("Assigned return point");
            ship.transform.position = player.transform.position + Vector3.back * 25;
            Call(map, "SetSources", player, system, ship.transform);
            yield return null;
            string oldStatus = ((TMP_Text)ReadMember(map, "oxygenState")).text;
            ChooseTracker(map, "Refill");
            yield return new WaitForSecondsRealtime(.1f);
            Assert.That(((TMP_Text)ReadMember(map, "objectiveHeading")).text, Is.EqualTo("NEAREST REFILL STATION"));
            Assert.That(((TMP_Text)ReadMember(map, "targetDetail")).text, Is.EqualTo("7 M"));
            Assert.That(((TMP_Text)ReadMember(map, "oxygenState")).text, Is.EqualTo(oldStatus));
            Assert.That(((TMP_Text)ReadMember(map, "returnText")).text, Is.EqualTo("REFILL 25M"));
            Object.Destroy(CaptureGameplay("tracker-refill"));
            second.SetActive(false);
            yield return null;
            Assert.That(((TMP_Text)ReadMember(map, "targetDetail")).text, Is.EqualTo("20 M"));
            station.GetComponent<Collider>().enabled = false;
            yield return null;
            Assert.That(((TMP_Text)ReadMember(map, "targetDetail")).text, Is.EqualTo("25 M"));
            Call(player, "SetOxygenStation", new object[] { null });
            Call(map, "SetSources", player, system, null);
            yield return null;
            Assert.That(((TMP_Text)ReadMember(map, "targetText")).text, Is.EqualTo("NO REFILL FOUND"));
            Assert.That(((TMP_Text)ReadMember(map, "targetDetail")).text, Is.Empty);
            Object.Destroy(second);
            Object.Destroy(ship);
        }
    }
}
