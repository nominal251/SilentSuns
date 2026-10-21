using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Audio;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.DualShock;
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
        private Transform terminal;
        private Keyboard keyboard;
        private Mouse mouse;
        private Gamepad gamepad;
        private InputSettings originalInputSettings;
        private InputSettings testInputSettings;
        private HideFlags originalInputFlags;
        private const string VolumeKey = "SilentSuns.Menu.Volume";
        private const string MotionKey = "SilentSuns.Menu.ReduceMotion";
        private const string BootKey = "SilentSuns.Menu.PlayBoot";
        private bool hadVolume;
        private bool hadMotion;
        private bool hadBoot;
        private float oldVolume;
        private int oldMotion;
        private int oldBoot;

        private readonly Dictionary<string, float> savedFloats = new Dictionary<string, float>();
        private bool hadCompleted;
        private int oldCompleted;

        // Keep these checks independent of the user's saved menu preferences.
        [UnitySetUp]
        public IEnumerator OpenMenu()
        {
            foreach (string key in new[] { "SilentSuns.Audio.FX", "SilentSuns.Audio.Music", "SilentSuns.Input.MouseSensitivity" })
            {
                if (PlayerPrefs.HasKey(key)) savedFloats[key] = PlayerPrefs.GetFloat(key);
                PlayerPrefs.DeleteKey(key);
            }
            hadCompleted = PlayerPrefs.HasKey("SilentSuns.Progress.Completed");
            oldCompleted = PlayerPrefs.GetInt("SilentSuns.Progress.Completed");
            PlayerPrefs.DeleteKey("SilentSuns.Progress.Completed");
            hadVolume = PlayerPrefs.HasKey(VolumeKey);
            hadMotion = PlayerPrefs.HasKey(MotionKey);
            hadBoot = PlayerPrefs.HasKey(BootKey);
            oldVolume = PlayerPrefs.GetFloat(VolumeKey);
            oldMotion = PlayerPrefs.GetInt(MotionKey);
            oldBoot = PlayerPrefs.GetInt(BootKey);
            PlayerPrefs.SetFloat(VolumeKey, 0.65f);
            PlayerPrefs.SetInt(MotionKey, 0);
            PlayerPrefs.SetInt(BootKey, 1);
            originalInputSettings = InputSystem.settings;
            originalInputFlags = originalInputSettings.hideFlags;
            originalInputSettings.hideFlags = HideFlags.DontUnloadUnusedAsset;
            testInputSettings = Object.Instantiate(originalInputSettings);
            testInputSettings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            testInputSettings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            InputSystem.settings = testInputSettings;
            keyboard = InputSystem.AddDevice<Keyboard>();
            mouse = InputSystem.AddDevice<Mouse>();
            gamepad = InputSystem.AddDevice<Gamepad>();
            SceneManager.sceneLoaded += IsolateUnwiredTrackers;
            yield return SceneManager.LoadSceneAsync("MainMenu");
            float startupDeadline = Time.realtimeSinceStartup + 30;
            while (!TerminalShipIntro.Ready && Time.realtimeSinceStartup < startupDeadline) yield return null;
            Assert.That(TerminalShipIntro.Ready, Is.True);
            yield return null;
            terminal = GameObject.Find("ARCA Terminal").transform;
            // Most checks start at the usable invitation; arrival checks and the recording include cold power-up.
            string test = TestContext.CurrentContext.Test.Name;
            if (test != "ColdScreenWarmsBeforeInvitation" && test != "RecordCommandBootWalkthrough")
                yield return WaitForInvitation();
        }

        // Restore preferences even when a test fails.
        [TearDown]
        public void RestorePreferences()
        {
            SceneManager.sceneLoaded -= IsolateUnwiredTrackers;
            TerminalPause pause = Object.FindFirstObjectByType<TerminalPause>();
            if (pause != null)
                pause.Resume();
            if (keyboard != null)
                InputSystem.RemoveDevice(keyboard);
            if (mouse != null)
                InputSystem.RemoveDevice(mouse);
            if (gamepad != null)
                InputSystem.RemoveDevice(gamepad);
            InputSystem.settings = originalInputSettings;
            originalInputSettings.hideFlags = originalInputFlags;
            Object.Destroy(testInputSettings);
            if (hadVolume) PlayerPrefs.SetFloat(VolumeKey, oldVolume); else PlayerPrefs.DeleteKey(VolumeKey);
            if (hadMotion) PlayerPrefs.SetInt(MotionKey, oldMotion); else PlayerPrefs.DeleteKey(MotionKey);
            if (hadBoot) PlayerPrefs.SetInt(BootKey, oldBoot); else PlayerPrefs.DeleteKey(BootKey);
            foreach (string key in new[] { "SilentSuns.Audio.FX", "SilentSuns.Audio.Music", "SilentSuns.Input.MouseSensitivity" })
            {
                if (savedFloats.TryGetValue(key, out float value)) PlayerPrefs.SetFloat(key, value); else PlayerPrefs.DeleteKey(key);
            }
            savedFloats.Clear();
            if (hadCompleted) PlayerPrefs.SetInt("SilentSuns.Progress.Completed", oldCompleted); else PlayerPrefs.DeleteKey("SilentSuns.Progress.Completed");
            GameAudio.ApplyVolumes();
            PlayerPrefs.Save();
        }

        // A real Enter press must start diagnostics without skipping them on the same frame.
        [UnityTest]
        [Category("Menu")]
        public IEnumerator KeyboardBootAndPanelNavigation()
        {
            AssertScreen("Standby");
            Capture("01-standby", 1920, 1080);
            yield return Press(Key.Enter);
            Assert.That(terminal.GetComponent<TerminalMenu>().State, Is.EqualTo(TerminalMenu.PowerState.PoweringOn));
            yield return new WaitForSecondsRealtime(0.52f);
            Capture("00-warm-up", 1920, 1080);
            yield return new WaitForSecondsRealtime(.75f);
            Capture("00-warm-up-settling", 1920, 1080);
            yield return WaitForState(TerminalMenu.PowerState.Booting);
            AssertScreen("Boot");
            yield return new WaitForSecondsRealtime(2.2f);
            Capture("02-boot", 1920, 1080);
            yield return Press(Key.Space);
            yield return new WaitForSecondsRealtime(1.6f);
            AssertScreen("Main Menu");
            Assert.That(EventSystem.current.currentSelectedGameObject.name, Is.EqualTo("Begin Survey"));
            Assert.That(terminal.Find("Main Menu/Carrier trace"), Is.Null);
            Capture("03-main-menu", 1920, 1080);
            Capture("04-main-menu-4-3", 1280, 960);
            Capture("04-main-menu-1440p", 2560, 1440);
            Capture("04-main-menu-laptop", 1366, 768);

            yield return Press(Key.DownArrow);
            Assert.That(EventSystem.current.currentSelectedGameObject.name, Is.EqualTo("Terminal Settings"));
            Assert.That(terminal.Find("Main Menu/Command line").GetComponent<TMP_Text>().text, Is.EqualTo("> OPEN SETTINGS"));
            Assert.That(terminal.Find("Main Menu/Terminal Settings/Arrow").GetComponent<TMP_Text>().enabled, Is.True);
            Assert.That(terminal.Find("Main Menu/Begin Survey/Arrow").GetComponent<TMP_Text>().enabled, Is.False);
            yield return Press(Key.Enter);
            AssertScreen("Settings");
            yield return Press(Key.Escape);
            AssertScreen("Main Menu");
            Assert.That(EventSystem.current.currentSelectedGameObject.name, Is.EqualTo("Terminal Settings"));

            yield return Press(Key.DownArrow);
            yield return Press(Key.Enter);
            AssertScreen("Operator Guide");
            Assert.That(terminal.Find("Operator Guide/Control labels").GetComponent<TMP_Text>().text, Does.Contain("CTRL"));
            yield return new WaitForSecondsRealtime(0.8f);
            Capture("06-guide", 1920, 1080);
            yield return Press(Key.Escape);
            yield return Press(Key.Escape);
            AssertScreen("Confirm Quit");
            Assert.That(EventSystem.current.currentSelectedGameObject.name, Is.EqualTo("Cancel Quit"));
            yield return Press(Key.Escape);
            AssertScreen("Main Menu");
            yield return PressPad(GamepadButton.DpadDown);
            Assert.That(EventSystem.current.currentSelectedGameObject.name, Is.EqualTo("Exit Program"));
            Assert.That(terminal.Find("Main Menu/Menu hints").GetComponent<TMP_Text>().text, Does.Contain("D-PAD").And.Not.Contain("ENTER"));
            yield return new WaitForSecondsRealtime(0.8f);
            Capture("07-controller", 1920, 1080);
            yield return PressPad(GamepadButton.South);
            AssertScreen("Confirm Quit");
            yield return PressPad(GamepadButton.East);
            AssertScreen("Main Menu");
            InputSystem.QueueStateEvent(mouse, new MouseState { position = new Vector2(20f, 20f), delta = new Vector2(20f, 20f) });
            yield return null;
            yield return null;
            Assert.That(terminal.Find("Main Menu/Menu hints").GetComponent<TMP_Text>().text, Does.Contain("ENTER").And.Not.Contain("D-PAD"));
            InputSystem.QueueStateEvent(gamepad, new GamepadState { leftStick = new Vector2(0.02f, 0.02f) });
            yield return null;
            yield return null;
            Assert.That(terminal.Find("Main Menu/Menu hints").GetComponent<TMP_Text>().text, Does.Contain("ENTER"));
            yield return PressPad(GamepadButton.DpadUp);
            yield return Press(Key.UpArrow);
            Assert.That(terminal.Find("Main Menu/Menu hints").GetComponent<TMP_Text>().text, Does.Contain("ENTER"));
            InputSystem.RemoveDevice(gamepad);
            gamepad = InputSystem.AddDevice<DualShockGamepad>();
            yield return null;
            yield return PressPad(GamepadButton.DpadDown);
            Assert.That(terminal.Find("Main Menu/Menu hints").GetComponent<TMP_Text>().text, Does.Contain("CROSS").And.Contain("CIRCLE"));
        }

        // Exercise actual UI callbacks, saved settings and a world-space pointer raycast.
        [UnityTest]
        [Category("Menu")]
        public IEnumerator SettingsAndPointerInput()
        {
            Click("Standby/Wake screen");
            yield return WaitForState(TerminalMenu.PowerState.Booting);
            Click("Boot/Skip Boot");
            yield return new WaitForSecondsRealtime(0.8f);
            Click("Main Menu/Terminal Settings");
            AssertScreen("Settings");
            Slider volume = terminal.Find("Settings/Sound Effects").GetComponent<Slider>();
            volume.value = 0.25f;
            Assert.That(terminal.GetComponent<AudioSource>().outputAudioMixerGroup.name, Is.EqualTo("FX"));
            AssertMixer("FXVolume", 0.25f);
            Assert.That(PlayerPrefs.GetFloat("SilentSuns.Audio.FX"), Is.EqualTo(0.25f));
            terminal.Find("Settings/Music").GetComponent<Slider>().value = 0.4f;
            AssertMixer("MusicVolume", 0.4f);
            terminal.Find("Settings/Mouse Sensitivity").GetComponent<Slider>().value = 1.6f;
            Assert.That(GamePreferences.MouseSensitivity, Is.EqualTo(1.6f));
            Assert.That(terminal.Find("Settings/Preview Audio"), Is.Null);
            foreach (string category in new[] { "AUDIO", "CONTROLS", "DISPLAY & STARTUP" })
                Assert.That(terminal.Find("Settings/" + category + " category").GetComponent<TMP_Text>().text, Is.EqualTo(category));
            Assert.That(terminal.Find("Settings/Sound Effects label").GetComponent<TMP_Text>().text, Is.EqualTo("SOUND EFFECTS (SFX)"));
            terminal.Find("Settings/Reduced Motion").GetComponent<Toggle>().isOn = true;
            Assert.That(terminal.Find("CRT Display").GetComponent<TerminalDisplay>().ReducedMotion, Is.True);
            Assert.That(terminal.Find("Settings").GetComponent<CanvasGroup>().alpha, Is.EqualTo(1f));
            terminal.Find("Settings/Play Boot").GetComponent<Toggle>().isOn = false;
            yield return new WaitForSecondsRealtime(0.25f);
            Assert.That(volume.fillRect.rect.width / volume.fillRect.parent.GetComponent<RectTransform>().rect.width, Is.EqualTo(0.25f).Within(0.01f));
            Capture("05-settings", 1920, 1080);
            Click("Settings/Settings Back");
            AssertScreen("Main Menu");
            yield return SceneManager.LoadSceneAsync("MainMenu");
            while (!TerminalShipIntro.Ready) yield return null;
            yield return null;
            terminal = GameObject.Find("ARCA Terminal").transform;
            yield return WaitForInvitation();
            Click("Standby/Wake screen");
            yield return WaitForState(TerminalMenu.PowerState.MainMenu);
            AssertScreen("Main Menu");
            Assert.That(terminal.GetComponent<AudioSource>().outputAudioMixerGroup.name, Is.EqualTo("FX"));
            AssertMixer("FXVolume", 0.25f);
        }

        // Completing the sequence naturally must reach the same usable menu as Skip.
        [UnityTest]
        [Category("Menu")]
        public IEnumerator BootCompletesNaturally()
        {
            Click("Standby/Wake screen");
            yield return WaitForState(TerminalMenu.PowerState.Booting);
            yield return new WaitForSecondsRealtime(0.3f);
            TMP_Text output = terminal.Find("Boot/Boot output").GetComponent<TMP_Text>();
            TMP_Text progress = terminal.Find("Boot/POST progress").GetComponent<TMP_Text>();
            Assert.That(progress.text, Does.EndWith("%"));
            Assert.That(progress.isTextOverflowing, Is.False);
            Assert.That(output.text, Does.StartWith(@"A:\SYS>").And.Not.Contain("ATTENTION"));
            Capture("10-command-prompt", 1920, 1080);
            yield return new WaitForSecondsRealtime(5.4f);
            Assert.That(output.text, Does.Contain("BOOTSTRAP /LOCAL").And.Contain("SERVICE /START SHIP"));
            Capture("11-post-output", 1920, 1080);
            float deadline = Time.realtimeSinceStartup + 24f;
            int lastPercent = 0;
            while (!terminal.Find("Main Menu").gameObject.activeSelf && Time.realtimeSinceStartup < deadline)
            {
                int percent = int.Parse(progress.text.Substring(progress.text.Length - 4, 3).Trim());
                Assert.That(percent, Is.InRange(lastPercent, 100));
                lastPercent = percent;
                yield return null;
            }
            Assert.That(lastPercent, Is.EqualTo(100));
            yield return new WaitForSecondsRealtime(0.3f);
            AssertScreen("Main Menu");
            Assert.That(EventSystem.current.currentSelectedGameObject.name, Is.EqualTo("Begin Survey"));
        }

        // Typing must not lock the menu, replay on Back, or blink with Reduced Motion enabled.
        [UnityTest]
        [Category("Menu")]
        public IEnumerator TitleTypesAndRespectsReducedMotion()
        {
            Click("Standby/Wake screen");
            yield return WaitForState(TerminalMenu.PowerState.Booting);
            Click("Boot/Skip Boot");
            TMP_Text title = terminal.Find("Main Menu/Wordmark").GetComponent<TMP_Text>();
            GameObject cursor = title.transform.Find("Block Cursor").gameObject;
            Assert.That(title.maxVisibleCharacters, Is.EqualTo(0));
            yield return new WaitForSecondsRealtime(0.7f);
            Assert.That(title.maxVisibleCharacters, Is.GreaterThan(0).And.LessThan(title.text.Length));
            Assert.That(cursor.activeSelf, Is.True);
            Capture("08-title-typing", 1920, 1080);
            yield return new WaitForSecondsRealtime(1f);
            Assert.That(title.maxVisibleCharacters, Is.EqualTo(title.text.Length));
            Assert.That(cursor.GetComponent<RectTransform>().anchoredPosition.x, Is.GreaterThan(title.textInfo.characterInfo[title.text.Length - 1].xAdvance));

            Click("Main Menu/Terminal Settings");
            terminal.Find("Settings/Reduced Motion").GetComponent<Toggle>().isOn = true;
            yield return null;
            Click("Settings/Settings Back");
            Assert.That(title.maxVisibleCharacters, Is.EqualTo(title.text.Length));
            yield return new WaitForSecondsRealtime(0.7f);
            Assert.That(cursor.activeSelf, Is.True);
            Capture("09-title-complete", 1920, 1080);
            Click("Main Menu/Terminal Settings");
            terminal.Find("Settings/Reduced Motion").GetComponent<Toggle>().isOn = false;
            yield return null;
            Click("Settings/Settings Back");
            Assert.That(cursor.activeSelf, Is.True);
            yield return new WaitForSecondsRealtime(0.6f);
            Assert.That(cursor.activeSelf, Is.False);
            yield return new WaitForSecondsRealtime(0.6f);
            Assert.That(cursor.activeSelf, Is.True);

            GameObject guide = terminal.Find("Main Menu/Operator Guide").gameObject;
            ExecuteEvents.Execute(guide, new PointerEventData(EventSystem.current), ExecuteEvents.pointerEnterHandler);
            Assert.That(terminal.Find("Main Menu/Command line").GetComponent<TMP_Text>().text, Is.EqualTo("> READ OPERATOR_GUIDE"));
        }

        // Pointer dragging must stay aligned after the screen curve is applied.
        [UnityTest]
        [Category("Menu")]
        public IEnumerator CurvedSliderFollowsPointer()
        {
            Click("Standby/Wake screen");
            yield return WaitForState(TerminalMenu.PowerState.Booting);
            Click("Boot/Skip Boot");
            yield return new WaitForSecondsRealtime(0.3f);
            Click("Main Menu/Terminal Settings");
            yield return null;
            Canvas.ForceUpdateCanvases();
            Slider slider = terminal.Find("Settings/Sound Effects").GetComponent<Slider>();
            TerminalDisplay display = terminal.Find("CRT Display").GetComponent<TerminalDisplay>();
            RectTransform area = slider.handleRect.parent.GetComponent<RectTransform>();
            PointerEventData pointer = new PointerEventData(EventSystem.current);
            pointer.button = PointerEventData.InputButton.Left;
            foreach (float fraction in new[] { 0.2f, 0.8f })
            {
                Vector3 local = new Vector3(Mathf.Lerp(area.rect.xMin, area.rect.xMax, fraction), area.rect.center.y);
                pointer.position = display.ContentToScreenPoint(area.TransformPoint(local));
                pointer.pointerPressRaycast = new RaycastResult { module = terminal.GetComponent<TerminalRaycaster>() };
                ExecuteEvents.Execute(slider.gameObject, pointer, ExecuteEvents.dragHandler);
                Assert.That(slider.value, Is.EqualTo(fraction).Within(0.02f));
            }
        }

        // Verify pause in the actual survey scene, including raw mouse input and scene return.
        [UnityTest]
        [Category("Menu")]
        public IEnumerator SurveyPauseFreezesAndRestoresGameplay()
        {
            Click("Standby/Wake screen");
            yield return WaitForState(TerminalMenu.PowerState.Booting);
            Click("Boot/Skip Boot");
            yield return new WaitForSecondsRealtime(0.3f);
            Click("Main Menu/Begin Survey");
            float deadline = Time.realtimeSinceStartup + 30f;
            while (SceneManager.GetActiveScene().name != "SampleScene" && Time.realtimeSinceStartup < deadline)
                yield return null;
            yield return null;
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("SampleScene"));
            Assert.That(TerminalTransition.IsActive, Is.False);
            Assert.That(TerminalPause.BlocksGameplayInput, Is.True);
            yield return WaitForTransition();
            TerminalPause pause = Object.FindFirstObjectByType<TerminalPause>();
            Assert.That(pause, Is.Not.Null);
            MonoBehaviour movement = System.Array.Find(Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None), item => item.GetType().Name == "PlayerMovement");
            Assert.That(movement, Is.Not.Null);
            yield return Press(Key.P);
            Assert.That(pause.IsPaused, Is.False);
            CursorLockMode cursorBeforePause = Cursor.lockState;
            MonoBehaviour interaction = System.Array.Find(Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None), item => item.GetType().Name == "InteractOrigin");
            MonoBehaviour heldButton = System.Array.Find(Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None), item => item.GetType().Name == "Button" && !(bool)item.GetType().GetField("isToggle").GetValue(item));
            Assert.That(interaction, Is.Not.Null);
            Assert.That(heldButton, Is.Not.Null);
            heldButton.GetType().GetField("activated").SetValue(heldButton, true);
            interaction.GetType().GetField("heldInteractable", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(interaction, heldButton);
            yield return Press(Key.Escape);
            Assert.That(pause.IsPaused, Is.True);
            Assert.That(Time.timeScale, Is.Zero);
            Assert.That(AudioListener.pause, Is.True);
            Assert.That(Cursor.lockState, Is.EqualTo(CursorLockMode.None));
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.Null, "ESC must not preselect Resume.");
            yield return Press(Key.DownArrow);
            Assert.That(EventSystem.current.currentSelectedGameObject.name, Is.EqualTo("Resume"));
            yield return Press(Key.DownArrow);
            Assert.That(EventSystem.current.currentSelectedGameObject.name, Is.EqualTo("Settings"));
            Assert.That((bool)heldButton.GetType().GetField("activated").GetValue(heldButton), Is.False);
            var settingsRect = (RectTransform)pause.transform.Find("Layout/Pause/Settings");
            Vector2 hover = pause.GetComponent<TerminalPauseDisplay>().SourceCamera.WorldToScreenPoint(settingsRect.TransformPoint(settingsRect.rect.center));
            InputSystem.QueueStateEvent(mouse, new MouseState { position = hover, delta = new Vector2(20, 0) });
            yield return new WaitForSecondsRealtime(.2f);
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.Null);
            Assert.That(settingsRect.Find("Label").GetComponent<TMP_Text>().color.r, Is.LessThan(.1f), "The hovered Settings button must highlight.");
            Assert.That(pause.transform.Find("Layout/Pause/Resume/Label").GetComponent<TMP_Text>().color.r, Is.GreaterThan(.6f), "Resume must not retain a competing highlight.");
            CapturePause(pause, "12-pause-settings-hover");
            InputSystem.QueueStateEvent(mouse, new MouseState { position = new Vector2(2, 2), delta = new Vector2(-20, 0) });
            yield return new WaitForSecondsRealtime(.2f);
            Assert.That(settingsRect.Find("Label").GetComponent<TMP_Text>().color.r, Is.GreaterThan(.6f), "Leaving a button must clear its highlight.");
            Vector3 position = movement.transform.position;
            Quaternion rotation = movement.transform.rotation;
            InputSystem.QueueStateEvent(mouse, new MouseState { delta = new Vector2(200, 100), position = new Vector2(800, 450) }.WithButton(MouseButton.Left));
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W));
            yield return new WaitForSecondsRealtime(0.2f);
            Assert.That(Vector3.Distance(position, movement.transform.position), Is.LessThan(0.001f));
            Assert.That(Quaternion.Angle(rotation, movement.transform.rotation), Is.LessThan(0.001f));
            Assert.That((bool)heldButton.GetType().GetField("activated").GetValue(heldButton), Is.False);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            InputSystem.QueueStateEvent(mouse, new MouseState());
            yield return new WaitForSecondsRealtime(0.3f);
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.Null, "Mouse input must clear stale navigation highlighting.");
            CapturePause(pause);
            pause.OpenSettings();
            yield return null;
            Assert.That(EventSystem.current.currentSelectedGameObject.name, Is.EqualTo("Sound Effects"));
            Transform settings = pause.transform.Find("Layout/Settings");
            settings.Find("Sound Effects").GetComponent<Slider>().value = 0f;
            settings.Find("Music").GetComponent<Slider>().value = 0.3f;
            settings.Find("Mouse Sensitivity").GetComponent<Slider>().value = 2f;
            settings.Find("Play Boot").GetComponent<Toggle>().isOn = false;
            AssertMixer("FXVolume", 0f);
            AssertMixer("MusicVolume", 0.3f);
            Assert.That(Time.timeScale, Is.Zero);
            foreach (AudioSource source in Object.FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                Assert.That(source.outputAudioMixerGroup, Is.Not.Null, source.name);
            CapturePause(pause, "13-pause-settings");
            yield return Press(Key.Escape);
            Assert.That(pause.IsPaused, Is.True);
            Assert.That(settings.gameObject.activeSelf, Is.False);
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.Null);
            yield return Press(Key.UpArrow);
            Assert.That(EventSystem.current.currentSelectedGameObject.name, Is.EqualTo("Quit"));
            yield return Press(Key.Escape);
            Assert.That(TerminalPause.BlocksGameplayInput, Is.True);
            yield return new WaitForSecondsRealtime(0.3f);
            Assert.That(pause.IsPaused, Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            Assert.That(AudioListener.pause, Is.False);
            Assert.That(Cursor.lockState, Is.EqualTo(cursorBeforePause));
            CheckMouseSensitivity(movement);
            yield return PressPad(GamepadButton.Start);
            Assert.That(pause.IsPaused, Is.True);
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.Null);
            yield return PressPad(GamepadButton.DpadDown);
            Assert.That(EventSystem.current.currentSelectedGameObject.name, Is.EqualTo("Resume"));
            yield return PressPad(GamepadButton.DpadDown);
            Assert.That(EventSystem.current.currentSelectedGameObject.name, Is.EqualTo("Settings"));
            yield return PressPad(GamepadButton.South);
            Assert.That(settings.gameObject.activeSelf, Is.True);
            yield return PressPad(GamepadButton.East);
            Assert.That(EventSystem.current.currentSelectedGameObject.name, Is.EqualTo("Settings"));
            yield return PressPad(GamepadButton.East);
            yield return new WaitForSecondsRealtime(0.3f);
            Assert.That(pause.IsPaused, Is.False);
            yield return Press(Key.Escape);
            pause.AskReturnToMenu();
            Assert.That(EventSystem.current.currentSelectedGameObject.name, Is.EqualTo("Cancel"));
            yield return Press(Key.Escape);
            Assert.That(pause.IsPaused, Is.True);
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.Null, "Cancel must not invent a Resume selection.");
            pause.AskReturnToMenu();
            pause.ConfirmLeave();
            Assert.That(TerminalTransition.IsActive, Is.True);
            yield return WaitForTransition();
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("SampleScene"));
            Assert.That(TerminalShipIntro.Ready, Is.True);
            Assert.That(Time.timeScale, Is.Zero);
            Assert.That(AudioListener.pause, Is.False);
            Assert.That(TerminalPause.Available, Is.True);
            terminal = GameObject.Find("ARCA Terminal").transform;
            Assert.That(terminal.GetComponent<TerminalSessionRestoration>().IsActive, Is.False);
            yield return WaitForInvitation();
            Click("Standby/Wake screen");
            yield return WaitForState(TerminalMenu.PowerState.MainMenu);
            AssertScreen("Main Menu");
            Assert.That(GamePreferences.PlayBoot, Is.False);
            Assert.That(GamePreferences.MouseSensitivity, Is.EqualTo(2f));
            AssertMixer("FXVolume", 0f);
            AssertMixer("MusicVolume", 0.3f);
        }

        private void CheckMouseSensitivity(MonoBehaviour movement)
        {
            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            object handler = movement.GetType().GetField("inputHandler", flags).GetValue(movement);
            var look = handler.GetType().GetProperty("lookInput");
            Vector2 previous = (Vector2)look.GetValue(handler);
            look.SetValue(handler, new Vector2(2f, 0f));
            var rotate = movement.GetType().GetMethod("HandleRotationNoGravity", flags);
            Quaternion initial = movement.transform.rotation;
            GamePreferences.SetMouseSensitivity(1f);
            rotate.Invoke(movement, null);
            float normal = Quaternion.Angle(initial, movement.transform.rotation);
            movement.transform.rotation = initial;
            GamePreferences.SetMouseSensitivity(2f);
            rotate.Invoke(movement, null);
            float doubled = Quaternion.Angle(initial, movement.transform.rotation);
            Assert.That(normal, Is.GreaterThan(0.1f));
            Assert.That(doubled, Is.EqualTo(normal * 2f).Within(0.02f));
            movement.transform.rotation = initial;
            look.SetValue(handler, previous);
        }

        private void AssertMixer(string parameter, float slider)
        {
            AudioMixer mixer = Resources.Load<AudioMixer>("SilentSunsAudio");
            Assert.That(mixer.GetFloat(parameter, out float actual), Is.True, parameter + " must be exposed");
            Assert.That(actual, Is.EqualTo(GameAudio.Decibels(slider)).Within(0.01f));
        }

        private IEnumerator WaitForTransition()
        {
            float deadline = Time.realtimeSinceStartup + 30f;
            while ((TerminalTransition.IsActive || TerminalShipIntro.IsActive && (!TerminalShipIntro.Ready || Object.FindFirstObjectByType<TerminalMenu>().State == TerminalMenu.PowerState.InGame)) && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(TerminalTransition.IsActive, Is.False);
            yield return null;
            yield return null;
        }

        // Complete real objectives through the existing tracker, then reload the menu.
        [UnityTest]
        [Category("Menu")]
        public IEnumerator CompletionPersistsAfterVictory()
        {
            Assert.That(GameProgress.HasCompletedGame, Is.False);
            yield return SceneManager.LoadSceneAsync("SampleScene");
            yield return null;
            MonoBehaviour tracker = System.Array.Find(Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None), item => item.GetType().Name == "ObjectiveTracker" && item.enabled);
            Assert.That(tracker, Is.Not.Null);
            for (int i = 1; i <= 4; i++)
            {
                MonoBehaviour planet = (MonoBehaviour)tracker.GetType().GetField("planet" + i).GetValue(tracker);
                planet.GetType().GetField("waveformComplete").SetValue(planet, true);
                planet.GetType().GetField("compComplete").SetValue(planet, true);
            }
            object cryo = tracker.GetType().GetField("cryoButton").GetValue(tracker);
            cryo.GetType().GetField("activated").SetValue(cryo, true);
            float deadline = Time.realtimeSinceStartup + 5f;
            while (SceneManager.GetActiveScene().name != "VictoryScene" && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("VictoryScene"));
            Assert.That(GameProgress.HasCompletedGame, Is.True);
            yield return SceneManager.LoadSceneAsync("MainMenu");
            while (!TerminalShipIntro.Ready) yield return null;
            yield return null;
            Assert.That(GameProgress.HasCompletedGame, Is.True);
            Assert.That(PlayerPrefs.GetInt("SilentSuns.Progress.Completed"), Is.EqualTo(1));
        }

        // Keep the fine raster while allowing animated interference to be switched off.
        [UnityTest]
        [Category("Menu")]
        public IEnumerator DisplayInterferenceRespectsReducedMotion()
        {
            Click("Standby/Wake screen");
            yield return WaitForState(TerminalMenu.PowerState.Booting);
            Click("Boot/Skip Boot");
            yield return new WaitForSecondsRealtime(2f);
            Material material = terminal.Find("CRT Display").GetComponent<TerminalDisplay>().EffectMaterial;
            Assert.That(material.GetFloat("_Curvature"), Is.LessThanOrEqualTo(0.02f));
            material.SetFloat("_DisplayTime", 4.5f);
            Capture("14-crt-signal", 1920, 1080);
            Assert.That(material.HasProperty("_MeshStrength"), Is.False);
            Assert.That(material.HasProperty("_RefreshStrength"), Is.False);
            Assert.That(material.GetFloat("_InterferenceStrength"), Is.GreaterThan(0));
            terminal.GetComponent<TerminalMenu>().SetReducedMotion(true);
            yield return null;
            Assert.That(material.GetFloat("_InterferenceStrength"), Is.Zero);
        }

        // The powered-off computer is silent; music starts after warm-up and fades before gameplay.
        [UnityTest]
        [Category("Menu")]
        public IEnumerator MenuMusicStartsOnBootAndFadesOnPlay()
        {
            AudioSource music = terminal.Find("Menu Music").GetComponent<AudioSource>();
            Assert.That(music.outputAudioMixerGroup.name, Is.EqualTo("Music"));
            Assert.That(music.clip.name, Is.EqualTo("Menu Music"));
            Assert.That(music.loop, Is.True);
            Assert.That(music.isPlaying, Is.False);
            Click("Standby/Wake screen");
            yield return WaitForState(TerminalMenu.PowerState.Booting);
            Assert.That(music.isPlaying, Is.True);
            Assert.That(music.volume, Is.LessThan(0.55f));
            Click("Boot/Skip Boot");
            yield return new WaitForSecondsRealtime(3f);
            Assert.That(music.volume, Is.EqualTo(0.55f).Within(0.001f));
            GamePreferences.SetMusic(0f);
            AssertMixer("MusicVolume", 0f);
            GamePreferences.SetMusic(0.4f);
            GamePreferences.SetFX(0f);
            AssertMixer("MusicVolume", 0.4f);
            AssertMixer("FXVolume", 0f);
            Assert.That(music.volume, Is.EqualTo(0.55f).Within(0.001f));
            Click("Main Menu/Begin Survey");
            yield return new WaitForSecondsRealtime(0.25f);
            Assert.That(music.volume, Is.GreaterThan(0).And.LessThan(0.55f));
            yield return new WaitForSecondsRealtime(0.75f);
            Assert.That(music.volume, Is.Zero);
            Assert.That(music.isPlaying, Is.False);
            Assert.That(terminal.Find("Monitor Audio").GetComponent<TerminalPowerAudio>().Running, Is.False);
            yield return WaitForTransition();
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("SampleScene"));
            Assert.That(Object.FindFirstObjectByType<TerminalMenuMusic>(), Is.Null);
        }

        [UnityTest]
        [Category("Menu")]
        public IEnumerator BootDiagnosticsContinueWithAudioMuted()
        {
            GamePreferences.SetFX(0f);
            GamePreferences.SetMusic(0f);
            Click("Standby/Wake screen");
            yield return WaitForState(TerminalMenu.PowerState.Booting);
            var log = terminal.GetComponent<TerminalBootLog>();
            yield return new WaitForSecondsRealtime(21.3f);
            string text = terminal.Find("Boot/Boot output").GetComponent<TMP_Text>().text;
            Assert.That(text, Does.Contain("TTY01").And.Contain("TTY02").And.Contain("01 LOCAL / 00 REMOTE"));
            Capture("15-session-enumeration", 1920, 1080);
            Assert.That(log.Running, Is.True);
            Click("Boot/Skip Boot");
            Assert.That(log.Running, Is.False);
            float stoppedAt = log.Elapsed;
            yield return new WaitForSecondsRealtime(.6f);
            Assert.That(log.Elapsed, Is.EqualTo(stoppedAt));
            AssertScreen("Main Menu");
            Assert.That(terminal.Find("CRT Display").GetComponent<TerminalDisplay>().DiagnosticGlitch, Is.Zero);
        }

        [UnityTest]
        [Category("Menu")]
        public IEnumerator ResumeIsAbsentAndInfrastructureRemainsInactive()
        {
            GameProgress.MarkCompleted();
            TerminalSessionRestoration restore = terminal.GetComponent<TerminalSessionRestoration>();
            restore.RefreshAvailability();
            Assert.That(terminal.Find("Main Menu/Resume Session"), Is.Null);
            Assert.That(terminal.Find("Main Menu/Save availability"), Is.Null);
            Click("Standby/Wake screen");
            yield return WaitForState(TerminalMenu.PowerState.Booting);
            Click("Boot/Skip Boot");
            yield return new WaitForSecondsRealtime(0.3f);
            restore.Begin();
            yield return null;
            AssertScreen("Main Menu");
            Assert.That(restore.IsActive, Is.False);
        }



        [UnityTest]
        [Category("Menu")]
        public IEnumerator HeldAndRepeatedEnterCannotBeginSurvey()
        {
            yield return Press(Key.Enter);
            for (int i = 0; i < 6; i++) yield return Press(Key.Enter);
            yield return WaitForState(TerminalMenu.PowerState.Booting);
            var menu = terminal.GetComponent<TerminalMenu>();
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Enter));
            yield return null;
            yield return new WaitForSecondsRealtime(TerminalBootLog.Duration);
            yield return new WaitForSecondsRealtime(.5f);
            Assert.That(menu.State, Is.EqualTo(TerminalMenu.PowerState.MainMenu));
            Assert.That(terminal.Find("Main Menu").GetComponent<CanvasGroup>().interactable, Is.False);
            yield return new WaitForSecondsRealtime(.6f);
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("SampleScene"));
            Assert.That(TerminalShipIntro.Ready, Is.True);
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());
            yield return new WaitForSecondsRealtime(.35f);
            Assert.That(terminal.Find("Main Menu").GetComponent<CanvasGroup>().interactable, Is.True);
            Assert.That(EventSystem.current.currentSelectedGameObject.name, Is.EqualTo("Begin Survey"));
        }

        [UnityTest]
        [Category("Menu")]
        public IEnumerator AcceptedInputsUseTheirOwnRecordingAndHoverIsSilent()
        {
            Click("Standby/Wake screen");
            yield return WaitForState(TerminalMenu.PowerState.Booting);
            Click("Boot/Skip Boot");
            yield return new WaitForSecondsRealtime(.3f);
            var sounds = terminal.GetComponent<TerminalInteractionAudio>();
            int count = sounds.AcceptedSounds;
            yield return Press(Key.DownArrow);
            Assert.That(sounds.AcceptedSounds, Is.EqualTo(count+1));
            Assert.That(sounds.LastClip.name, Is.EqualTo("KeyboardPress"));
            yield return new WaitForSecondsRealtime(.1f);
            yield return Press(Key.Enter);
            AssertScreen("Settings");
            Assert.That(sounds.AcceptedSounds, Is.EqualTo(count+2));
            yield return new WaitForSecondsRealtime(.1f);
            Slider slider = terminal.Find("Settings/Sound Effects").GetComponent<Slider>();
            float before = slider.value;
            yield return Press(Key.RightArrow);
            Assert.That(slider.value, Is.GreaterThan(before));
            Assert.That(sounds.AcceptedSounds, Is.EqualTo(count+3));
            yield return new WaitForSecondsRealtime(.1f);
            Click("Settings/Reduced Motion");
            Assert.That(sounds.LastClip.name, Is.EqualTo("MouseClick"));
            Assert.That(sounds.AcceptedSounds, Is.EqualTo(count+4));
            yield return new WaitForSecondsRealtime(.1f);
            yield return Press(Key.Escape);
            Assert.That(sounds.LastClip.name, Is.EqualTo("KeyboardPress"));
            Assert.That(sounds.AcceptedSounds, Is.EqualTo(count+5));
            GameObject guide = terminal.Find("Main Menu/Operator Guide").gameObject;
            ExecuteEvents.Execute(guide,new PointerEventData(EventSystem.current),ExecuteEvents.pointerEnterHandler);
            yield return new WaitForSecondsRealtime(.2f);
            Assert.That(sounds.AcceptedSounds, Is.EqualTo(count+5));
            Click("Main Menu/Operator Guide");
            Assert.That(sounds.LastClip.name, Is.EqualTo("MouseClick"));
            AssertScreen("Operator Guide");
            yield return new WaitForSecondsRealtime(.1f);
            yield return Press(Key.Escape);
            count = sounds.AcceptedSounds;
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.DownArrow));
            yield return new WaitForSecondsRealtime(1f);
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());
            yield return null;
            Assert.That(sounds.AcceptedSounds-count, Is.InRange(1,8));
            Assert.That(terminal.GetComponent<AudioSource>().outputAudioMixerGroup.name, Is.EqualTo("FX"));
            GamePreferences.SetFX(0f);
            AssertMixer("FXVolume",0f);
        }

        [UnityTest]
        [Category("Menu")]
        public IEnumerator MissingInteractionAudioStillBootsWithSavedPreferences()
        {
            GamePreferences.SetReducedMotion(true);
            GamePreferences.SetPlayBoot(false);
            GamePreferences.SetFX(0f);
            GamePreferences.SetMusic(.23f);
            yield return SceneManager.LoadSceneAsync("MainMenu");
            while (!TerminalShipIntro.Ready) yield return null;
            yield return null;
            terminal = GameObject.Find("ARCA Terminal").transform;
            var sounds = terminal.GetComponent<TerminalInteractionAudio>();
            foreach (string field in new[] { "keyboardPress","mouseClick" })
                SetPrivate(sounds,field,null);
            Assert.That(terminal.Find("CRT Display").GetComponent<TerminalDisplay>().ReducedMotion, Is.True);
            AssertMixer("FXVolume",0f);
            AssertMixer("MusicVolume",.23f);
            yield return WaitForInvitation();
            yield return Press(Key.Enter);
            yield return WaitForState(TerminalMenu.PowerState.MainMenu);
            AssertScreen("Main Menu");
            Assert.That(sounds.AcceptedSounds, Is.Zero);
            Assert.That(terminal.GetComponent<TerminalSessionRestoration>().IsActive, Is.False);
            Assert.That(terminal.Find("Main Menu/Wordmark").GetComponent<TMP_Text>().maxVisibleCharacters, Is.GreaterThanOrEqualTo(11));
            yield return Press(Key.DownArrow);
            yield return Press(Key.Enter);
            AssertScreen("Settings");
        }



        [UnityTest]
        [Category("Menu")]
        public IEnumerator ExitProgramStopsAudio()
        {
            Click("Standby/Wake screen");
            yield return WaitForState(TerminalMenu.PowerState.Booting);
            Click("Boot/Skip Boot");
            yield return new WaitForSecondsRealtime(.3f);
            Click("Main Menu/Exit Program");
            AssertScreen("Confirm Quit");
            Capture("22-exit-confirmation",1920,1080);
            yield return null;
            Click("Confirm Quit/Confirm Exit");
            var menu = terminal.GetComponent<TerminalMenu>();
            Assert.That(menu.ExitRequested, Is.True);
            Assert.That(menu.State, Is.EqualTo(TerminalMenu.PowerState.Exiting));
            foreach (AudioSource source in Object.FindObjectsByType<AudioSource>()) Assert.That(source.isPlaying, Is.False, source.name);
            yield return null;
        }





        private IEnumerator WaitForInvitation()
        {
            float deadline = Time.realtimeSinceStartup + 8;
            while (!terminal.GetComponent<TerminalMenu>().InvitationReady && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(terminal.GetComponent<TerminalMenu>().InvitationReady, Is.True);
        }

        private IEnumerator WaitForState(TerminalMenu.PowerState expected)
        {
            float deadline = Time.realtimeSinceStartup + 4f;
            while (terminal.GetComponent<TerminalMenu>().State != expected && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(terminal.GetComponent<TerminalMenu>().State, Is.EqualTo(expected));
            if (expected == TerminalMenu.PowerState.MainMenu || expected == TerminalMenu.PowerState.WaitingForTouch)
                yield return new WaitForSecondsRealtime(0.3f);
        }

        private IEnumerator WaitForTransitionStart()
        {
            float deadline = Time.realtimeSinceStartup + 4f;
            while (!TerminalTransition.IsActive && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(TerminalTransition.IsActive, Is.True);
        }

        private void SetPrivate(object instance, string name, object value)
        {
            instance.GetType().GetField(name, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(instance, value);
        }

        // These two existing broken trackers are excluded only during the pause integration check.
        private void IsolateUnwiredTrackers(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != "SampleScene")
                return;
            foreach (MonoBehaviour component in Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            {
                if (component.GetType().Name == "ObjectiveTracker" && component.GetType().GetField("objText").GetValue(component) == null)
                    component.enabled = false;
            }
        }

        private void CapturePause(TerminalPause pause, string name = "12-survey-pause")
        {
            string folder = System.Environment.GetEnvironmentVariable("SILENT_SUNS_MENU_CAPTURES");
            if (string.IsNullOrEmpty(folder))
                return;
            var pauseDisplay = pause.GetComponent<TerminalPauseDisplay>();
            Canvas canvas = pauseDisplay.Presentation;
            RenderPipeline.SubmitRenderRequest(pauseDisplay.SourceCamera, new UniversalRenderPipeline.SingleCameraRequest { destination = pauseDisplay.PanelTexture });
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = Camera.main;
            canvas.planeDistance = 0.5f;
            Canvas.ForceUpdateCanvases();
            RenderTexture target = new RenderTexture(1920, 1080, 24);
            target.Create();
            RenderPipeline.SubmitRenderRequest(Camera.main, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = target;
            Texture2D image = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
            image.Apply();
            File.WriteAllBytes(Path.Combine(folder, name + ".png"), image.EncodeToPNG());
            RenderTexture.active = previous;
            Object.Destroy(image);
            target.Release();
            Object.Destroy(target);
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        }

        // Keep the prototype's own startup errors visible, separate from menu-only checks.
        [UnityTest]
        [Category("PrototypeIntegration")]
        [Explicit("SampleScene has two extra ObjectiveTracker components with missing text and button references.")]
        public IEnumerator BeginSurveyLoadsPrototype()
        {
            Click("Standby/Wake screen");
            yield return WaitForState(TerminalMenu.PowerState.Booting);
            Click("Boot/Skip Boot");
            yield return new WaitForSecondsRealtime(0.3f);
            Click("Main Menu/Begin Survey");
            AssertScreen("Loading");
            float deadline = Time.realtimeSinceStartup + 30f;
            while (SceneManager.GetActiveScene().name != "SampleScene" && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("SampleScene"));
            yield return null;
            Assert.That(GameObject.Find("ARCA Terminal"), Is.Null);
        }

        // Queue input through the same Input System device path used by the menu module.
        private IEnumerator Press(Key key)
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(key));
            yield return null;
            yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return null;
            yield return null;
        }

        // Controller navigation uses the same selectable controls as keyboard navigation.
        private IEnumerator PressPad(GamepadButton button)
        {
            InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(button));
            yield return null;
            yield return null;
            InputSystem.QueueStateEvent(gamepad, new GamepadState());
            yield return null;
            yield return null;
        }

        // Hidden panels must not block the visible button's pointer event.
        private void Click(string path)
        {
            Canvas.ForceUpdateCanvases();
            GameObject target = terminal.Find(path).gameObject;
            RectTransform rect = target.GetComponent<RectTransform>();
            PointerEventData pointer = new PointerEventData(EventSystem.current);
            pointer.position = terminal.Find("CRT Display").GetComponent<TerminalDisplay>().ContentToScreenPoint(rect.TransformPoint(rect.rect.center));
            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer, results);
            Assert.That(results.Count, Is.GreaterThan(0), path + " must receive pointer input");
            Assert.That(results[0].gameObject, Is.EqualTo(target));
            ExecuteEvents.Execute(target, pointer, ExecuteEvents.pointerClickHandler);
        }

        // One active interactive screen prevents input reaching a covered button.
        private void AssertScreen(string name)
        {
            int visible = 0;
            foreach (CanvasGroup group in terminal.GetComponentsInChildren<CanvasGroup>(true))
            {
                if (group.gameObject.activeSelf)
                {
                    visible++;
                    Assert.That(group.name, Is.EqualTo(name));
                    Assert.That(group.blocksRaycasts, Is.True);
                }
                else
                    Assert.That(group.blocksRaycasts, Is.False);
            }
            Assert.That(visible, Is.EqualTo(1));
        }

        // Optional captures render the real scene through URP, outside the asset folders.
        private void Capture(string name, int width, int height, bool fit = true, System.Action<Texture2D,Camera> inspect = null)
        {
            string folder = System.Environment.GetEnvironmentVariable("SILENT_SUNS_MENU_CAPTURES");
            if (string.IsNullOrEmpty(folder) && inspect == null)
                return;
            Camera camera = Camera.main;
            float aspect = camera.aspect;
            camera.aspect = (float)width / height;
            if (fit) terminal.SendMessage("FitCamera");
            Canvas.ForceUpdateCanvases();
            TerminalDisplay display = terminal.Find("CRT Display").GetComponent<TerminalDisplay>();
            RenderTexture target = new RenderTexture(width, height, 24);
            target.Create();
            camera.targetTexture = target;
            display.SendMessage("ResizeTexture");
            RenderPipeline.SubmitRenderRequest(display.SourceCamera, new UniversalRenderPipeline.SingleCameraRequest { destination = display.SourceCamera.targetTexture });
            RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = target;
            Texture2D image = new Texture2D(width, height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            image.Apply();
            inspect?.Invoke(image,camera);
            if (!string.IsNullOrEmpty(folder))
            {
                Directory.CreateDirectory(folder);
                File.WriteAllBytes(Path.Combine(folder, name + ".png"), image.EncodeToPNG());
            }
            RenderTexture.active = previous;
            Object.Destroy(image);
            target.Release();
            Object.Destroy(target);
            camera.targetTexture = null;
            display.SendMessage("ResizeTexture");
            camera.aspect = aspect;
            terminal.SendMessage("FitCamera");
        }
    }
    // In-memory adapter for presentation tests, never assigned to the production scene.
    public class TestSessionProvider : SessionSaveProvider
    {
        public bool valid = true;
        public bool fail;
        public int restoreCalls;
        public string restoredId;
        public override bool TryGetLatest(out SavedSession session)
        {
            session = valid ? new SavedSession { id = "test-only", description = "ADAPTER TEST / NO SAVE FILE" } : null;
            return valid;
        }
        public override IEnumerator Restore(SavedSession session, SessionRestoreResult result)
        {
            restoreCalls++;
            restoredId = session.id;
            yield return null;
            if (fail)
                throw new System.InvalidOperationException("Test load failure");
            result.succeeded = true;
        }
    }

}
