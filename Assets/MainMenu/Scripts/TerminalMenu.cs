using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SilentSuns.Menu
{
    public class TerminalMenu : MonoBehaviour
    {
        // This script runs the UI. TerminalShipIntro handles the ship and player camera.
        [Header("Screens")]
        [SerializeField] private CanvasGroup standbyScreen;
        [SerializeField] private CanvasGroup bootScreen;
        [SerializeField] private CanvasGroup menuScreen;
        [SerializeField] private CanvasGroup settingsScreen;
        [SerializeField] private CanvasGroup guideScreen;
        [SerializeField] private CanvasGroup quitScreen;
        [SerializeField] private CanvasGroup loadingScreen;
        [SerializeField] private GameObject powerButton;
        [SerializeField] private GameObject startButton;
        [SerializeField] private GameObject guideBackButton;
        [SerializeField] private GameObject cancelQuitButton;

        [Header("Terminal")]
        [SerializeField] private TMP_Text bootText;
        [SerializeField] private TMP_Text bootProgress;
        [SerializeField] private TMP_Text loadingText;
        [SerializeField] private TerminalSettings settings;
        [SerializeField] private TerminalTitle titleAnimation;
        [SerializeField] private TerminalDisplay display;
        [SerializeField] private TerminalBootLog bootLog;
        [SerializeField] private TerminalSessionRestoration restoration;
        [SerializeField] private Camera menuCamera;
        [SerializeField] private string gameplayScene = "SampleScene";

        [Header("Audio")]
        [SerializeField] private TerminalMenuMusic menuMusic;
        [SerializeField] private TerminalPowerAudio powerAudio;
        public const float WarmupDuration = 2.4f;
        public const float ArrivalDuration = 3.6f;

        [Header("Hardware")]
        [SerializeField] private TerminalHardware hardware;
        [SerializeField] private Canvas sourceCanvas;
        [SerializeField] private TerminalTypingAudio typingAudio;
        [SerializeField] private UnityEngine.InputSystem.UI.InputSystemUIInputModule inputModule;
        public enum PowerState
        {
            WaitingForTouch = 0,
            PoweringOn = 1,
            Booting = 2,
            MainMenu = 3,
            InGame = 4,
            PoweringDown = 5,
            Exiting = 6,
            WarmingUp = 7
        }
        public PowerState State { get; private set; } = PowerState.WarmingUp;
        public bool InvitationReady => State == PowerState.WaitingForTouch && !waitingForRelease;
        // The team has not connected a gameplay save system yet.
        public bool CanRestore => false;
        public bool ExitRequested { get; private set; }
        public bool CanTogglePower => !TerminalTransition.IsActive && Time.unscaledTime >= nextPowerInput &&
            (State == PowerState.WaitingForTouch || State == PowerState.PoweringOn || State == PowerState.Booting || State == PowerState.MainMenu);
        private float nextPowerInput;
        private CanvasGroup currentScreen;
        private Coroutine bootRoutine;
        private bool waitingForRelease;
        private float releaseAt;
        private GameObject returnSelection;
        private int lastWidth;
        private int lastHeight;
        private TMP_Text invitation;
        private Image dormantFace;
        private Material wakeMaterial;
        private float wakeHover;
        private static readonly string[] Invitations = { "hello?", "are you there?", "i need you to respond.", "touch the light." };
        private float dormantSince;
        private bool screenWarm;

        public void ShowRestoration(CanvasGroup screen, GameObject selection)
        {
            ShowScreen(screen, selection);
        }

        // Wait for TerminalShipIntro to load the ship before showing anything.
        private IEnumerator Start()
        {
            while (!TerminalShipIntro.Ready)
                yield return null;
            GetComponent<TerminalInteractionAudio>().TakeFocus();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            SetReducedMotion(GamePreferences.ReducedMotion);
            foreach (CanvasGroup screen in new[] { standbyScreen, bootScreen, menuScreen, settingsScreen, guideScreen, quitScreen, loadingScreen })
                SetVisible(screen, false);
            sourceCanvas.enabled = true;
            bootLog.Stop();
            FitCamera();
            yield return WarmScreen();
        }

        private IEnumerator WarmScreen()
        {
            ChangeState(PowerState.WarmingUp);
            ShowScreen(standbyScreen, null);
            inputModule.enabled = false;
            powerButton.GetComponent<Button>().interactable = false;
            currentScreen.interactable = false;
            display.SetDormant(true);
            display.SetPower(0);
            wakeMaterial.SetFloat("_Reveal", 0);
            wakeMaterial.SetFloat("_Warming", 0);
            invitation.text = "";
            yield return new WaitForSecondsRealtime(.55f);
            powerAudio?.PowerOn();
            bool reduced = GamePreferences.ReducedMotion;
            float duration = reduced ? 1.2f : ArrivalDuration;
            for (float t = 0; t < duration; t += Time.unscaledDeltaTime)
            {
                float progress = t / duration;
                float reveal = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.58f, 1, progress));
                display.SetPower(Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.05f, .65f, progress)));
                display.SetStandbyWarmup(reduced ? 0 : 1 - progress);
                wakeMaterial.SetFloat("_Reveal", reveal);
                wakeMaterial.SetFloat("_Warming", Mathf.Sin(progress * Mathf.PI));
                wakeMaterial.SetFloat("_Pulse", .55f);
                wakeMaterial.SetFloat("_Phase", 0);
                wakeMaterial.SetFloat("_ReducedMotion", 1);
                if (progress > .82f) invitation.text = "hello?";
                invitation.color = new Color(.52f, .68f, 1, Mathf.SmoothStep(0, .7f, Mathf.InverseLerp(.82f, 1, progress)));
                yield return null;
            }
            display.SetPower(1);
            display.SetStandbyWarmup(0);
            wakeMaterial.SetFloat("_Reveal", 1);
            wakeMaterial.SetFloat("_Warming", 0);
            screenWarm = true;
            dormantSince = Time.unscaledTime;
            ChangeState(PowerState.WaitingForTouch);
            WaitForRelease();
        }

        public void ConfigureShipScreen(Camera camera, float aspect)
        {
            menuCamera = camera;
            float height = 1600f / aspect;
            ResizePanels(height);
            ResizeFrame(height);
            CreateInvitation(aspect);
        }

        // TerminalShipIntro calls ConfigureShipScreen with the telescope's shape.
        // Move the controls to fit that shape without stretching the letters.
        private void ResizePanels(float height)
        {
            float stretch = height / 900f;
            ((RectTransform)transform).sizeDelta = new Vector2(1600, height);
            foreach (CanvasGroup panel in new[] { standbyScreen, bootScreen, menuScreen, settingsScreen, guideScreen, quitScreen, loadingScreen })
            {
                ((RectTransform)panel.transform).sizeDelta = new Vector2(1600, height);
                foreach (RectTransform child in panel.transform)
                {
                    Vector2 position = child.anchoredPosition;
                    position.y *= stretch;
                    child.anchoredPosition = position;
                    if (child.name == "POST background") child.sizeDelta = new Vector2(1596, height - 82 + position.y);
                }
            }
            bootText.rectTransform.sizeDelta = new Vector2(1350, 550 * stretch);
        }

        private void ResizeFrame(float height)
        {
            // These names come from MainMenu.unity. They are the screen's border and footer.
            foreach (string name in new[] { "Monitor face", "Left frame", "Right frame" })
            {
                var rect = (RectTransform)transform.Find(name);
                rect.sizeDelta = new Vector2(rect.sizeDelta.x, height);
            }
            foreach (string name in new[] { "Footer band", "Footer rule", "Footer left", "Footer right", "Bottom frame" })
            {
                var rect = (RectTransform)transform.Find(name);
                rect.anchoredPosition += Vector2.down * (height - 900);
            }
        }

        private void CreateInvitation(float aspect)
        {
            var face = new GameObject("Wake screen", typeof(RectTransform), typeof(Image), typeof(TerminalButton));
            face.transform.SetParent(standbyScreen.transform, false);
            var faceRect = (RectTransform)face.transform;
            faceRect.anchorMin = Vector2.zero;
            faceRect.anchorMax = Vector2.one;
            faceRect.offsetMin = faceRect.offsetMax = Vector2.zero;
            dormantFace = face.GetComponent<Image>();
            wakeMaterial = new Material(Resources.Load<Material>("TerminalWake"));
            wakeMaterial.SetFloat("_Aspect", aspect);
            dormantFace.material = wakeMaterial;
            dormantFace.color = Color.white;
            var tap = face.GetComponent<TerminalButton>();
            tap.transition = Selectable.Transition.None;
            tap.onClick.AddListener(PowerOn);
            powerButton = face;
            invitation = new GameObject("Invitation", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
            invitation.transform.SetParent(face.transform, false);
            invitation.font = bootText.font;
            invitation.fontSize = 42;
            invitation.alignment = TextAlignmentOptions.Center;
            invitation.rectTransform.sizeDelta = new Vector2(1000, 130);
            invitation.rectTransform.anchoredPosition = new Vector2(0, -105);
            invitation.raycastTarget = false;
        }

        private void UpdateInvitation()
        {
            float elapsed = Time.unscaledTime - dormantSince;
            float phase = Mathf.Repeat(elapsed / 3.6f, 1);
            float pulse = GamePreferences.ReducedMotion ? .55f : Mathf.Pow(.5f + .5f * Mathf.Cos(phase * Mathf.PI * 2), 2);
            bool hovering = Mouse.current != null && display.TryGetContentPoint(Mouse.current.position.ReadValue(), out Vector2 point) &&
                Vector2.Distance(new Vector2(point.x / display.SourceCamera.targetTexture.width, point.y / display.SourceCamera.targetTexture.height), new Vector2(.5f, .56f)) < .13f;
            wakeHover = Mathf.MoveTowards(wakeHover, hovering ? 1 : 0, Time.unscaledDeltaTime * 3);
            wakeMaterial.SetFloat("_Phase", phase);
            wakeMaterial.SetFloat("_Pulse", pulse);
            wakeMaterial.SetFloat("_Hover", wakeHover);
            wakeMaterial.SetFloat("_ReducedMotion", GamePreferences.ReducedMotion ? 1 : 0);
            invitation.text = Invitations[Mathf.FloorToInt(elapsed / 3.6f) % Invitations.Length];
            invitation.color = new Color(.52f, .68f, 1f, .48f + pulse * .42f);
            display.SetDormant(true, pulse);
        }

        private void OnDestroy()
        {
            if (wakeMaterial != null) Destroy(wakeMaterial);
        }

        private void Update()
        {
            if (!TerminalShipIntro.Ready || invitation == null) return;
            if (Screen.width != lastWidth || Screen.height != lastHeight)
                FitCamera();
            if (TerminalTransition.IsActive)
                return;
            if (State == PowerState.WarmingUp) return;
            Keyboard keyboard = Keyboard.current;
            Gamepad pad = Gamepad.current;
            if (waitingForRelease)
            {
                UpdateInputRelease(keyboard, pad);
                return;
            }
            if (State == PowerState.WaitingForTouch)
            {
                UpdateInvitation();
                if (keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame))
                {
                    TerminalInteractionAudio.Instance?.Keyboard();
                    PowerOn();
                }
                return;
            }
            if (State == PowerState.PoweringOn || State == PowerState.PoweringDown || State == PowerState.InGame || State == PowerState.Exiting)
                return;
            bool back = (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) || (pad != null && pad.buttonEast.wasPressedThisFrame);
            if (State == PowerState.Booting)
            {
                // Enter belongs to the power/confirm path. It never skips into an active menu.
                if (back || (keyboard != null && keyboard.spaceKey.wasPressedThisFrame))
                {
                    TerminalInteractionAudio.Instance?.Keyboard();
                    SkipBoot();
                }
                return;
            }
            if (back)
            {
                TerminalInteractionAudio.Instance?.Keyboard();
                if (currentScreen == menuScreen)
                    OpenQuit();
                else
                    BackToMenu();
            }
        }

        // Require a fresh press after changing screens. Holding Enter must not also start a game.
        private void UpdateInputRelease(Keyboard keyboard, Gamepad pad)
        {
            bool keyboardHeld = keyboard != null &&
                (keyboard.enterKey.isPressed || keyboard.numpadEnterKey.isPressed || keyboard.spaceKey.isPressed);
            bool mouseHeld = Mouse.current != null && Mouse.current.leftButton.isPressed;
            bool controllerHeld = pad != null && pad.buttonSouth.isPressed;

            if (keyboardHeld || mouseHeld || controllerHeld)
                releaseAt = Time.unscaledTime + 0.25f;
            if (Time.unscaledTime < releaseAt)
                return;

            waitingForRelease = false;
            currentScreen.interactable = true;
            powerButton.GetComponent<Button>().interactable = true;
            inputModule.enabled = true;
            EventSystem.current.SetSelectedGameObject(State == PowerState.WaitingForTouch ? powerButton : startButton);
        }

        private void FitCamera()
        {
            lastWidth = Screen.width;
            lastHeight = Screen.height;
            if (display.IsShipScreen) { TerminalShipIntro.FitCamera(); return; }
            if (menuCamera == null) return;
            float halfHeight = Mathf.Max(5.56f, 9.15f / menuCamera.aspect);
            float distance = Vector3.Distance(menuCamera.transform.position, Vector3.zero);
            menuCamera.fieldOfView = 2f * Mathf.Atan(halfHeight / distance) * Mathf.Rad2Deg;
        }

        private void ChangeState(PowerState value)
        {
            State = value;
            if (hardware != null) hardware.ApplyState(value);
        }

        // Kept for the older cabinet prefab. The current telescope uses tap-to-wake.
        public void TogglePower()
        {
            if (!CanTogglePower) return;
            if (State == PowerState.WaitingForTouch) PowerOn();
            else PowerOff();
        }

        public void PowerOn()
        {
            if (State != PowerState.WaitingForTouch || !CanTogglePower) return;
            nextPowerInput = Time.unscaledTime + .18f;
            waitingForRelease = false;
            returnSelection = null;
            titleAnimation.ResetForBoot();
            titleAnimation.enabled = true;
            bootLog.ResetLog();
            bootLog.Stop();
            bootText.text = "";
            bootProgress.text = "";
            display.SetDormant(false);
            ChangeState(PowerState.PoweringOn);
            EventSystem.current.SetSelectedGameObject(null);
            bootRoutine = StartCoroutine(StartDiagnostics());
        }

        public void PowerOff()
        {
            if (!CanTogglePower || State == PowerState.WaitingForTouch) return;
            nextPowerInput = Time.unscaledTime + .18f;
            if (bootRoutine != null) StopCoroutine(bootRoutine);
            bootRoutine = null;
            waitingForRelease = false;
            ChangeState(PowerState.PoweringDown);
            powerButton.GetComponent<Button>().interactable = false;
            currentScreen.interactable = false;
            EventSystem.current.SetSelectedGameObject(null);
            titleAnimation.enabled = false;
            typingAudio.Stop();
            bootLog.Stop();
            bootLog.ResetLog();
            bootProgress.text = "";
            TerminalInteractionAudio.Instance?.Switch();
            PlayerPrefs.Save();
            bootRoutine = StartCoroutine(ShutDown());
        }

        private IEnumerator StartDiagnostics()
        {
            yield return new WaitForSecondsRealtime(0.12f);
            if (!screenWarm) powerAudio?.PowerOn();
            yield return new WaitForSecondsRealtime(0.23f);
            sourceCanvas.enabled = true;
            if (!screenWarm) display.PowerOn();
            bootText.text = "";
            bootProgress.text = "";
            ShowScreen(bootScreen, null);
            float duration = screenWarm || GamePreferences.ReducedMotion ? 0.18f : WarmupDuration;
            for (float elapsed = 0; elapsed < duration; elapsed += Time.unscaledDeltaTime)
            {
                float amount = screenWarm ? 1f : Mathf.SmoothStep(0f,1f,elapsed/duration);
                display.SetPower(amount);
                if (hardware != null) hardware.SetIllumination(amount);
                yield return null;
            }
            display.SetPower(1f);
            screenWarm = true;
            if (hardware != null) hardware.SetIllumination(1f);
            ChangeState(PowerState.Booting);
            menuMusic.Play();
            inputModule.enabled = true;
            SetBootProgress(0f);
            if (GamePreferences.PlayBoot)
            {
                bootLog.Begin();
                while (bootLog.Running)
                {
                    SetBootProgress(bootLog.Progress);
                    yield return null;
                }
                // Finish the fictional ship diagnostics before opening the operator menu.
                SetBootProgress(1f);
                yield return new WaitForSecondsRealtime(.3f);
            }
            bootRoutine = null;
            OpenMainAfterBoot();
        }

        public void SkipBoot()
        {
            if (State != PowerState.Booting)
                return;
            if (bootRoutine != null)
                StopCoroutine(bootRoutine);
            bootRoutine = null;
            OpenMainAfterBoot();
        }

        private void SetBootProgress(float progress)
        {
            int percent = Mathf.Clamp(Mathf.FloorToInt(progress * 100f), 0, 100);
            int filled = percent * 12 / 100;
            bootProgress.text = "[" + new string('=', filled) + new string('.', 12 - filled) + "]  SYSTEM CHECK " + percent.ToString().PadLeft(3) + "%";
        }

        private void OpenMainAfterBoot()
        {
            bootLog.Stop();
            typingAudio.Stop();
            ChangeState(PowerState.MainMenu);
            ShowScreen(menuScreen,startButton);
            WaitForRelease();
        }

        // A held or rapidly repeated activation cannot carry into Begin Survey.
        private void WaitForRelease()
        {
            waitingForRelease = true;
            releaseAt = Time.unscaledTime + 0.25f;
            currentScreen.interactable = false;
            EventSystem.current.SetSelectedGameObject(null);
        }

        public void OpenSettings()
        {
            if (State != PowerState.MainMenu || waitingForRelease) return;
            returnSelection = EventSystem.current.currentSelectedGameObject;
            ShowScreen(settingsScreen, settings.FirstControl);
        }

        public void OpenGuide()
        {
            if (State != PowerState.MainMenu || waitingForRelease) return;
            returnSelection = EventSystem.current.currentSelectedGameObject;
            ShowScreen(guideScreen, guideBackButton);
        }

        public void OpenQuit()
        {
            if (State != PowerState.MainMenu || waitingForRelease) return;
            returnSelection = EventSystem.current.currentSelectedGameObject;
            ShowScreen(quitScreen, cancelQuitButton);
        }

        public void BackToMenu()
        {
            PlayerPrefs.Save();
            ShowScreen(menuScreen, returnSelection != null ? returnSelection : startButton);
        }

        public void BeginSurvey()
        {
            if (State != PowerState.MainMenu || waitingForRelease || currentScreen != menuScreen)
                return;
            if (!Application.CanStreamedLevelBeLoaded(gameplayScene))
            {
                Debug.LogError("The menu destination is not enabled in Build Profiles: " + gameplayScene, this);
                return;
            }
            ChangeState(PowerState.InGame);
            powerButton.GetComponent<Button>().interactable = false;
            inputModule.enabled = false;
            menuMusic.FadeOut();
            powerAudio?.FadeOut(menuMusic.FadeOutDuration);
            PlayerPrefs.Save();
            StartCoroutine(HandOverToShip());
        }

        private IEnumerator HandOverToShip()
        {
            ShowScreen(loadingScreen, null);
            loadingText.text = "OPENING SURVEY SESSION...";
            yield return new WaitForSecondsRealtime(menuMusic.FadeOutDuration);
            loadingText.text = "RELEASING OPERATOR CONTROLS...";
            // The ship is already loaded. Let TerminalShipIntro move the camera and restore gameplay.
            TerminalShipIntro.BeginSurvey();
        }

        // The terminal menu exits the desktop application; the hardware switch never calls Quit.
        public void ConfirmQuit()
        {
            if (State != PowerState.MainMenu || currentScreen != quitScreen) return;
            ExitRequested = true;
            ChangeState(PowerState.Exiting);
            inputModule.enabled = false;
            powerButton.GetComponent<Button>().interactable = false;
            currentScreen.interactable = false;
            menuMusic.Stop();
            powerAudio?.Stop();
            typingAudio.Stop();
            GetComponent<AudioSource>().Stop();
            PlayerPrefs.Save();
            Application.Quit();
        }

        private IEnumerator ShutDown()
        {
            menuMusic.FadeOut();
            powerAudio?.FadeOut(menuMusic.FadeOutDuration);
            float initial = display.Illumination;
            float duration = menuMusic.FadeOutDuration;
            for (float elapsed = 0; elapsed < duration; elapsed += Time.unscaledDeltaTime)
            {
                float amount = initial * (1f - Mathf.SmoothStep(0,1,elapsed/duration));
                display.SetPower(amount);
                if (hardware != null) hardware.SetIllumination(amount);
                yield return null;
            }
            menuMusic.Stop();
            powerAudio?.Stop();
            display.SetPower(0f);
            if (hardware != null) hardware.SetIllumination(0f);
            sourceCanvas.enabled = true;
            screenWarm = false;
            yield return WarmScreen();
            bootRoutine = null;
        }

        public void SetReducedMotion(bool value)
        {
            titleAnimation.SetReducedMotion(value);
            display.ReducedMotion = value;
            GamePreferences.SetReducedMotion(value);
        }

        private void ShowScreen(CanvasGroup screen, GameObject selection)
        {
            // Turn off the old panel's input too, so hidden buttons cannot receive clicks.
            if (currentScreen != null)
                SetVisible(currentScreen, false);
            currentScreen = screen;
            SetVisible(screen, true);
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(selection);
        }

        private void SetVisible(CanvasGroup screen, bool visible)
        {
            screen.gameObject.SetActive(visible);
            screen.alpha = visible ? 1f : 0f;
            screen.interactable = visible;
            screen.blocksRaycasts = visible;
        }
    }
}
