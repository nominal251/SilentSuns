#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SilentSuns.Menu.Tests
{
    public partial class TerminalMenuTests
    {
        private MasterComputerDisplay master;
        private Component station;
        private CursorLockMode masterPreviousLock;
        private Vector3 masterStartPosition;
        private Quaternion masterStartRotation;

        private IEnumerator WaitForMasterPage()
        {
            float deadline = Time.realtimeSinceStartup + 2;
            while (master.UI.Transitioning && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(master.UI.Transitioning, Is.False, "The page change must finish promptly.");
            yield return null;
        }

        private IEnumerator OpenMaster()
        {
            yield return SceneManager.LoadSceneAsync("SampleScene");
            yield return null;
            var camera = Camera.main;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/MasterComputer/MasterComputer.prefab");
            Assert.That(prefab, Is.Not.Null);
            // The actual ship player and pause system remain in use. Only the bench is isolated.
            var player = (Component)Object.FindFirstObjectByType(GameplayType("PlayerMovement"));
            player.transform.position += Vector3.right * 1000;
            HoldPlayer(player);
            var rotation = Quaternion.LookRotation(-camera.transform.forward, camera.transform.up);
            var root = Object.Instantiate(prefab, camera.transform.position + camera.transform.forward * 3 - rotation * Vector3.up * .63f, rotation);
            master = root.GetComponent<MasterComputerDisplay>();
            station = root.GetComponentInChildren(GameplayType("MasterComputerStation"));
            yield return null;
            while (!master.UI.Initialized) yield return null;
            master.UI.ConfigureStartup(AssetDatabase.LoadAssetAtPath<Texture>("Assets/MasterComputer/Media/ScreenIntrusion.png"), 0);
            Assert.That(master.UI.Ready, Is.False);
            Assert.That(master.UI.Power, Is.Zero, "The unused computer stays dark.");
            CaptureMaster("master-00-idle");
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            masterStartPosition = camera.transform.localPosition;
            masterStartRotation = camera.transform.localRotation;
            Call(station, "Interact", camera.gameObject);
            // The unfocused test editor can refuse locking; check the state we actually borrowed.
            masterPreviousLock = (CursorLockMode)typeof(TerminalToolInput).GetField("previousLock",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic).GetValue(null);
            yield return new WaitForSecondsRealtime(1.4f);
            CaptureMaster("master-01-login");
            float deadline = Time.realtimeSinceStartup + 12;
            while (!master.UI.Ready && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(master.UI.Ready, Is.True);
            Assert.That(master.UI.GetComponentsInChildren<TMP_Text>().Any(t => t.text.Contains("100% / STABLE")), Is.True,
                "The ship readings must be filled before the login reveals the status page.");
        }

        [UnityTest, Category("Menu"), Category("MasterComputer")]
        public IEnumerator MasterComputerUsesShipResourcesAndRequiresAlertReview()
        {
            yield return OpenMaster();
            var game = TerminalGameSession.Manager;
            Assert.That(master.UI.Data.Game, Is.SameAs(game));
            CaptureMaster("master-02-status");
            game.Resources.ApplyHullDamage(4);
            yield return null;
            Assert.That(master.UI.AlertBlocking, Is.True);
            Assert.That(master.UI.Data.UnreadCount, Is.EqualTo(1));
            master.UI.SelectPage(1);
            Assert.That(master.UI.Page, Is.EqualTo(MasterComputerUI.ScreenPage.Status));
            CaptureMaster("master-03-caution");
            master.UI.SelectPage(3);
            yield return WaitForMasterPage();
            master.UI.SelectRecord(0);
            yield return null;
            Assert.That(master.UI.Data.UnreadCount, Is.Zero);
            CaptureMaster("master-04-alert");
            game.MarkCatalogReady();
            yield return null;
            master.UI.SelectRecord(0);
            master.UI.SelectPage(0);
            yield return WaitForMasterPage();
            var submit = master.UI.GetComponentsInChildren<TerminalButton>().Single(b => b.name == "SUBMIT CATALOG  >");
            Assert.That(submit.interactable, Is.True);
            submit.onClick.Invoke();
            yield return null;
            Assert.That(game.CatalogSubmitted, Is.True);
            Assert.That(game.Resources.NitrogenCanisters, Is.EqualTo(3), "Submission must not consume cryosleep resources.");
            Assert.That(master.UI.AlertBlocking, Is.True);
            Assert.That(master.UI.Data.Alerts[0].title, Is.EqualTo("CRYOCHAMBER AVAILABLE"));
        }

        [UnityTest, Category("Menu"), Category("MasterComputer")]
        public IEnumerator MasterComputerReadsRecordsWithoutChangingSourceAssets()
        {
            yield return OpenMaster();
            var data = master.UI.Data;
            var disk = ScriptableObject.CreateInstance<MasterComputerRecord>();
            var tool = ScriptableObject.CreateInstance<MasterComputerRecord>();
            try
            {
                disk.id = "test-disk"; disk.title = "Engineering log"; disk.author = "Engineering"; disk.recordedAt = "LOCAL RECORD / 001";
                disk.body = "Pressure check complete. The forward seal is holding.\n\n" + string.Join("\n\n", Enumerable.Repeat("Inspection record. Check the seal before departure.", 18));
                tool.type = MasterComputerRecord.RecordType.Tool;
                tool.id = "test-tool"; tool.title = "Field instrument"; tool.useType = "Passive"; tool.foundOn = "Survey site"; tool.body = "Instrument report stored in local memory.";
                Assert.That(data.Logs, Is.Empty);
                master.UI.SelectPage(1);
                yield return WaitForMasterPage();
                CaptureMaster("master-13-empty-log");
                master.UI.SelectPage(2);
                yield return WaitForMasterPage();
                CaptureMaster("master-14-empty-tools");
                master.UI.SelectPage(3);
                yield return WaitForMasterPage();
                CaptureMaster("master-15-empty-alerts");
                Assert.That(data.InsertDisk(tool), Is.False);
                Assert.That(data.InsertDisk(disk), Is.True);
                Assert.That(data.InsertDisk(disk), Is.False);
                Assert.That(data.RegisterTool(tool), Is.True);
                master.UI.SelectPage(1);
                yield return WaitForMasterPage();
                master.UI.SelectRecord(0);
                yield return null;
                CaptureMaster("master-05-log");
                var scroll = master.UI.GetComponentInChildren<UnityEngine.UI.ScrollRect>();
                Assert.That(scroll.content.rect.height, Is.GreaterThan(scroll.viewport.rect.height));
                // Drag the real handle through the monitor's texture, not just the wheel or an API call.
                var bar = scroll.verticalScrollbar;
                var barRect = (RectTransform)bar.transform;
                Vector2 start = MasterPointer(bar.handleRect.position);
                Vector2 end = MasterPointer(barRect.TransformPoint(new Vector3(0, barRect.rect.yMin + barRect.rect.height * .15f, 0)));
                InputSystem.QueueStateEvent(mouse, new MouseState { position = start });
                yield return null;
                InputSystem.QueueStateEvent(mouse, new MouseState { position = start }.WithButton(MouseButton.Left));
                yield return null;
                InputSystem.QueueStateEvent(mouse, new MouseState { position = end }.WithButton(MouseButton.Left));
                yield return null;
                InputSystem.QueueStateEvent(mouse, new MouseState { position = end });
                yield return new WaitForSecondsRealtime(.2f);
                Assert.That(scroll.verticalNormalizedPosition, Is.LessThan(.8f), "The log scrollbar must respond to a physical-screen drag.");
                Assert.That(disk.title, Is.EqualTo("Engineering log"), "Uppercase display must not rewrite the content asset.");
                master.UI.SelectPage(2);
                yield return WaitForMasterPage();
                master.UI.SelectRecord(0);
                CaptureMaster("master-06-tool");
                Assert.That(master.UI.GetComponentsInChildren<TMP_Text>().Any(t => t.text.Contains("PASSIVE") && t.text.Contains("SURVEY SITE")), Is.True);
                foreach (var label in master.UI.GetComponentsInChildren<TMP_Text>())
                {
                    Assert.That(label.font, Is.SameAs(TerminalUIAssets.Current.font));
                    Assert.That(label.text, Is.EqualTo(label.text.ToUpperInvariant()));
                }
            }
            finally { Object.Destroy(disk); Object.Destroy(tool); }
        }

        [UnityTest, Category("Menu"), Category("MasterComputer")]
        public IEnumerator MasterComputerPhysicalClicksAndPauseKeepTheirOwnInput()
        {
            yield return OpenMaster();
            Assert.That(TerminalToolInput.BlocksInput, Is.True);
            master.transform.localScale = Vector3.one * .75f;
            Assert.That(master.UI.transform.lossyScale.x, Is.EqualTo(.01f).Within(.0001f), "Resizing the model must not resize its hidden UI canvas.");
            var button = master.UI.GetComponentsInChildren<TerminalButton>().Single(b => b.name == "EXIT COMPUTER");
            Canvas.ForceUpdateCanvases();
            Vector3 contentPoint = master.SourceCamera.WorldToViewportPoint(button.transform.position);
            Vector3 glassPoint = master.Screen.transform.TransformPoint(new Vector3(contentPoint.x - .5f, contentPoint.y - .5f, 0));
            Vector2 pointer = Camera.main.WorldToScreenPoint(glassPoint);
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = pointer }, hits);
            Assert.That(hits.Any(h => h.gameObject.GetComponentInParent<TerminalButton>() == button), Is.True, "The visible glass button must receive pointer input.");
            Assert.That(master.EffectMaterial.GetFloat("_ScanStrength"), Is.EqualTo(TerminalUIAssets.Current.crtSettings.scanlines * .6f * GamePreferences.CRTIntensity).Within(.001f));
            Assert.That(TerminalUIAssets.Current.crtSettings.scanlines, Is.EqualTo(.62f).Within(.001f));
            var pause = Object.FindFirstObjectByType<TerminalPause>();
            pause.Pause();
            yield return null;
            Assert.That(master.UI.InUse, Is.False);
            hits.Clear();
            master.UI.GetComponent<MasterComputerRaycaster>().Raycast(new PointerEventData(EventSystem.current) { position = pointer }, hits);
            Assert.That(hits, Is.Empty);
            pause.Resume();
            yield return new WaitForSecondsRealtime(.5f);
            Assert.That(TerminalToolInput.BlocksInput, Is.False);
            Assert.That(Cursor.visible, Is.False);
            Assert.That(Cursor.lockState, Is.EqualTo(masterPreviousLock));
        }

        [UnityTest, Category("Menu"), Category("MasterComputer")]
        public IEnumerator EscapeExitsComputerBeforeOpeningPause()
        {
            yield return OpenMaster();
            master.UI.SelectPage(1);
            yield return WaitForMasterPage();
            yield return Press(Key.Escape);
            Assert.That(master.UI.InUse, Is.False);
            Assert.That(master.UI.Page, Is.EqualTo(MasterComputerUI.ScreenPage.Status));
            Assert.That(master.UI.Power, Is.Zero);
            Assert.That(TerminalPause.Paused, Is.False, "Escape exits the computer without pausing the run.");
            Assert.That(Time.timeScale, Is.EqualTo(1));
            Assert.That(TerminalToolInput.BlocksInput, Is.True, "The return animation keeps player input held.");
            yield return Press(Key.Escape);
            Assert.That(TerminalPause.Paused, Is.False, "Escape during the return must not open pause underneath it.");
            yield return new WaitForSecondsRealtime(.7f);
            Assert.That(TerminalToolInput.BlocksInput, Is.False);
            Assert.That(Cursor.visible, Is.False);
            Assert.That(Cursor.lockState, Is.EqualTo(masterPreviousLock));
            Assert.That(Vector3.Distance(Camera.main.transform.localPosition, masterStartPosition), Is.LessThan(.001f));
            yield return Press(Key.Escape);
            Assert.That(TerminalPause.Paused, Is.True, "Once outside the computer, Escape pauses normally.");
            Object.FindFirstObjectByType<TerminalPause>().Resume();
            yield return new WaitForSecondsRealtime(.5f);

            // Escape must also cancel a login that has only just started.
            Call(station, "Interact", Camera.main.gameObject);
            yield return null;
            Assert.That(master.UI.Ready, Is.False);
            yield return Press(Key.Escape);
            yield return new WaitForSecondsRealtime(.7f);
            Assert.That(master.UI.InUse, Is.False);
            Assert.That(master.UI.Ready, Is.False);
            Assert.That(master.UI.Power, Is.Zero);
            Assert.That(TerminalPause.Paused, Is.False);
            Assert.That(TerminalToolInput.BlocksInput, Is.False);
        }

        private Vector2 MasterPointer(Vector3 contentPosition)
        {
            Vector3 point = master.SourceCamera.WorldToViewportPoint(contentPosition);
            return Camera.main.WorldToScreenPoint(master.Screen.transform.TransformPoint(new Vector3(point.x - .5f, point.y - .5f, 0)));
        }

        [UnityTest, Category("Menu"), Category("MasterComputer")]
        public IEnumerator MasterComputerReviewOpensThroughPlayerInteraction()
        {
            yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(
                "Assets/MasterComputer/MasterComputerReview.unity", new LoadSceneParameters(LoadSceneMode.Single));
            float deadline = Time.realtimeSinceStartup + 15;
            while (!GameplayType("MasterComputerReview").GetProperty("Ready").GetValue(null).Equals(true) && Time.realtimeSinceStartup < deadline)
                yield return null;
            master = Object.FindFirstObjectByType<MasterComputerDisplay>();
            Assert.That(master, Is.Not.Null);
            while (!master.UI.Initialized && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(master.UI.Initialized, Is.True);
            Assert.That(master.UI.Ready, Is.False);
            Assert.That(master.UI.InUse, Is.False);
            master.UI.ConfigureStartup(AssetDatabase.LoadAssetAtPath<Texture>("Assets/MasterComputer/Media/ScreenIntrusion.png"), 0);
            // Use Ian's actual remappable interact action, rather than calling the station ourselves.
            SetTestBinding("Interact", "<Mouse>/leftButton", "<Keyboard>/f");
            Physics.SyncTransforms();
            Assert.That(Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out var hit, 3), Is.True);
            Assert.That(hit.collider.gameObject.GetComponent(GameplayType("MasterComputerStation")), Is.Not.Null);
            yield return Press(Key.F);
            Assert.That(master.UI.InUse, Is.True);
            while (!master.UI.Ready && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(master.UI.Ready, Is.True);
            var logs = master.UI.GetComponentsInChildren<TerminalButton>().Single(b => b.name == "EXIT COMPUTER");
            Vector3 point = master.SourceCamera.WorldToViewportPoint(logs.transform.position);
            Vector2 pointer = Camera.main.WorldToScreenPoint(master.Screen.transform.TransformPoint(new Vector3(point.x - .5f, point.y - .5f, 0)));
            InputSystem.QueueStateEvent(mouse, new MouseState { position = pointer });
            yield return null;
            InputSystem.QueueStateEvent(mouse, new MouseState { position = pointer }.WithButton(MouseButton.Left));
            yield return null;
            InputSystem.QueueStateEvent(mouse, new MouseState { position = pointer });
            yield return new WaitForSecondsRealtime(.2f);
            Assert.That(master.UI.InUse, Is.False, "The exit button must work through the physical glass.");
            Assert.That(TerminalInteractionAudio.Instance.LastClip, Is.SameAs(
                AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/MainMenu/Audio/Interaction/MouseClick.wav")));
            yield return new WaitForSecondsRealtime(.6f);
            CaptureMaster("master-07-review-scene");
        }

        [UnityTest, Category("MasterComputer")]
        public IEnumerator MasterComputerZoomDialAndExitPreservePlayerCamera()
        {
            yield return OpenMaster();
            var camera = Camera.main;
            Assert.That(Vector3.Distance(camera.transform.localPosition, masterStartPosition), Is.GreaterThan(.1f));
            CaptureMaster("master-08-focused");
            InputSystem.QueueStateEvent(mouse, new MouseState { position = new Vector2(Screen.width * .5f, Screen.height * .5f) });
            yield return null;
            InputSystem.QueueStateEvent(mouse, new MouseState { position = new Vector2(Screen.width * .5f, 2) });
            yield return new WaitForSecondsRealtime(.75f);
            InputSystem.QueueStateEvent(mouse, new MouseState { position = new Vector2(Screen.width * .5f, Screen.height * .5f) });
            yield return new WaitForSecondsRealtime(.25f);
            CaptureMaster("master-09-console-dial");
            var selector = master.GetComponentInChildren(GameplayType("MasterComputerSelector"));
            Assert.That(selector, Is.Not.Null);
            var dial = selector.transform.Find("Dial");
            Quaternion firstPosition = dial.localRotation;
            var rotary = dial.GetComponent(GameplayType("RotarySwitch"));
            Assert.That(rotary, Is.Not.Null);
            var angles = (Vector3[])ReadMember(rotary, "stateRotations");
            Assert.That(angles.Length, Is.EqualTo(4));
            Assert.That(AssetDatabase.AssetPathToGUID("Assets/Scripts/RotarySwitch.cs"), Is.EqualTo("8b390ee9dae3e1048b6fbc1a88bfd9fe"));
            Assert.That(master.GetComponentInChildren<Light>(true).transform.parent.name, Is.EqualTo("MasterComputer_Console"));
            var switchAudio = selector.GetComponent<AudioSource>();
            Assert.That(switchAudio.clip, Is.SameAs(AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/Click2.mp3")));
            Assert.That(switchAudio.outputAudioMixerGroup.name, Is.EqualTo("FX"));
            Vector3 point = camera.WorldToScreenPoint(selector.GetComponent<Collider>().bounds.center);
            Assert.That(point.z, Is.GreaterThan(0));
            Assert.That(point.y, Is.InRange(0, Screen.height));
            Assert.That(Physics.Raycast(camera.ScreenPointToRay(point), out var hit, 5), Is.True);
            Assert.That(hit.collider.GetComponent(GameplayType("MasterComputerSelector")), Is.SameAs(selector));
            InputSystem.QueueStateEvent(mouse, new MouseState { position = point });
            yield return null;
            InputSystem.QueueStateEvent(mouse, new MouseState { position = point }.WithButton(MouseButton.Left));
            yield return null;
            InputSystem.QueueStateEvent(mouse, new MouseState { position = point });
            yield return new WaitForSecondsRealtime(.2f);
            Assert.That(master.UI.Page, Is.EqualTo(MasterComputerUI.ScreenPage.Logs));
            Call(selector, "Interact", camera.gameObject);
            Assert.That(master.UI.Page, Is.EqualTo(MasterComputerUI.ScreenPage.Tools));
            Assert.That(switchAudio.isPlaying, Is.True, "Turning the dial plays the existing ship-switch sound.");
            yield return null;
            Call(selector, "Interact", camera.gameObject);
            Assert.That(master.UI.Page, Is.EqualTo(MasterComputerUI.ScreenPage.Alerts));
            yield return null;
            Call(selector, "Interact", camera.gameObject);
            Assert.That(master.UI.Page, Is.EqualTo(MasterComputerUI.ScreenPage.Status));
            Assert.That(master.UI.GetComponentsInChildren<TerminalButton>().Any(b => new[] { "STATUS", "LOGS", "R&D / TOOLS", "ALERTS" }.Contains(b.name)), Is.False);
            var page = master.UI.transform.Find("Screen contents/Controls/Page").GetComponent<CanvasGroup>();
            for (int i = 0; i < 5; i++)
            {
                Assert.That(page.alpha, Is.EqualTo(1), "Changing pages must not dim or flash the screen.");
                yield return null;
            }
            for (int i = 0; i < 4; i++)
            {
                master.UI.SelectPage(i);
                yield return WaitForMasterPage();
                yield return new WaitForSecondsRealtime(.3f);
                Assert.That(ReadMember(rotary, "currentState"), Is.EqualTo(i));
                Assert.That(Quaternion.Angle(dial.localRotation, Quaternion.Euler(angles[i])), Is.LessThan(.1f));
                string title = new[] { "00 / STATUS", "01 / LOGS", "02 / R&D", "03 / ALERTS" }[i];
                Assert.That(master.UI.GetComponentsInChildren<TMP_Text>().Any(t => t.text == title), Is.True);
                CaptureMaster("master-dial-position-" + i);
            }
            master.UI.SelectPage(2);
            yield return WaitForMasterPage();
            Assert.That(Quaternion.Angle(dial.localRotation, firstPosition), Is.GreaterThan(20));
            switchAudio.Stop();
            master.UI.GetComponentsInChildren<TerminalButton>().Single(b => b.name == "EXIT COMPUTER").onClick.Invoke();
            Assert.That(master.UI.Page, Is.EqualTo(MasterComputerUI.ScreenPage.Status));
            Assert.That(TerminalToolInput.BlocksInput, Is.True, "Controls stay held during the return transition.");
            yield return null;
            yield return null;
            Assert.That(switchAudio.isPlaying, Is.True, "The automatic return to Status clicks when leaving.");
            switchAudio.Stop();
            yield return new WaitForSecondsRealtime(.75f);
            Assert.That(switchAudio.isPlaying, Is.False, "The return click must not repeat while the dial settles.");
            Assert.That(Vector3.Distance(camera.transform.localPosition, masterStartPosition), Is.LessThan(.001f));
            Assert.That(Quaternion.Angle(camera.transform.localRotation, masterStartRotation), Is.LessThan(.01f));
            Assert.That(TerminalToolInput.BlocksInput, Is.False);
            Assert.That(Quaternion.Angle(dial.localRotation, firstPosition), Is.LessThan(.1f), "Leaving returns the physical dial to 0 / Status.");
        }

        [UnityTest, Category("MasterComputer")]
        public IEnumerator SceneRotarySwitchesKeepTheirAssignedPages()
        {
            // The old development scenes have an unassigned objective system. This check
            // exercises their switches; SampleScene still runs its connected objectives.
            void IsolateUnconfiguredObjectives(Scene loadedScene, LoadSceneMode mode)
            {
                foreach (var root in loadedScene.GetRootGameObjects())
                    foreach (Component tracker in root.GetComponentsInChildren(GameplayType("ObjectiveTracker"), true))
                        if (ReadMember(tracker, "objectiveSystem") == null)
                            ((Behaviour)tracker).enabled = false;
            }
            SceneManager.sceneLoaded += IsolateUnconfiguredObjectives;
            try
            {
                foreach (string scene in new[] { "SampleScene", "EventsTesting", "JackMergeScene" })
                {
                    yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(
                        "Assets/Scenes/" + scene + ".unity", new LoadSceneParameters(LoadSceneMode.Single));
                    yield return null;
                    var switches = Object.FindObjectsByType(GameplayType("RotarySwitch"), FindObjectsInactive.Include, FindObjectsSortMode.None);
                    Assert.That(switches.Length, Is.GreaterThan(0), scene + " must retain its rotary switch.");
                    foreach (Component rotary in switches)
                    {
                        var pages = (GameObject[])ReadMember(rotary, "stateObjects");
                        var angles = (Vector3[])ReadMember(rotary, "stateRotations");
                        var dial = (Transform)ReadMember(rotary, "dial");
                        Assert.That(pages.Length, Is.EqualTo(3));
                        Assert.That(angles.Length, Is.EqualTo(3));
                        if (scene != "EventsTesting") Assert.That(pages.All(p => p != null), Is.True);
                        Assert.That(dial, Is.Not.Null);
                        Assert.That(dial.GetComponent<MeshFilter>().sharedMesh, Is.Not.Null, scene + " dial model must be connected.");
                        Call(rotary, "SetState", 1);
                        yield return new WaitForSecondsRealtime(.3f);
                        // EventsTesting has unassigned placeholder outputs; the ship scenes have real pages.
                        for (int i = 0; i < pages.Length; i++)
                            if (pages[i] != null) Assert.That(pages[i].activeSelf, Is.EqualTo(i == 1));
                        Assert.That(Quaternion.Angle(dial.localRotation, Quaternion.Euler(angles[1])), Is.LessThan(.1f));
                        Call(rotary, "SetState", 0);
                    }
                }
            }
            finally { SceneManager.sceneLoaded -= IsolateUnconfiguredObjectives; }
        }

        [UnityTest, Category("MasterComputer")]
        public IEnumerator MasterComputerBootTypesCommandsWithSharedInterfaceAudio()
        {
            yield return OpenMaster();
            Call(station, "EndUse");
            var sound = TerminalInteractionAudio.Instance;
            Assert.That(sound, Is.Not.Null);
            Assert.That(sound.GetComponent<AudioSource>().outputAudioMixerGroup, Is.SameAs(GameAudio.InterfaceGroup));
            int beforeSounds = sound.AcceptedSounds;
            Call(station, "Interact", Camera.main.gameObject);
            var text = master.UI.GetComponentsInChildren<TMP_Text>(true).Single(t => t.name == "Credentials");
            float deadline = Time.realtimeSinceStartup + 12;
            while (!text.text.Contains("MC:\\> L") && Time.realtimeSinceStartup < deadline) yield return null;
            string firstText = text.text;
            yield return new WaitForSecondsRealtime(.2f);
            Assert.That(text.text, Is.Not.EqualTo(firstText), "The command must appear as typing, not a whole line at once.");
            Assert.That(master.UI.Ready, Is.False);
            Assert.That(sound.AcceptedSounds, Is.GreaterThan(beforeSounds));
            Assert.That(sound.LastClip, Is.SameAs(AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/MainMenu/Audio/Interaction/KeyboardPress.wav")));
            while (!text.text.Contains("SESSION READY") && Time.realtimeSinceStartup < deadline) yield return null;
            CaptureMaster("master-12-typed-startup");
            Assert.That(text.text, Does.Contain("LOG ARCHIVE"));
            Assert.That(text.text, Does.Contain("RESEARCH RECORDS"));
            Assert.That(text.text, Does.Contain("TELEMETRY / CATALOG / ALERTS"));
            Assert.That(text.isTextOverflowing, Is.False, "The boot transcript must fit on the monitor.");
            while (!master.UI.Transitioning && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(master.UI.Ready, Is.False, "Login hands over before the page accepts input.");
            yield return new WaitForSecondsRealtime(.15f);
            CaptureMaster("master-16-boot-handoff");
            while (!master.UI.Ready && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(master.UI.Ready, Is.True);
        }

        [UnityTest, Category("MasterComputer")]
        public IEnumerator MasterComputerPageSettlingKeepsLatestDialChoiceAndCancelsCleanly()
        {
            yield return OpenMaster();
            var page = master.UI.transform.Find("Screen contents/Controls/Page").GetComponent<CanvasGroup>();
            master.UI.SelectPage(1);
            Assert.That(master.UI.Transitioning, Is.True);
            Assert.That(page.interactable, Is.False);
            Assert.That(master.UI.GetComponentsInChildren<TMP_Text>().Any(t => t.text.Contains("100% / STABLE")), Is.True,
                "The old page should hold instead of going blank.");
            yield return new WaitForSecondsRealtime(.1f);
            CaptureMaster("master-17-page-access");
            master.UI.SelectPage(3);
            master.UI.SelectPage(2);
            yield return WaitForMasterPage();
            Assert.That(master.UI.Page, Is.EqualTo(MasterComputerUI.ScreenPage.Tools));
            Assert.That(master.UI.GetComponentsInChildren<TMP_Text>().Any(t => t.text == "02 / R&D"), Is.True);
            Assert.That(page.interactable, Is.True);
            Assert.That(master.EffectMaterial.GetFloat("_SyncRipple"), Is.EqualTo(0).Within(.001f));

            // Ambient disturbances should be brief, then return to an undistorted reading view.
            float deadline = Time.realtimeSinceStartup + 12;
            while (master.EffectMaterial.GetFloat("_SyncRipple") < .1f && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(master.EffectMaterial.GetFloat("_SyncRipple"), Is.GreaterThan(.1f));
            CaptureMaster("master-18-ambient-ripple");
            yield return new WaitForSecondsRealtime(1.2f);
            Assert.That(master.EffectMaterial.GetFloat("_SyncRipple"), Is.EqualTo(0).Within(.001f));
            GamePreferences.SetReducedMotion(true);
            master.UI.SelectPage(3);
            yield return null;
            yield return null;
            Assert.That(master.EffectMaterial.GetFloat("_SyncRipple"), Is.Zero);
            Assert.That(master.EffectMaterial.GetFloat("_Disturbance"), Is.Zero);
            yield return WaitForMasterPage();
            CaptureMaster("master-19-clear-symbols");
            GamePreferences.SetReducedMotion(false);

            master.UI.SelectPage(1);
            yield return null;
            yield return null;
            var switchAudio = master.GetComponentInChildren(GameplayType("MasterComputerSelector")).GetComponent<AudioSource>();
            switchAudio.Stop();
            Object.FindFirstObjectByType<TerminalPause>().Pause();
            yield return null;
            yield return null;
            Assert.That(AudioListener.pause, Is.True);
            Assert.That(switchAudio.ignoreListenerPause, Is.True);
            Assert.That(switchAudio.isPlaying, Is.True, "The return click must play while gameplay audio is paused.");
            Assert.That(master.UI.InUse, Is.False);
            Assert.That(master.UI.Page, Is.EqualTo(MasterComputerUI.ScreenPage.Status));
            Assert.That(master.UI.Transitioning, Is.False);
            Assert.That(master.UI.Power, Is.Zero);
            Assert.That(master.EffectMaterial.GetFloat("_SyncRipple"), Is.Zero);
            Object.FindFirstObjectByType<TerminalPause>().Resume();
            yield return new WaitForSecondsRealtime(.5f);
            Call(station, "Interact", Camera.main.gameObject);
            deadline = Time.realtimeSinceStartup + 12;
            while (!master.UI.Ready && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(master.UI.Ready, Is.True, "An interrupted page change must not leave the next login stuck.");
            Assert.That(master.UI.Page, Is.EqualTo(MasterComputerUI.ScreenPage.Status));
            Assert.That(page.interactable, Is.True);
            switchAudio.Stop();
            Call(station, "EndUse");
            yield return null;
            yield return null;
            Assert.That(switchAudio.isPlaying, Is.False, "Leaving from Status does not click again.");
        }

        [UnityTest, Category("MasterComputer")]
        public IEnumerator MasterComputerIntrusionCancelsOnPauseWithoutATestingKey()
        {
            yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(
                "Assets/MasterComputer/MasterComputerReview.unity", new LoadSceneParameters(LoadSceneMode.Single));
            float deadline = Time.realtimeSinceStartup + 15;
            while (!GameplayType("MasterComputerReview").GetProperty("Ready").GetValue(null).Equals(true) && Time.realtimeSinceStartup < deadline) yield return null;
            master = Object.FindFirstObjectByType<MasterComputerDisplay>();
            station = master.GetComponentInChildren(GameplayType("MasterComputerStation"));
            while (!master.UI.Initialized && Time.realtimeSinceStartup < deadline) yield return null;
            master.UI.ConfigureStartup(AssetDatabase.LoadAssetAtPath<Texture>("Assets/MasterComputer/Media/ScreenIntrusion.png"), 0);
            var light = new GameObject("Startup light test", typeof(Light)).GetComponent<Light>();
            light.transform.position = master.transform.position + Vector3.up * 2;
            light.type = LightType.Point; light.intensity = 2.4f;
            yield return Press(Key.J);
            Assert.That(master.UI.InUse, Is.False);
            Assert.That(master.UI.Intrusion, Is.False);
            Assert.That(master.UI.Power, Is.Zero, "J has no shortcut in the review scene.");
            Call(station, "Interact", Camera.main.gameObject);
            while (!master.UI.Ready && Time.realtimeSinceStartup < deadline)
            {
                Assert.That(master.UI.Intrusion, Is.False, "J must not arm a later startup either.");
                yield return null;
            }
            Assert.That(master.UI.Ready, Is.True);
            Call(station, "EndUse");
            yield return null;
            deadline = Time.realtimeSinceStartup + 15;
            // Force the event through the editor-only test hook, never a gameplay key.
            master.UI.ArmStartupIntrusion();
            SetTestBinding("Interact", "<Mouse>/leftButton", "<Keyboard>/f");
            yield return Press(Key.F);
            while (!master.UI.Intrusion && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(master.UI.Intrusion, Is.True);
            Assert.That(master.UI.Ready, Is.False);
            yield return new WaitForSecondsRealtime(.5f);
            CaptureMaster("master-10-intrusion");
            var sound = master.GetComponent<AudioSource>();
            Assert.That(sound, Is.Not.Null);
            Assert.That(sound.clip, Is.Not.Null);
            Assert.That(sound.clip.length, Is.InRange(2, 4));
            Assert.That(sound.outputAudioMixerGroup.name, Is.EqualTo("FX"));
            Assert.That(sound.volume, Is.LessThanOrEqualTo(.18f));
            Assert.That(light.intensity, Is.LessThan(2.4f));
            Assert.That(master.EffectMaterial.GetFloat("_HandoffFault"), Is.GreaterThan(0));
            Object.FindFirstObjectByType<TerminalPause>().Pause();
            yield return null;
            Assert.That(master.UI.Intrusion, Is.False);
            Assert.That(master.UI.Power, Is.Zero);
            Assert.That(sound.isPlaying, Is.False);
            Assert.That(light.intensity, Is.EqualTo(2.4f));
            Object.FindFirstObjectByType<TerminalPause>().Resume();
            yield return new WaitForSecondsRealtime(.5f);
            Call(station, "Interact", Camera.main.gameObject);
            deadline = Time.realtimeSinceStartup + 12;
            while (!master.UI.Ready && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(master.UI.Ready, Is.True, "The forced startup is consumed once, even when interrupted.");
            Assert.That(master.UI.Intrusion, Is.False);
            Object.Destroy(light.gameObject);
        }

        [UnityTest, Category("MasterComputer")]
        public IEnumerator MasterComputerIntrusionFinishesAndRespectsReducedMotion()
        {
            yield return OpenMaster();
            Call(station, "EndUse");
            GamePreferences.SetReducedMotion(true);
            var light = new GameObject("Comfort light test", typeof(Light)).GetComponent<Light>();
            light.transform.position = master.transform.position + Vector3.up;
            light.type = LightType.Point; light.intensity = 3.2f;
            master.UI.ArmStartupIntrusion();
            Call(station, "Interact", Camera.main.gameObject);
            float deadline = Time.realtimeSinceStartup + 16;
            while (!master.UI.Intrusion && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(master.UI.Intrusion, Is.True);
            yield return new WaitForSecondsRealtime(.5f);
            Assert.That(light.intensity, Is.EqualTo(3.2f));
            Assert.That(master.UI.FaultStrength, Is.Zero);
            Assert.That(master.EffectMaterial.GetFloat("_HandoffFault"), Is.Zero);
            while (!master.UI.Ready && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(master.UI.Ready, Is.True);
            Assert.That(master.UI.Intrusion, Is.False);
            Assert.That(master.UI.Power, Is.EqualTo(1));
            Assert.That(master.GetComponent<AudioSource>().isPlaying, Is.False);
            Assert.That(light.intensity, Is.EqualTo(3.2f));
            CaptureMaster("master-11-recovered");
            Object.Destroy(light.gameObject);
        }

        [Test, Category("MasterComputer")]
        public void MasterComputerRareRollDoesNotRepeatWithinTheRun()
        {
            var owner = new GameObject("Startup roll", typeof(ResourceManager), typeof(GameManager));
            try
            {
                var game = owner.GetComponent<GameManager>();
                var data = MasterComputerData.For(game);
                game.StartNewRun();
                Assert.That(data.TryStartupIntrusion(.5f, .001f), Is.False);
                Assert.That(data.TryStartupIntrusion(0, .001f), Is.False, "Repeated opening cannot reroll during the cooldown.");
                game.ReturnToBoot(); game.StartNewRun();
                Assert.That(data.TryStartupIntrusion(0, .001f), Is.True);
                Assert.That(data.TryStartupIntrusion(0, 1), Is.False, "Only one random interruption is allowed per run.");
            }
            finally { Object.DestroyImmediate(owner); }
        }

        [Test, Category("MasterComputer")]
        public void MasterComputerRecordsResetOnlyForANewRun()
        {
            var owner = new GameObject("Test run", typeof(ResourceManager), typeof(GameManager));
            var game = owner.GetComponent<GameManager>();
            var data = MasterComputerData.For(game);
            var disk = ScriptableObject.CreateInstance<MasterComputerRecord>();
            try
            {
                game.StartNewRun(); game.NotifySystemLoaded();
                disk.id = "retained-log";
                data.InsertDisk(disk);
                game.MarkCatalogReady(); game.SubmitCatalog(); game.EnterCryosleep(); game.FinishCryosleepTransition(); game.NotifySystemLoaded();
                Assert.That(data.Logs.Count, Is.EqualTo(1));
                game.ReturnToBoot(); game.StartNewRun();
                Assert.That(data.Logs, Is.Empty);
                Assert.That(data.Alerts, Is.Empty);
            }
            finally { Object.DestroyImmediate(owner); Object.DestroyImmediate(disk); }
        }

        private void CaptureMaster(string name)
        {
            string folder = System.Environment.GetEnvironmentVariable("SILENT_SUNS_MENU_CAPTURES");
            if (string.IsNullOrEmpty(folder)) return;
            Canvas.ForceUpdateCanvases();
            RenderPipeline.SubmitRenderRequest(master.SourceCamera, new UniversalRenderPipeline.SingleCameraRequest { destination = master.Texture });
            var target = new RenderTexture(1600, 1000, 24);
            target.Create();
            RenderPipeline.SubmitRenderRequest(Camera.main, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
            var previous = RenderTexture.active;
            RenderTexture.active = target;
            var image = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
            image.Apply();
            Directory.CreateDirectory(folder);
            File.WriteAllBytes(Path.Combine(folder, name + ".png"), image.EncodeToPNG());
            RenderTexture.active = previous;
            Object.Destroy(image); target.Release(); Object.Destroy(target);
        }
    }
}
#endif
