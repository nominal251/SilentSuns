using System;
using System.Collections;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SilentSuns.Menu
{
    public class TerminalExtras : MonoBehaviour
    {
        private static TerminalExtras instance;
        private ColorAdjustments color;
        private Bloom calibrationBloom;
        private FilmGrain calibrationGrain;
        private ChromaticAberration calibrationFringing;
        private Vignette calibrationVignette;
        private LensDistortion calibrationDistortion;
        private MotionBlur calibrationBlur;
        private Volume volume;
        private int digits;
        private float lastDigit;
        private Gamepad activePad;
        private AudioSource alert;
        private Texture2D cursor;
        private float cursorSize = -1;
        private bool darkCursor, outlineCursor;
        private float playedSeconds;
        private string sessionScene;
        public static string ScreenshotFolder => Path.Combine(Application.persistentDataPath, "Screenshots");
        public static float SessionSeconds => instance == null ? 0 : instance.playedSeconds;
        public static bool ShuttingDown
        {
            get; private set;
        }
        public static bool MenuOpen => TerminalPause.Paused || (TerminalMenu.Active != null && TerminalMenu.Active.State == TerminalMenu.PowerState.MainMenu);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Create()
        {
            ShuttingDown = false;
            var root = new GameObject("Terminal services");
            DontDestroyOnLoad(root);
            instance = root.AddComponent<TerminalExtras>();
        }
        private void Awake()
        {
            volume = gameObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 100;
            volume.profile = ScriptableObject.CreateInstance<VolumeProfile>();
            color = volume.profile.Add<ColorAdjustments>();
            // Temporary overrides leave the scene's own post effects untouched.
            calibrationBloom = volume.profile.Add<Bloom>();
            calibrationBloom.intensity.Override(0);
            calibrationGrain = volume.profile.Add<FilmGrain>();
            calibrationGrain.intensity.Override(0);
            calibrationFringing = volume.profile.Add<ChromaticAberration>();
            calibrationFringing.intensity.Override(0);
            calibrationVignette = volume.profile.Add<Vignette>();
            calibrationVignette.intensity.Override(0);
            calibrationDistortion = volume.profile.Add<LensDistortion>();
            calibrationDistortion.intensity.Override(0);
            calibrationBlur = volume.profile.Add<MotionBlur>();
            calibrationBlur.intensity.Override(0);
            UpdateCalibrationEffects();
            alert = gameObject.AddComponent<AudioSource>();
            alert.playOnAwake = false;
            alert.ignoreListenerPause = true;
            alert.outputAudioMixerGroup = GameAudio.InterfaceGroup;
            InputSystem.onDeviceChange += DeviceChanged;
            SceneManager.sceneLoaded += SceneLoaded;
        }
        private void SceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (TerminalGameSession.IsSurveyScene(scene.name))
            {
                sessionScene = scene.name;
                playedSeconds = 0;
            }
        }
        private void Update()
        {
            color.postExposure.Override(GamePreferences.Brightness * 2);
            color.contrast.Override((GamePreferences.Contrast - 1) * 100);
            UpdateCalibrationEffects();
            if (Camera.main != null)
                Camera.main.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            if (!ShuttingDown)
                AudioListener.volume = !Application.isFocused && GamePreferences.MuteUnfocused ? 0 : 1;
            if (SceneManager.GetActiveScene().name == sessionScene && !TerminalPause.BlocksGameplayInput)
                playedSeconds += Time.deltaTime;
            CheckController();
            UpdateCursor();
            if (Keyboard.current?.f12Key.wasPressedThisFrame == true && !TerminalBindings.Listening)
                StartCoroutine(Capture());
            CheckBuildSequence();
        }
        private void UpdateCalibrationEffects()
        {
            bool clear = TerminalSettings.EffectsSuspended;
            calibrationBloom.active = clear;
            calibrationGrain.active = clear;
            calibrationFringing.active = clear;
            calibrationVignette.active = clear;
            calibrationDistortion.active = clear;
            calibrationBlur.active = clear;
        }
        private void CheckController()
        {
            var pad = Gamepad.current;
            if (pad != null)
            {
                bool used = pad.leftStick.ReadValue().sqrMagnitude > .12f || pad.rightStick.ReadValue().sqrMagnitude > .12f || pad.dpad.ReadValue().sqrMagnitude > .1f;
                foreach (var control in pad.allControls)
                    if (control is ButtonControl button && button.wasPressedThisFrame)
                        used = true;
                if (used)
                    activePad = pad;
            }
            if (Keyboard.current?.anyKey.wasPressedThisFrame == true || Mouse.current?.delta.ReadValue().sqrMagnitude >= 4)
                activePad = null;
        }
        private void DeviceChanged(InputDevice device, InputDeviceChange change)
        {
            if (device != activePad || (change != InputDeviceChange.Disconnected && change != InputDeviceChange.Removed))
                return;
            activePad = null;
            TerminalPause.PauseForDisconnect();
            var modal = TerminalModal.Open("CONTROLLER DISCONNECTED", "INPUT CONNECTION LOST\n\nReconnect your controller or continue with keyboard and mouse.\nThe survey remains paused.");
            TerminalUI.Button(modal.Body, "CONTINUE WITH KEYBOARD / MOUSE", 0, .70f, 1, .16f, modal.Close);
            alert.outputAudioMixerGroup = GameAudio.InterfaceGroup;
            alert.PlayOneShot(TerminalUIAssets.Current.disconnected, .45f);
            modal.tick = () =>
            {
                if (Gamepad.current != null && Gamepad.current.added)
                    modal.Message.text = "CONTROLLER CONNECTED\n\nPress B / CIRCLE to close this notice.\nResume when you are ready.";
            };
        }
        private void CheckBuildSequence()
        {
            GameObject selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            if (!MenuOpen || TerminalModal.Current != null || TerminalBindings.Listening || (selected != null && selected.GetComponent<TMP_InputField>() != null))
            {
                digits = 0;
                return;
            }
            if (Time.unscaledTime - lastDigit > 4)
                digits = 0;
            var keyboard = Keyboard.current;
            if (keyboard == null)
                return;
            foreach (var key in keyboard.allKeys)
            {
                if (!key.wasPressedThisFrame)
                    continue;
                string name = key.keyCode.ToString();
                char digit = name.StartsWith("Digit") ? name[name.Length - 1] : name.StartsWith("Numpad") && name.Length == 7 ? name[6] : '\0';
                if (digit == '\0')
                {
                    digits = 0;
                    continue;
                }
                lastDigit = Time.unscaledTime;
                digits = digit == "4725"[digits] ? digits + 1 : digit == '4' ? 1 : 0;
                if (digits != 4)
                    continue;
                digits = 0;
                var data = TerminalUIAssets.Current;
                string text = "VERSION  " + data.version + "\nBUILD    " + data.build + "\nUPDATED  " + data.updated;
                var modal = TerminalModal.Open("SYSTEM INFORMATION", text);
                var copy = TerminalUI.Button(modal.Body, "COPY INFORMATION", 0, .7f, .6f, .15f, () => GUIUtility.systemCopyBuffer = text);
                copy.onClick.AddListener(() => copy.transform.Find("Label").GetComponent<TMP_Text>().text = "COPIED");
                break;
            }
        }

        private IEnumerator Capture()
        {
            if (Application.isBatchMode)
                yield return null;
            else
                yield return new WaitForEndOfFrame();
            string path = Path.Combine(ScreenshotFolder, "SilentSuns_" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss-fff") + ".png");
            try
            {
                Directory.CreateDirectory(ScreenshotFolder);
                ScreenCapture.CaptureScreenshot(path);
            }
            catch (Exception e) { Notify("SCREENSHOT FAILED\n" + e.Message); yield break; }
            Notify("SCREENSHOT SAVED\n" + path);
        }
        public static void Notify(string text)
        {
            if (instance != null)
                instance.StartCoroutine(instance.Notice(text));
        }
        private IEnumerator Notice(string text)
        {
            var root = new GameObject("Terminal notification", typeof(Canvas), typeof(CanvasScaler));
            root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            root.GetComponent<Canvas>().sortingOrder = 900;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600, 900);
            scaler.matchWidthOrHeight = .5f;
            float margin = GamePreferences.SafeArea;
            var panel = TerminalUI.Panel("Notice", root.transform, .5f, margin, .5f - margin, .16f, TerminalUI.Dark);
            panel.raycastTarget = false;
            TerminalUI.Text("Message", panel.transform, text, .04f, .08f, .92f, .84f, 21);
            var group = panel.gameObject.AddComponent<CanvasGroup>();
            for (float t = 0; t < 5; t += Time.unscaledDeltaTime)
            {
                group.alpha = Mathf.Clamp01(Mathf.Min(t * 5, (5 - t) * 3));
                yield return null;
            }
            Destroy(root);
        }
        private void UpdateCursor()
        {
            if (Mathf.Approximately(cursorSize, GamePreferences.CursorSize) && darkCursor == GamePreferences.DarkCursor && outlineCursor == GamePreferences.CursorOutline)
                return;
            cursorSize = GamePreferences.CursorSize;
            darkCursor = GamePreferences.DarkCursor;
            outlineCursor = GamePreferences.CursorOutline;
            if (cursor != null)
                Destroy(cursor);
            int size = Mathf.RoundToInt(28 * cursorSize);
            cursor = new Texture2D(size, size, TextureFormat.RGBA32, false);
            cursor.filterMode = FilterMode.Point;
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    int row = size - 1 - y;
                    bool inside = x >= 2 && row >= 2 && row < size - 3 && x <= row * .5f;
                    if (!inside)
                        continue;
                    bool edge = x < 4 || row < 4 || x > row * .5f - 2 || row >= size - 5;
                    bool dark = outlineCursor && edge ? !darkCursor : darkCursor;
                    pixels[y * size + x] = dark ? new Color32(8, 12, 34, 255) : new Color32(220, 235, 255, 255);
                }
            cursor.SetPixels32(pixels);
            cursor.Apply();
            Cursor.SetCursor(cursor, new Vector2(2, 2), CursorMode.Auto);
        }
        public static void Shutdown()
        {
            if (ShuttingDown || instance == null)
                return;
            ShuttingDown = true;
            instance.StartCoroutine(instance.PowerDown());
        }
        private IEnumerator PowerDown()
        {
            PlayerPrefs.Save();
            var modal = TerminalModal.Open("SYSTEM SHUTDOWN", "CLOSING SESSION...\nFLUSHING PREFERENCES... OK\nDISPLAY POWER OFF");
            modal.AllowClose = false;
            var group = modal.GetComponent<CanvasGroup>();
            group.interactable = false;
            float duration = GamePreferences.ReducedMotion ? .35f : 1.5f;
            float initial = AudioListener.volume;
            // Capture the complete display, then collapse it to a line before quitting.
            if (Application.isBatchMode)
                yield return null;
            else
                yield return new WaitForEndOfFrame();
            var picture = Application.isBatchMode ? new Texture2D(1, 1) : ScreenCapture.CaptureScreenshotAsTexture();
            var cover = new GameObject("Power off", typeof(Canvas));
            cover.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            cover.GetComponent<Canvas>().sortingOrder = 1000;
            TerminalUI.Panel("Black", cover.transform, 0, 0, 1, 1, Color.black);
            var screen = TerminalUI.Rect("Display", cover.transform, 0, 0, 1, 1).gameObject.AddComponent<RawImage>();
            screen.texture = picture;
            for (float t = 0; t < duration; t += Time.unscaledDeltaTime)
            {
                float phase = t / duration;
                AudioListener.volume = initial * (1 - phase);
                float collapse = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.55f, 1, phase));
                screen.color = Color.Lerp(Color.white, Color.black, collapse);
                if (!GamePreferences.ReducedMotion)
                    screen.rectTransform.localScale = new Vector3(1 - collapse * .9f, Mathf.Max(.002f, 1 - collapse), 1);
                yield return null;
            }
            screen.enabled = false;
            yield return new WaitForSecondsRealtime(.15f);
            foreach (var source in FindObjectsByType<AudioSource>(FindObjectsSortMode.None))
                source.Stop();
            Application.Quit();
#if UNITY_EDITOR
            Destroy(cover);
            Destroy(picture);
            modal.AllowClose=true; modal.Close(); AudioListener.volume=1; ShuttingDown=false;
#endif
        }
        private void OnDestroy()
        {
            InputSystem.onDeviceChange -= DeviceChanged;
            SceneManager.sceneLoaded -= SceneLoaded;
            if (cursor != null)
                Destroy(cursor);
            if (volume != null && volume.profile != null)
                Destroy(volume.profile);
            if (instance == this)
                instance = null;
        }
    }
}
