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
        public static TerminalMenu Active { get; private set; }
        private static bool returningToMenu;
        public static void PrepareReturn() { returningToMenu = true; }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetReturn() { returningToMenu = false; }
        public bool IdleAllowed => State == PowerState.MainMenu && currentScreen == menuScreen && !waitingForRelease;
        private TerminalScreensaver screensaver;
        private void Awake() { Active = this; }
        // This script runs the UI. TerminalShipIntro handles the ship and player camera.
        [Header("Screens")]
        [SerializeField] private CanvasGroup standbyScreen;
        [SerializeField] private CanvasGroup bootScreen;
        [SerializeField] private CanvasGroup menuScreen;
        [SerializeField] private CanvasGroup settingsScreen;
        [SerializeField] private CanvasGroup guideScreen;
        [SerializeField] private CanvasGroup quitScreen;
        [SerializeField] private CanvasGroup loadingScreen;
        private GameObject wakeButton;
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
        [SerializeField] private string gameplayScene = "SampleScene";

        [Header("Audio")]
        [SerializeField] private TerminalMenuMusic menuMusic;
        [SerializeField] private TerminalPowerAudio powerAudio;
        public const float WarmupDuration = 2.4f;
        public const float ArrivalDuration = 3.6f;

        [Header("UI and input")]
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
            Exiting = 6,
            WarmingUp = 7
        }
        public PowerState State { get; private set; } = PowerState.WarmingUp;
        public bool InvitationReady => State == PowerState.WaitingForTouch && !waitingForRelease;
        // The team has not connected a gameplay save system yet.
        public bool CanRestore => false;
        public bool ExitRequested { get; private set; }
        private bool CanWake => State == PowerState.WaitingForTouch &&
            !TerminalTransition.IsActive && Time.unscaledTime >= nextPowerInput;
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
            TerminalTextScale.Install(transform);
            GamePreferences.Changed += RefreshPreferences;
            screensaver = gameObject.AddComponent<TerminalScreensaver>();
            screensaver.Initialize(this, menuScreen);
            foreach (var button in quitScreen.GetComponentsInChildren<TerminalButton>(true))
                if (button.gameObject != cancelQuitButton) TerminalHoldConfirm.Attach(button.gameObject, ConfirmQuit);
            if (returningToMenu)
            {
                // A session exit opens the choices directly. Cold startup is only for launching the game.
                returningToMenu = false;
                screenWarm = true;
                display.SetDormant(false);
                display.SetStandbyWarmup(0);
                display.SetPower(1);
                powerAudio?.PlayHum();
                menuMusic.Play();
                inputModule.enabled = true;
                titleAnimation.ShowComplete();
                OpenMainAfterBoot();
            }
            else yield return WarmScreen();
        }

        private IEnumerator WarmScreen()
        {
            ChangeState(PowerState.WarmingUp);
            ShowScreen(standbyScreen, null);
            inputModule.enabled = false;
            wakeButton.GetComponent<Button>().interactable = false;
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

        public void ConfigureShipScreen(float aspect)
        {
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
            wakeButton = face;
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
            if (Active == this) Active = null;
            GamePreferences.Changed -= RefreshPreferences;
        }

        private void Update()
        {
            if (!TerminalShipIntro.Ready || invitation == null) return;
            if (TerminalModal.BlocksInput || TerminalDropdown.BlocksInput || TerminalBindings.Listening || Time.frameCount <= TerminalBindings.ConsumedFrame || TerminalExtras.ShuttingDown) return;
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
            if (State == PowerState.PoweringOn || State == PowerState.InGame || State == PowerState.Exiting)
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
            wakeButton.GetComponent<Button>().interactable = true;
            inputModule.enabled = true;
            EventSystem.current.SetSelectedGameObject(State == PowerState.WaitingForTouch ? wakeButton : (returnSelection != null ? returnSelection : startButton));
        }

        private void FitCamera()
        {
            lastWidth = Screen.width;
            lastHeight = Screen.height;
            TerminalShipIntro.FitCamera();
        }

        private void ChangeState(PowerState value)
        {
            State = value;
        }

        public void PowerOn()
        {
            if (!CanWake) return;
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
                float amount = screenWarm ? 1f : Mathf.SmoothStep(0f, 1f, elapsed / duration);
                display.SetPower(amount);
                yield return null;
            }
            display.SetPower(1f);
            screenWarm = true;
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
            ShowScreen(menuScreen, startButton);
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
            if (currentScreen == settingsScreen)
            {
                settings.RequestLeave(() => ShowScreen(menuScreen, returnSelection != null ? returnSelection : startButton));
                return;
            }
            ShowScreen(menuScreen, returnSelection != null ? returnSelection : startButton);
        }

        public void BeginSurvey()
        {
            if (screensaver != null && screensaver.BlocksActivation) return;
            if (State != PowerState.MainMenu || waitingForRelease || currentScreen != menuScreen)
                return;
            if (!Application.CanStreamedLevelBeLoaded(gameplayScene))
            {
                Debug.LogError("The menu destination is not enabled in Build Profiles: " + gameplayScene, this);
                return;
            }
            TerminalGameSession.StartNewRun();
            ChangeState(PowerState.InGame);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            wakeButton.GetComponent<Button>().interactable = false;
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

        // The confirmation button saves settings and closes the application.
        public void ConfirmQuit()
        {
            if (State != PowerState.MainMenu || currentScreen != quitScreen) return;
            var hold = quitScreen.GetComponentInChildren<TerminalHoldConfirm>();
            if (hold != null && !hold.Completed) return;
            ExitRequested = true;
            ChangeState(PowerState.Exiting);
            inputModule.enabled = false;
            wakeButton.GetComponent<Button>().interactable = false;
            currentScreen.interactable = false;
            menuMusic.FadeOut();
            powerAudio?.FadeOut(1.5f);
            typingAudio.Stop();
            GetComponent<AudioSource>().Stop();
            PlayerPrefs.Save();
            TerminalExtras.Shutdown();
        }

        public void SetReducedMotion(bool value)
        {
            titleAnimation.SetReducedMotion(value);
            display.ReducedMotion = value;
            GamePreferences.SetReducedMotion(value);
        }

        private void RefreshPreferences()
        {
            titleAnimation.SetReducedMotion(GamePreferences.ReducedMotion);
            display.ReducedMotion = GamePreferences.ReducedMotion;
            FitCamera();
        }

        public void WakeFromIdle(GameObject selection)
        {
            returnSelection = selection;
            ShowScreen(menuScreen, selection != null ? selection : startButton);
            WaitForRelease();
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
