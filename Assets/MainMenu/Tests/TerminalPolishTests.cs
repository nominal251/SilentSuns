using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace SilentSuns.Menu.Tests
{
    public partial class TerminalMenuTests
    {
        private readonly string[] optionNames = { "Interface", "Brightness", "Contrast", "TextScale", "CRTIntensity", "SafeArea", "ControllerSensitivity", "DeadZone", "CursorSize", "DarkCursor", "CursorOutline", "InvertLook", "MuteUnfocused", "VSync", "FrameCap", "Refresh" };
        private readonly Dictionary<string, float> savedOptions = new Dictionary<string, float>();
        private string savedBindings;
        private void SavePolishPreferences()
        {
            savedOptions.Clear();
            foreach (string name in optionNames)
            {
                string key = "SilentSuns.Options." + name;
                if (PlayerPrefs.HasKey(key))
                    savedOptions[key] = PlayerPrefs.GetFloat(key);
                PlayerPrefs.DeleteKey(key);
            }
            savedBindings = PlayerPrefs.HasKey("SilentSuns.Input.Bindings") ? PlayerPrefs.GetString("SilentSuns.Input.Bindings") : null;
            TerminalBindings.Reset();
        }
        private void RestorePolishPreferences()
        {
            foreach (string name in optionNames)
            {
                string key = "SilentSuns.Options." + name;
                if (savedOptions.TryGetValue(key, out float value))
                    PlayerPrefs.SetFloat(key, value);
                else
                    PlayerPrefs.DeleteKey(key);
            }
            TerminalBindings.Reset();
            if (savedBindings != null)
            {
                PlayerPrefs.SetString("SilentSuns.Input.Bindings", savedBindings);
                TerminalBindings.Controls?.LoadBindingOverridesFromJson(savedBindings);
            }
            if (TerminalModal.Current != null)
                TerminalModal.Current.Close();
        }
        private TerminalSettings MainSettings => terminal.Find("Settings").GetComponent<TerminalSettings>();
        private Slider SettingsSlider(string tab, string row, TerminalSettings owner = null)
        {
            owner = owner == null ? MainSettings : owner;
            owner.ShowTab(tab);
            Canvas.ForceUpdateCanvases();
            return owner.GetComponentsInChildren<Slider>().First(s => s.transform.parent.name == row);
        }
        private void SettingsFlag(string tab, string row)
        {
            MainSettings.ShowTab(tab);
            var button = MainSettings.GetComponentsInChildren<TerminalButton>().First(b => b.name.StartsWith(row + " "));
            ExecuteEvents.Execute(button.gameObject, new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left }, ExecuteEvents.pointerClickHandler);
        }
        private IEnumerator HoldButton(GameObject button)
        {
            EventSystem.current.SetSelectedGameObject(button);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Enter));
            yield return new WaitForSecondsRealtime(1.25f);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return null;
        }
        private IEnumerator ReadyMain()
        {
            Click("Standby/Wake screen");
            yield return WaitForState(TerminalMenu.PowerState.Booting);
            Click("Boot/Skip Boot");
            yield return new WaitForSecondsRealtime(.4f);
        }
        [UnityTest, Category("Menu")]
        public IEnumerator ReloadingSceneWithSettingsOpenRestoresTheMenu()
        {
            yield return ReadyMain();
            terminal.GetComponent<TerminalMenu>().OpenSettings();
            GamePreferences.SetOption("SafeArea", .06f);
            Assert.That(MainSettings.HasChanges, Is.True);
            yield return SceneManager.LoadSceneAsync("MainMenu");
            float deadline = Time.realtimeSinceStartup + 30;
            while (!TerminalShipIntro.Ready && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(TerminalShipIntro.Ready, Is.True);
            terminal = GameObject.Find("ARCA Terminal").transform;
            yield return WaitForInvitation();
            Assert.That(GamePreferences.HasChanges, Is.False);
            AssertScreen("Standby");
        }

        private IEnumerator CaptureOverlay(string name)
        {
            string folder = System.Environment.GetEnvironmentVariable("SILENT_SUNS_MENU_CAPTURES");
            if (string.IsNullOrEmpty(folder))
                yield break;
            // Batch-mode tests do not dispatch WaitForEndOfFrame.
            yield return null;
            Object.Destroy(CaptureGameplay(name));
        }

        private Vector2 ModalPoint(RectTransform rect, float across = .5f)
        {
            var camera = TerminalModal.Current.GetComponent<TerminalPauseDisplay>().SourceCamera;
            return camera.WorldToScreenPoint(rect.TransformPoint(new Vector2(
                Mathf.Lerp(rect.rect.xMin, rect.rect.xMax, across), rect.rect.center.y)));
        }

        private IEnumerator DragModalSlider(Slider slider)
        {
            Canvas.ForceUpdateCanvases();
            Vector2 start = ModalPoint(slider.handleRect);
            var pointer = new PointerEventData(EventSystem.current) { position = start };
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer, hits);
            Assert.That(hits.Count, Is.GreaterThan(0));
            Assert.That(hits[0].gameObject.GetComponentInParent<Slider>(), Is.SameAs(slider),
                "The visible modal handle must receive the click, not " + hits[0].gameObject.name);
            InputSystem.QueueStateEvent(mouse, new MouseState { position = start });
            yield return null;
            InputSystem.QueueStateEvent(mouse, new MouseState { position = start }.WithButton(MouseButton.Left));
            yield return null;
            var area = (RectTransform)slider.handleRect.parent;
            for (int i = 1; i <= 6; i++)
            {
                Vector2 next = Vector2.Lerp(start, ModalPoint(area, .8f), i / 6f);
                InputSystem.QueueStateEvent(mouse, new MouseState { position = next }.WithButton(MouseButton.Left));
                yield return null;
            }
            InputSystem.QueueStateEvent(mouse, new MouseState { position = ModalPoint(area, .8f) });
            yield return null;
            Assert.That(slider.normalizedValue, Is.EqualTo(.8f).Within(.08f), "Dragging must change the saved setting.");
        }

        private IEnumerator ClickModalButton(string name)
        {
            var button = TerminalModal.Current.GetComponentsInChildren<TerminalButton>().First(b => b.name == name);
            Vector2 point = ModalPoint((RectTransform)button.transform);
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = point }, hits);
            Assert.That(hits.Count, Is.GreaterThan(0));
            Assert.That(hits[0].gameObject, Is.SameAs(button.gameObject), name + " must be the top mouse target.");
            InputSystem.QueueStateEvent(mouse, new MouseState { position = point });
            yield return null;
            InputSystem.QueueStateEvent(mouse, new MouseState { position = point }.WithButton(MouseButton.Left));
            yield return null;
            InputSystem.QueueStateEvent(mouse, new MouseState { position = point });
            yield return null;
        }

        [UnityTest, Category("Menu")]
        public IEnumerator SettingsDraftsApplyDiscardAndCalibrationReset()
        {
            yield return ReadyMain();
            Click("Main Menu/Terminal Settings");
            for (int pass = 0; pass < 2; pass++)
            {
                var owner = pass == 0 ? MainSettings : Object.FindFirstObjectByType<TerminalPause>().GetComponentInChildren<TerminalSettings>();
                float music = GamePreferences.Music;
                GamePreferences.SetMusic(.21f);
                GamePreferences.SetOption("Brightness", .31f);
                GamePreferences.SetOption("Contrast", 1.3f);
                GamePreferences.SetOption("SafeArea", .09f);
                Assert.That(owner.HasChanges, Is.True);
                Assert.That(PlayerPrefs.GetFloat("SilentSuns.Options.Brightness", 0), Is.Zero);
                TerminalCalibration.Display();
                yield return null; yield return null;
                yield return ClickModalButton("RESET TO DEFAULT");
                Assert.That(GamePreferences.Brightness, Is.Zero);
                Assert.That(GamePreferences.Contrast, Is.EqualTo(1));
                yield return ClickModalButton("X");
                TerminalCalibration.Boundaries();
                yield return null; yield return null;
                var margin = TerminalModal.Current.GetComponentInChildren<Slider>();
                foreach (float edge in new[] { 0f, .12f })
                {
                    margin.value = edge;
                    yield return null; yield return null;
                    Canvas.ForceUpdateCanvases();
                    var preview = GameObject.Find("Screen boundary guides").transform.Find("Boundary preview");
                    Assert.That(preview.GetComponentsInChildren<TMP_Text>().Length, Is.Zero);
                    var points = new Vector3[4];
                    foreach (RectTransform bracket in preview)
                    {
                        Assert.That(bracket.GetComponentsInChildren<Image>().Length, Is.EqualTo(2));
                        bracket.GetWorldCorners(points);
                        foreach (var point in points)
                        {
                            Assert.That(point.x, Is.InRange(0, Screen.width));
                            Assert.That(point.y, Is.InRange(0, Screen.height));
                        }
                    }
                    preview.GetComponent<RectTransform>().GetWorldCorners(points);
                    Assert.That(points[0].x, Is.EqualTo(Screen.width - points[2].x).Within(1));
                    Assert.That(points[0].y, Is.EqualTo(Screen.height - points[2].y).Within(1));
                    if (edge == 0) yield return CaptureOverlay("boundary-corners-zero-" + pass);
                }
                yield return ClickModalButton("RESET TO DEFAULT");
                Assert.That(GamePreferences.SafeArea, Is.EqualTo(.03f));
                yield return ClickModalButton("X");
                Assert.That(GameObject.Find("Screen boundary guides"), Is.Null);
                if (pass == 0) terminal.GetComponent<TerminalMenu>().BackToMenu();
                else Object.FindFirstObjectByType<TerminalPause>().CloseSettings();
                yield return null; yield return null;
                yield return ClickModalButton("X");
                Assert.That(owner.gameObject.activeInHierarchy, Is.True, "Cancelling the question keeps the draft.");
                Assert.That(GamePreferences.Music, Is.EqualTo(.21f));
                if (pass == 0) terminal.GetComponent<TerminalMenu>().BackToMenu();
                else Object.FindFirstObjectByType<TerminalPause>().CloseSettings();
                yield return null; yield return null;
                yield return ClickModalButton("DISCARD CHANGES");
                Assert.That(GamePreferences.Music, Is.EqualTo(music));
                Assert.That(owner.gameObject.activeInHierarchy, Is.False);
                if (pass == 0)
                {
                    yield return SceneManager.LoadSceneAsync("SampleScene");
                    yield return null; yield return null;
                    var pause = Object.FindFirstObjectByType<TerminalPause>();
                    pause.Pause();
                    yield return new WaitForSecondsRealtime(.35f);
                    pause.OpenSettings();
                }
            }
        }

        [UnityTest, Category("Menu")]
        public IEnumerator ModifierConflictCanBeEditedThenDiscarded()
        {
            yield return ReadyMain();
            Click("Main Menu/Terminal Settings");
            MainSettings.ShowTab("KEYBINDS");
            var action = TerminalBindings.Controls.FindAction("Move");
            int up = action.bindings.ToList().FindIndex(b => b.path == "<Keyboard>/space");
            TerminalBindings.Rebind(action, up, _ => { });
            yield return new WaitForSecondsRealtime(.2f);
            yield return Press(Key.LeftCtrl);
            yield return new WaitForSecondsRealtime(.25f);
            Assert.That(action.bindings[up].effectivePath, Is.EqualTo("<Keyboard>/leftCtrl"));
            Assert.That(TerminalBindings.ConflictWarning(), Does.Contain("MOVE UP").And.Contain("MOVE DOWN"));
            GamePreferences.SetMusic(.1f);
            Assert.That(MainSettings.ApplyChanges(), Is.False);
            Assert.That(PlayerPrefs.GetFloat("SilentSuns.Audio.Music", .65f), Is.EqualTo(.65f));
            MainSettings.DiscardChanges();
            Assert.That(action.bindings[up].effectivePath, Is.EqualTo("<Keyboard>/space"));
            Assert.That(TerminalBindings.ConflictWarning(), Is.Empty);
            Assert.That(MainSettings.HasChanges, Is.False);
        }

        [UnityTest, Category("Menu")]
        public IEnumerator ModalPointerWorksFromMainMenuAndPause()
        {
            yield return ReadyMain();
            Click("Main Menu/Terminal Settings");
            for (int pass = 0; pass < 2; pass++)
            {
                TerminalCalibration.Boundaries();
                yield return null; yield return null;
                var margin = TerminalModal.Current.GetComponentInChildren<Slider>();
                yield return DragModalSlider(margin);
                Assert.That(GamePreferences.SafeArea, Is.EqualTo(margin.value));
                yield return ClickModalButton("X");
                Assert.That(TerminalModal.Current, Is.Null);
                TerminalCalibration.Display();
                yield return null; yield return null;
                foreach (var slider in TerminalModal.Current.GetComponentsInChildren<Slider>())
                    yield return DragModalSlider(slider);
                yield return CaptureOverlay("modal-calibration-" + pass);
                yield return ClickModalButton("X");
                // Use the current mode so this input test does not change the editor's Game view.
                string[] keys = { "SilentSuns.Display.Width", "SilentSuns.Display.Height", "SilentSuns.Display.Mode" };
                bool[] had = keys.Select(PlayerPrefs.HasKey).ToArray();
                int[] saved = keys.Select(k => PlayerPrefs.GetInt(k)).ToArray();
                try
                {
                    TerminalDisplayOptions.Apply(Screen.width, Screen.height, Screen.fullScreenMode);
                    yield return null; yield return null;
                    yield return ClickModalButton("KEEP THIS MODE");
                    Assert.That(TerminalDisplayOptions.Pending, Is.False);
                    TerminalDisplayOptions.Apply(Screen.width, Screen.height, Screen.fullScreenMode);
                    yield return null; yield return null;
                    yield return ClickModalButton("REVERT");
                    Assert.That(TerminalDisplayOptions.Pending, Is.False);
                }
                finally
                {
                    for (int i = 0; i < keys.Length; i++)
                        if (had[i]) PlayerPrefs.SetInt(keys[i], saved[i]); else PlayerPrefs.DeleteKey(keys[i]);
                }
                if (pass == 0)
                {
                    // Test the same popups after loading gameplay and opening pause settings.
                    yield return SceneManager.LoadSceneAsync("SampleScene");
                    yield return null; yield return null;
                    var pause = Object.FindFirstObjectByType<TerminalPause>();
                    pause.Pause();
                    yield return new WaitForSecondsRealtime(.35f);
                    pause.OpenSettings();
                    yield return null;
                }
            }
        }

        [UnityTest, Category("Menu")]
        public IEnumerator TabSearchRemapConflictAndPersistence()
        {
            yield return ReadyMain();
            Click("Main Menu/Terminal Settings");
            var search = MainSettings.GetComponentInChildren<TMP_InputField>();
            search.text = "sens";
            yield return null;
            Assert.That(MainSettings.GetComponentsInChildren<Slider>().Length, Is.EqualTo(2));
            search.text = "SENS";
            Assert.That(MainSettings.GetComponentsInChildren<Slider>().Length, Is.EqualTo(2));
            MainSettings.ShowTab("KEYBINDS");
            var action = TerminalBindings.Controls.FindAction("Move");
            int forward = action.bindings.ToList().FindIndex(b => b.path == "<Keyboard>/w");
            string result = "";
            TerminalBindings.Rebind(action, forward, s => result = s);
            yield return new WaitForSecondsRealtime(.2f);
            yield return Press(Key.S);
            yield return new WaitForSecondsRealtime(.25f);
            Assert.That(result, Does.Contain("CONFLICT"));
            Assert.That(action.bindings[forward].effectivePath, Is.EqualTo("<Keyboard>/s"));
            Assert.That(MainSettings.ApplyChanges(), Is.False);
            Assert.That(PlayerPrefs.HasKey("SilentSuns.Input.Bindings"), Is.False);
            var warning = MainSettings.transform.Find("Tabbed settings/Changes status").GetComponent<TMP_Text>();
            Assert.That(warning.text, Does.Contain("MOVE FORWARD").And.Contain("MOVE BACKWARD"));
            Color firstColor = warning.color;
            yield return new WaitForSecondsRealtime(.51f);
            Assert.That(warning.color, Is.EqualTo(firstColor).And.EqualTo(TerminalUI.Conflict), "Conflicts must stay solid red.");
            Assert.That(warning.text, Is.EqualTo("You have conflicting keybinds:\n\nMOVE FORWARD (ON FOOT + IN SHIP) + MOVE BACKWARD (ON FOOT + IN SHIP)"));
            Capture("settings-binding-conflict", 1920, 1080);
            terminal.GetComponent<TerminalMenu>().BackToMenu();
            yield return null; yield return null;
            Assert.That(TerminalModal.Current.GetComponentsInChildren<TerminalButton>().First(b => b.name == "APPLY SETTINGS").interactable, Is.False);
            yield return ClickModalButton("X");
            AssertScreen("Settings");
            TerminalBindings.Rebind(action, forward, s => result = s);
            yield return new WaitForSecondsRealtime(.2f);
            yield return Press(Key.Z);
            yield return new WaitForSecondsRealtime(.25f);
            Assert.That(result, Does.Contain("APPLY TO SAVE"));
            Assert.That(PlayerPrefs.HasKey("SilentSuns.Input.Bindings"), Is.False);
            Assert.That(MainSettings.ApplyChanges(), Is.True);
            var copy = Object.Instantiate(TerminalBindings.Controls);
            copy.RemoveAllBindingOverrides();
            copy.LoadBindingOverridesFromJson(PlayerPrefs.GetString("SilentSuns.Input.Bindings"));
            Assert.That(copy.FindAction("Move").bindings[forward].effectivePath, Is.EqualTo("<Keyboard>/z"));
            Object.Destroy(copy);
            GamePreferences.SetMusic(.27f);
            GamePreferences.SetOption("Brightness", .22f);
            TerminalSettings.ResetTab("KEYBINDS");
            Assert.That(action.bindings[forward].effectivePath, Is.EqualTo("<Keyboard>/w"));
            Assert.That(GamePreferences.Music, Is.EqualTo(.27f));
            Assert.That(GamePreferences.Brightness, Is.EqualTo(.22f));
            MainSettings.ShowTab("AUDIO");
            yield return new WaitForSecondsRealtime(.2f);
            Capture("polish-audio", 1920, 1080);
            MainSettings.ShowTab("GENERAL");
            yield return new WaitForSecondsRealtime(.2f);
            Capture("polish-general", 1920, 1080);
            MainSettings.ShowTab("VIDEO");
            yield return new WaitForSecondsRealtime(.2f);
            Capture("polish-video", 1366, 768);
            MainSettings.ShowTab("KEYBINDS");
            yield return new WaitForSecondsRealtime(.2f);
            Capture("polish-keybinds", 2560, 1440);
            GamePreferences.SetOption("TextScale", 1.25f);
            MainSettings.ShowTab("GENERAL");
            yield return new WaitForSecondsRealtime(.2f);
            Capture("polish-large-text-ultrawide", 2560, 1080);
        }

        [UnityTest, Category("Menu")]
        public IEnumerator ConflictingKeysStayRedAndMovementContextsAreClear()
        {
            yield return ReadyMain();
            Click("Main Menu/Operator Guide");
            Assert.That(terminal.Find("Operator Guide/Control descriptions").GetComponent<TMP_Text>().text,
                Does.Contain("SPRINT (ON FOOT)").And.Contain("ASCEND / DESCEND (IN SHIP)").And.Contain("MOVE (ON FOOT + IN SHIP)"));
            Capture("movement-context-guide", 1920, 1080);
            yield return Press(Key.Escape);
            Click("Main Menu/Terminal Settings");
            for (int pass = 0; pass < 2; pass++)
            {
                var owner = pass == 0 ? MainSettings : Object.FindFirstObjectByType<TerminalPause>().GetComponentInChildren<TerminalSettings>();
                owner.ShowTab("KEYBINDS");
                yield return null;
                var action = TerminalBindings.Controls.FindAction("Move");
                int up = action.bindings.ToList().FindIndex(b => b.path == "<Keyboard>/space");
                action.ApplyBindingOverride(up, "<Keyboard>/leftCtrl");
                yield return new WaitForSecondsRealtime(.2f);
                var content = owner.GetComponentInChildren<TerminalScroll>().content;
                var warning = owner.transform.Find("Tabbed settings/Changes status").GetComponent<TMP_Text>();
                Assert.That(warning.text, Is.EqualTo("You have conflicting keybinds:\n\nMOVE UP (IN SHIP) + MOVE DOWN (IN SHIP)"));
                Assert.That(warning.color, Is.EqualTo(TerminalUI.Conflict));
                Assert.That(owner.transform.Find("Tabbed settings/Setting description").gameObject.activeSelf, Is.False);
                Assert.That(owner.CanApply, Is.False);
                var buttons = content.GetComponentsInChildren<TerminalButton>();
                foreach (var button in buttons.Where(b => b.name.StartsWith("KEY / MOVE UP") || b.name.StartsWith("KEY / MOVE DOWN")))
                {
                    var label = button.transform.Find("Label").GetComponent<TMP_Text>();
                    Assert.That(label.text, Does.Contain("<color=#" + ColorUtility.ToHtmlStringRGB(TerminalUI.Conflict) + ">"));
                    ExecuteEvents.Execute(button.gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerEnterHandler);
                    label.ForceMeshUpdate();
                    int keyStart = label.textInfo.characterCount - (button.name.StartsWith("KEY / MOVE UP") ? 4 : 7);
                    Color32 color = label.textInfo.characterInfo[keyStart].color;
                    Assert.That(color.r, Is.EqualTo(((Color32)TerminalUI.Conflict).r), "Hover must keep the actual key red.");
                    Assert.That(color.g, Is.EqualTo(((Color32)TerminalUI.Conflict).g));
                    ExecuteEvents.Execute(button.gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerExitHandler);
                }
                yield return new WaitForSecondsRealtime(.6f);
                Assert.That(warning.color, Is.EqualTo(TerminalUI.Conflict));
                yield return CaptureOverlay("steady-binding-conflicts-" + pass);

                foreach (bool controller in new[] { false, true })
                {
                    owner.ShowBindingDevice(controller);
                    yield return null;
                    var names = content.Cast<Transform>().Where(t => t.gameObject.activeSelf).Select(t => t.name).ToList();
                    int ship = names.IndexOf("IN SHIP");
                    int foot = names.IndexOf("ON FOOT");
                    int shared = names.IndexOf("ON FOOT + IN SHIP");
                    Assert.That(foot, Is.EqualTo(0));
                    Assert.That(ship, Is.GreaterThan(foot));
                    Assert.That(shared, Is.GreaterThan(ship));
                    var onFoot = names.Skip(foot + 1).Take(ship - foot - 1).ToArray();
                    var inShip = names.Skip(ship + 1).Take(shared - ship - 1).ToArray();
                    var inBoth = names.Skip(shared + 1).ToArray();
                    Assert.That(inShip.Any(n => n.Contains("SPRINT")), Is.False);
                    Assert.That(inShip.Any(n => n.Contains("MOVE UP (IN SHIP)")), Is.True);
                    Assert.That(inShip.Any(n => n.Contains("MOVE DOWN (IN SHIP)")), Is.True);
                    Assert.That(onFoot.Any(n => n.Contains("SPRINT (ON FOOT)")), Is.True);
                    Assert.That(onFoot.Any(n => n.Contains("MOVE UP") || n.Contains("MOVE DOWN") || n.Contains("ROLL") || n.Contains("REORIENT")), Is.False);
                    foreach (string direction in new[] { "FORWARD", "BACKWARD", "LEFT", "RIGHT" })
                    {
                        string label = "MOVE " + direction + " (ON FOOT + IN SHIP)";
                        Assert.That(names.Count(n => n.Contains(label)), Is.EqualTo(1));
                        Assert.That(inBoth.Any(n => n.Contains(label)), Is.True);
                    }
                    Assert.That(inBoth.Count(n => n.Contains("INTERACT")), Is.EqualTo(1));
                    Assert.That(names.Count - 3, Is.EqualTo(TerminalBindings.Editable().Count(
                        b => TerminalBindings.IsController(b.action, b.index) == controller)),
                        "Each editable binding should have exactly one row for the selected device.");
                    content.GetComponentInParent<TerminalScroll>().verticalNormalizedPosition = 0;
                    yield return null;
                    yield return CaptureOverlay("shared-controls-" + pass + (controller ? "-controller" : "-keyboard"));
                }
                owner.ShowBindingDevice(false);
                owner.DiscardChanges();
                yield return new WaitForSecondsRealtime(.2f);
                Assert.That(owner.CanApply, Is.True);
                Assert.That(owner.transform.Find("Tabbed settings/Setting description").gameObject.activeSelf, Is.True);
                Assert.That(warning.text, Does.Not.Contain("conflicting"));
                Assert.That(content.GetComponentsInChildren<TerminalButton>().All(b => !b.transform.Find("Label").GetComponent<TMP_Text>().text.Contains("<color=")), Is.True);
                if (pass == 0)
                {
                    yield return SceneManager.LoadSceneAsync("SampleScene");
                    yield return null; yield return null;
                    var pause = Object.FindFirstObjectByType<TerminalPause>();
                    pause.Pause();
                    yield return new WaitForSecondsRealtime(.35f);
                    pause.OpenSettings();
                }
            }
        }

        [UnityTest, Category("Menu")]
        public IEnumerator KeybindDeviceTabsFilterAndResetIndependently()
        {
            yield return ReadyMain();
            Click("Main Menu/Terminal Settings");
            var search = MainSettings.GetComponentInChildren<TMP_InputField>();
            foreach (string removed in new[] { "VISUAL CONTROLS", "CREDITS", "SCREENSHOTS" })
            {
                search.text = removed;
                Assert.That(MainSettings.transform.Find("Tabbed settings/Title").GetComponent<TMP_Text>().text, Does.EndWith("/ 0"));
            }
            MainSettings.ShowTab("KEYBINDS");
            yield return null;
            Assert.That(MainSettings.transform.Find("Tabbed settings/Setting description").GetComponent<TMP_Text>().text,
                Does.Not.Contain("NO MATCHES"));
            var content = MainSettings.GetComponentInChildren<TerminalScroll>().content;
            Assert.That(content.GetComponentsInChildren<TerminalButton>().All(b => b.name.StartsWith("KEY / ")), Is.True);
            Assert.That(content.GetComponentsInChildren<TerminalButton>().Length, Is.GreaterThan(0));
            Capture("keybinds-keyboard", 1920, 1080);
            Click(MainSettings.transform.Find("Tabbed settings/Binding devices/CONTROLLER").gameObject);
            yield return null;
            Assert.That(content.GetComponentsInChildren<TerminalButton>().All(b => b.name.StartsWith("PAD / ")), Is.True);
            Assert.That(content.GetComponentsInChildren<TerminalButton>().Length, Is.GreaterThan(0));
            Capture("keybinds-controller", 1920, 1080);
            var pairs = TerminalBindings.Editable().ToList();
            var keyboardBinding = pairs.First(p => !TerminalBindings.IsController(p.action, p.index));
            var controllerBinding = pairs.First(p => TerminalBindings.IsController(p.action, p.index));
            keyboardBinding.action.ApplyBindingOverride(keyboardBinding.index, "<Keyboard>/z");
            controllerBinding.action.ApplyBindingOverride(controllerBinding.index, "<Gamepad>/rightShoulder");
            var reset = MainSettings.GetComponentsInChildren<TerminalButton>().First(b => b.name == "RESET TAB");
            Click(reset.gameObject);
            yield return null; yield return null;
            yield return ClickModalButton("RESET CONTROLLER");
            Assert.That(controllerBinding.action.bindings[controllerBinding.index].overridePath, Is.Null);
            Assert.That(keyboardBinding.action.bindings[keyboardBinding.index].effectivePath, Is.EqualTo("<Keyboard>/z"));
            Click(MainSettings.GetComponentsInChildren<TerminalButton>().First(b => b.name == "KEYBOARD / MOUSE").gameObject);
            yield return null;
            Assert.That(content.GetComponentsInChildren<TerminalButton>().All(b => b.name.StartsWith("KEY / ")), Is.True);
        }

        [UnityTest, Category("Menu")]
        public IEnumerator IdleChoosesBothAndWakeDoesNotActivateMenu()
        {
            yield return ReadyMain();
            var saver = terminal.GetComponent<TerminalScreensaver>();
            var saved = Random.state;
            Random.InitState(4725);
            bool ship = false, pipes = false;
            for (int i = 0; i < 20; i++)
            {
                saver.StartSaver();
                var shuttle = terminal.Find("Idle screensaver/Space shuttle");
                ship |= shuttle.gameObject.activeSelf;
                pipes |= !shuttle.gameObject.activeSelf;
            }
            Random.state = saved;
            Assert.That(ship && pipes, Is.True);
            yield return new WaitForSecondsRealtime(1);
            Capture("polish-screensaver", 1920, 1080);
            var art = terminal.Find("Idle screensaver/Space shuttle").GetComponent<RectTransform>();
            for (int i = 0; i < 40 && !art.gameObject.activeSelf; i++)
                saver.StartSaver();
            art.anchoredPosition = Vector2.one * 10000;
            yield return null;
            var direction = (Vector2)saver.GetType().GetField("velocity", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(saver);
            Assert.That(Vector2.Dot(art.right, direction), Is.GreaterThan(.99f), "The shuttle's nose follows its reflected travel direction.");
            for (int i = 0; i < 40 && art.gameObject.activeSelf; i++)
                saver.StartSaver();
            yield return new WaitForSecondsRealtime(6);
            var pipeGraphic = terminal.GetComponentInChildren<TerminalPipes>();
            Assert.That(pipeGraphic.enabled, Is.True);
            Capture("polish-pipes", 1920, 1080, inspect: (image, camera) =>
            {
                int lit = 0;
                for (int y = 100; y < 980; y += 2)
                    for (int x = 380; x < 1540; x += 2)
                        if (image.GetPixel(x, y).b > .15f)
                            lit++;
                Assert.That(lit, Is.GreaterThan(100), "Pipe geometry must actually reach the screen.");
            });
            yield return Press(Key.Enter);
            yield return new WaitForSecondsRealtime(.4f);
            Assert.That(saver.Showing, Is.False);
            Assert.That(terminal.GetComponent<TerminalMenu>().State, Is.EqualTo(TerminalMenu.PowerState.MainMenu));
            Assert.That(TerminalShipIntro.Ready, Is.True);
            Assert.That(terminal.GetComponent<TerminalBootLog>().Running, Is.False);
        }

        [UnityTest, Category("Menu")]
        public IEnumerator BuildCodeWorksAndEscapeOnlyClosesModal()
        {
            yield return ReadyMain();
            yield return Press(Key.Digit4);
            yield return Press(Key.Numpad7);
            yield return Press(Key.Digit2);
            yield return Press(Key.Numpad5);
            Assert.That(TerminalModal.Current, Is.Not.Null);
            Assert.That(TerminalModal.Current.Message.text, Does.Contain("VERSION").And.Contain("BUILD").And.Contain("EST"));
            yield return CaptureOverlay("polish-build-info");
            yield return Press(Key.Escape);
            Assert.That(TerminalModal.Current, Is.Null);
            AssertScreen("Main Menu");
            Click("Main Menu/Terminal Settings");
            var search = MainSettings.GetComponentInChildren<TMP_InputField>();
            EventSystem.current.SetSelectedGameObject(search.gameObject);
            yield return Press(Key.Digit4);
            yield return Press(Key.Digit7);
            yield return Press(Key.Digit2);
            yield return Press(Key.Digit5);
            Assert.That(TerminalModal.Current, Is.Null, "Typing in search must not open build info.");
        }

        [UnityTest, Category("Menu")]
        public IEnumerator InterfaceVolumeIsIndependentAndDisplayRollbackWorks()
        {
            yield return ReadyMain();
            GamePreferences.SetFX(0);
            GamePreferences.SetOption("Interface", .4f);
            AssertMixer("FXVolume", 0);
            AssertMixer("InterfaceVolume", .4f);
            int width = Screen.width, height = Screen.height;
            var mode = Screen.fullScreenMode;
            TerminalDisplayOptions.Apply(width, height, mode);
            Assert.That(TerminalDisplayOptions.Pending, Is.True);
            TerminalModal.Current.Close();
            Assert.That(TerminalDisplayOptions.Pending, Is.False);
            yield return null;
            Assert.That(Screen.fullScreenMode, Is.EqualTo(mode));
            TerminalDisplayOptions.Apply(width, height, mode);
            yield return new WaitForSecondsRealtime(15.3f);
            Assert.That(TerminalDisplayOptions.Pending, Is.False, "Unconfirmed modes must revert without input.");
        }

        [UnityTest, Category("Menu")]
        public IEnumerator FailureScreenRestartSkipsIntro()
        {
            yield return SceneManager.LoadSceneAsync("GameOverScene");
            yield return null;
            Assert.That(TerminalFailure.Active, Is.True);
            yield return new WaitForSecondsRealtime(.3f);
            yield return CaptureOverlay("polish-game-over");
            var restart = Object.FindObjectsByType<TerminalButton>(FindObjectsSortMode.None).First(b => b.name.Contains("RESTART SURVEY"));
            restart.onClick.Invoke();
            yield return new WaitForSecondsRealtime(2);
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("SampleScene"));
            Assert.That(TerminalShipIntro.IsActive, Is.False);
            Assert.That(TerminalFailure.Active, Is.False);
        }

        [UnityTest, Category("Menu")]
        public IEnumerator SettingsScrollEasesAndDisplayDropdownApplies()
        {
            yield return ReadyMain();
            Click("Main Menu/Terminal Settings");
            yield return null;
            Assert.That(MainSettings.transform.Find("Settings border").GetComponent<Image>().color, Is.EqualTo(TerminalUI.Ink));
            var scroll = MainSettings.GetComponentInChildren<TerminalScroll>();
            Canvas.ForceUpdateCanvases();
            scroll.ResetPosition();
            scroll.OnScroll(new PointerEventData(EventSystem.current) { scrollDelta = new Vector2(0, -120) });
            Assert.That(scroll.content.anchoredPosition.y, Is.Zero, "Wheel input must not jump the content.");
            yield return new WaitForSecondsRealtime(.08f);
            Assert.That(scroll.content.anchoredPosition.y, Is.GreaterThan(1).And.LessThan(105));
            yield return new WaitForSecondsRealtime(.7f);
            Assert.That(scroll.content.anchoredPosition.y, Is.EqualTo(105).Within(1));
            MainSettings.ShowTab("VIDEO");
            yield return null;
            Assert.That(scroll.content.anchoredPosition.y, Is.Zero, "Changing tabs clears leftover scrolling.");

            var slider = MainSettings.GetComponentsInChildren<Slider>().First(s => s.transform.parent.name == "FRAME LIMIT");
            GamePreferences.SetOption("VSync", 1);
            TerminalDisplayOptions.ApplyFrameRate();
            Assert.That(slider.interactable, Is.False);
            Assert.That(Application.targetFrameRate, Is.EqualTo(-1));
            GamePreferences.SetOption("VSync", 0);
            TerminalDisplayOptions.ApplyFrameRate();
            Assert.That(slider.interactable, Is.True);
            slider.value = 90;
            Assert.That(QualitySettings.vSyncCount, Is.Zero);
            Assert.That(Application.targetFrameRate, Is.EqualTo(90));
            GamePreferences.SetOption("VSync", 1);
            TerminalDisplayOptions.ApplyFrameRate();
            Assert.That(slider.interactable, Is.False);
            Assert.That(GamePreferences.Read("FrameCap", 120, 30, 240), Is.EqualTo(90), "VSync should keep the player's saved cap.");

            var search = MainSettings.GetComponentInChildren<TMP_InputField>();
            search.text = "window mode";
            yield return null;
            var mode = MainSettings.GetComponentsInChildren<TerminalButton>().First(b => b.name.StartsWith("WINDOW MODE:"));
            string originalModeLabel = mode.transform.Find("Label").GetComponent<TMP_Text>().text;
            Click(mode.gameObject);
            yield return null;
            Assert.That(TerminalDropdown.Current, Is.Not.Null);
            Assert.That(TerminalDropdown.Current.GetComponentsInChildren<TerminalButton>().Length, Is.EqualTo(3));
            yield return CaptureOverlay("polish-window-dropdown");
            yield return Press(Key.Escape);
            Assert.That(TerminalDropdown.Current, Is.Null);
            AssertScreen("Settings");
            Click(mode.gameObject);
            yield return null;
            var windowed = TerminalDropdown.Current.GetComponentsInChildren<TerminalButton>().First(b => b.name.Trim().TrimStart('>').Trim() == "WINDOWED");
            Click(windowed.gameObject);
            yield return null;
            Assert.That(TerminalDisplayOptions.Pending, Is.True, "Choosing a mode must apply it without another Apply button.");
            Assert.That(TerminalModal.Current.Message.text, Does.Contain("WINDOWED"));
            TerminalModal.Current.Close();
            yield return null;
            Assert.That(mode.transform.Find("Label").GetComponent<TMP_Text>().text, Is.EqualTo(originalModeLabel));
            search.text = "resolution";
            yield return null;
            var resolution = MainSettings.GetComponentsInChildren<TerminalButton>().First(b => b.name.StartsWith("RESOLUTION:"));
            Click(resolution.gameObject);
            yield return null;
            var choices = TerminalDropdown.Current.GetComponentsInChildren<TerminalButton>();
            Assert.That(choices.Length, Is.GreaterThanOrEqualTo(1));
            yield return new WaitForSecondsRealtime(.7f);
            yield return CaptureOverlay("polish-resolution-dropdown");
            Click(EventSystem.current.currentSelectedGameObject);
            yield return null;
            Assert.That(TerminalDisplayOptions.Pending, Is.True);
            TerminalModal.Current.Close();
            yield return null;
        }

        [UnityTest, Category("Menu")]
        public IEnumerator SettingsAndCalibrationStayClearAndRestoreEffects()
        {
            yield return ReadyMain();
            var menu = terminal.GetComponent<TerminalMenu>();
            var screen = terminal.GetComponentInChildren<TerminalDisplay>();
            float intensity = GamePreferences.CRTIntensity;
            Assert.That(TerminalSettings.EffectsSuspended, Is.False);
            Click("Main Menu/Terminal Settings");
            yield return null;
            yield return null;
            Assert.That(screen.EffectMaterial.GetFloat("_ClearDisplay"), Is.EqualTo(1));
            var volume = Object.FindFirstObjectByType<TerminalExtras>().GetComponent<Volume>();
            Assert.That(volume.profile.TryGet(out Bloom bloom), Is.True);
            Assert.That(bloom.active, Is.True);
            Assert.That(bloom.intensity.value, Is.Zero);
            GamePreferences.SetOption("Brightness", .2f);
            GamePreferences.SetOption("Contrast", 1.2f);
            yield return null;
            Assert.That(volume.profile.TryGet(out ColorAdjustments colors), Is.True);
            Assert.That(colors.postExposure.value, Is.EqualTo(.4f).Within(.001f));
            Assert.That(colors.contrast.value, Is.EqualTo(20).Within(.001f));

            TerminalCalibration.Display();
            yield return null;
            yield return null;
            var overlay = TerminalModal.Current.GetComponent<TerminalPauseDisplay>();
            var material = overlay.Presentation.GetComponentInChildren<RawImage>().material;
            Assert.That(material.GetFloat("_ClearDisplay"), Is.EqualTo(1));
            Assert.That(material.GetFloat("_DisplayBrightness"), Is.EqualTo(.2f));
            Assert.That(material.GetFloat("_DisplayContrast"), Is.EqualTo(1.2f));
            AssertClearCalibrationPicture(material);
            yield return CaptureOverlay("polish-calibration-clear");
            TerminalModal.Current.Close();
            yield return null;
            Assert.That(TerminalSettings.EffectsSuspended, Is.True, "Closing calibration must leave its parent Settings clear.");
            Assert.That(MainSettings.ApplyChanges(), Is.True);
            menu.BackToMenu();
            yield return null;
            yield return null;
            Assert.That(TerminalSettings.EffectsSuspended, Is.False);
            Assert.That(screen.EffectMaterial.GetFloat("_ClearDisplay"), Is.Zero);
            Assert.That(bloom.active, Is.False, "The scene's own bloom should take over again.");
            Assert.That(GamePreferences.CRTIntensity, Is.EqualTo(intensity));

            // Calibration also works when opened directly, outside Settings.
            TerminalCalibration.Boundaries();
            yield return null;
            Assert.That(TerminalSettings.EffectsSuspended, Is.True);
            TerminalModal.Current.Close();
            Assert.That(TerminalSettings.EffectsSuspended, Is.False);

            yield return SceneManager.LoadSceneAsync("SampleScene");
            yield return null;
            var pause = Object.FindFirstObjectByType<TerminalPause>();
            pause.Pause();
            yield return new WaitForSecondsRealtime(.3f);
            pause.OpenSettings();
            yield return null;
            yield return null;
            var pauseMaterial = pause.GetComponent<TerminalPauseDisplay>().Presentation.GetComponentInChildren<RawImage>().material;
            Assert.That(pauseMaterial.GetFloat("_ClearDisplay"), Is.EqualTo(1));
            pause.CloseSettings();
            yield return null;
            yield return null;
            Assert.That(pauseMaterial.GetFloat("_ClearDisplay"), Is.Zero);
            Assert.That(GamePreferences.CRTIntensity, Is.EqualTo(intensity));
        }

        // A flat reference patch must stay flat even when the normal shader's faults are active.
        private void AssertClearCalibrationPicture(Material source)
        {
            var material = new Material(source);
            var input = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
            input.SetPixels(new[] { Color.gray, Color.gray, Color.gray, Color.gray });
            input.Apply();
            var output = new RenderTexture(128, 128, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            var pixels = new Texture2D(128, 128, TextureFormat.RGBA32, false, true);
            var previous = RenderTexture.active;
            try
            {
                material.SetFloat("_DisplayBrightness", 0);
                material.SetFloat("_DisplayContrast", 1);
                material.SetFloat("_DiagnosticGlitch", 1);
                material.SetFloat("_DisplayTime", 39);
                material.SetFloat("_Noise", 1);
                material.SetFloat("_Glow", 1);
                output.Create();
                Graphics.Blit(input, output, material);
                RenderTexture.active = output;
                pixels.ReadPixels(new Rect(0, 0, 128, 128), 0, 0);
                pixels.Apply();
                float grey = pixels.GetPixel(64, 64).r;
                Assert.That(grey, Is.InRange(.49f, .51f));
                foreach (Color pixel in pixels.GetPixels())
                    Assert.That(pixel.r, Is.EqualTo(grey).Within(.005f), "Calibration should have no scanlines, noise, vignette, or glow.");
                material.SetFloat("_DisplayBrightness", .2f);
                material.SetFloat("_DisplayContrast", 1.2f);
                Graphics.Blit(input, output, material);
                RenderTexture.active = output;
                pixels.ReadPixels(new Rect(0, 0, 128, 128), 0, 0);
                pixels.Apply();
                Assert.That(pixels.GetPixel(64, 64).r, Is.GreaterThan(grey + .1f), "Brightness and contrast must still change the reference.");
            }
            finally
            {
                RenderTexture.active = previous;
                output.Release();
                Object.Destroy(output);
                Object.Destroy(input);
                Object.Destroy(pixels);
                Object.Destroy(material);
            }
        }

        [UnityTest, Category("Menu")]
        public IEnumerator CalibrationPanelsStayInsideModal()
        {
            yield return ReadyMain();
            TerminalCalibration.Display();
            yield return null;
            yield return CaptureOverlay("polish-calibration");
            TerminalModal.Current.Close();
            TerminalCalibration.Controller();
            InputSystem.QueueStateEvent(gamepad, new GamepadState { leftStick = new Vector2(.6f, .4f) });
            yield return null;
            yield return CaptureOverlay("polish-controller-calibration");
            yield return Press(Key.Escape);
            AssertScreen("Main Menu");
        }

        [UnityTest, Category("Menu")]
        public IEnumerator ControllerDisconnectPausesAndSettingsAffectLook()
        {
            yield return SceneManager.LoadSceneAsync("SampleScene");
            yield return null;
            // Gameplay is in Assembly-CSharp; this test assembly cannot reference it directly.
            var input = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).First(b => b.GetType().Name == "InputHandler");
            var inputType = input.GetType();
            GamePreferences.SetOption("DeadZone", .25f);
            InputSystem.QueueStateEvent(gamepad, new GamepadState { rightStick = new Vector2(0, .2f) });
            yield return null;
            Assert.That(((Vector2)inputType.GetProperty("lookInput").GetValue(input)).magnitude, Is.LessThan(.001f));
            GamePreferences.SetOption("InvertLook", 1);
            GamePreferences.SetOption("ControllerSensitivity", 1.8f);
            InputSystem.QueueStateEvent(gamepad, new GamepadState { rightStick = new Vector2(0, .8f) });
            yield return null;
            Assert.That(((Vector2)inputType.GetProperty("lookInput").GetValue(input)).y, Is.LessThan(0));
            Assert.That((float)inputType.GetProperty("lookSensitivity").GetValue(input), Is.EqualTo(1.8f));
            InputSystem.RemoveDevice(gamepad);
            gamepad = null;
            yield return null;
            Assert.That(TerminalPause.Paused, Is.True);
            Assert.That(TerminalModal.Current.Message.text, Does.Contain("CONNECTION LOST"));
            yield return CaptureOverlay("polish-disconnected");
            yield return Press(Key.Escape);
            Assert.That(TerminalPause.Paused, Is.True, "Dismissing the notice must not resume gameplay.");
        }
    }
}
