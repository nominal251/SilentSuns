using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.DualShock;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

namespace SilentSuns.Menu
{
    // Check Escape before the player scripts read this frame's input.
    [DefaultExecutionOrder(-100)]
    public class TerminalPause : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private GameObject confirmationPanel;
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private TerminalSettings settings;
        [SerializeField] private GameObject settingsButton;
        [SerializeField] private CanvasGroup visibility;
        [SerializeField] private GameObject resumeButton;
        [SerializeField] private GameObject cancelButton;
        [SerializeField] private TMP_Text confirmationText;
        [SerializeField] private TMP_Text inputHint;
        [SerializeField] private string menuScene = "MainMenu";

        private static TerminalPause instance;
        private static int resumeFrame = -1;
        private bool paused;
        private bool closing;
        private Coroutine fadeRoutine;
        private bool quitRequested;
        private float previousTimeScale;
        private bool previousAudioPause;
        private CursorLockMode previousCursorLock;
        private bool previousCursorVisible;
        private GameObject previousSelection;
        private int inputScheme = -1;
        private bool keyboardNavigation;
        private int pendingNavigation;
        private GameObject leaveSelection;

        public static bool Available => instance != null;
        public static bool Paused => instance != null && instance.paused;
        public static void PauseForDisconnect() { if (instance != null && !instance.paused) instance.Pause(); }
        // PlayerMovement and InteractOrigin use this to ignore gameplay input during menus.
        public static bool BlocksGameplayInput => TerminalTransition.IsActive || TerminalShipIntro.BlocksInput || TerminalModal.BlocksInput || TerminalFailure.Active || TerminalExtras.ShuttingDown ||
            (instance != null && instance.paused) || Time.frameCount <= resumeFrame;
        public bool IsPaused => paused;

        // Install for the survey scene, including when it is played directly in the editor.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            instance = null;
            resumeFrame = -1;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != "SampleScene" || instance != null)
                return;
            GameObject prefab = Resources.Load<GameObject>("SilentSunsPause");
            if (prefab != null)
                Instantiate(prefab);
        }

        private void Awake()
        {
            instance = this;
            pausePanel.SetActive(false);
            confirmationPanel.SetActive(false);
            settingsPanel.SetActive(false);
            visibility.alpha = 0f;
            if (EventSystem.current == null)
            {
                GameObject events = new GameObject("Pause Input", typeof(EventSystem), typeof(InputSystemUIInputModule));
                events.transform.SetParent(transform, false);
                events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            }
            SetInputScheme(0);
            TerminalTextScale.Install(transform);
            foreach (var button in confirmationPanel.GetComponentsInChildren<TerminalButton>(true))
                if (button.gameObject != cancelButton) TerminalHoldConfirm.Attach(button.gameObject, ConfirmLeave);
        }

        // Escape and the controller's menu button toggle pause. P has no pause binding.
        private void Update()
        {
            if (closing || TerminalTransition.IsActive || TerminalShipIntro.IsActive || TerminalModal.BlocksInput || TerminalDropdown.BlocksInput ||
                TerminalBindings.Listening || Time.frameCount <= TerminalBindings.ConsumedFrame || TerminalFailure.Active || TerminalExtras.ShuttingDown)
                return;
            Keyboard keyboard = Keyboard.current;
            Gamepad gamepad = Gamepad.current;
            bool escape = keyboard != null && keyboard.escapeKey.wasPressedThisFrame;
            bool start = gamepad != null && gamepad.startButton.wasPressedThisFrame;
            bool back = gamepad != null && gamepad.buttonEast.wasPressedThisFrame;
            UpdateInputScheme(keyboard, gamepad, start, back);
            UpdateNavigation(keyboard, gamepad);

            if (escape || start || (paused && back))
            {
                TerminalInteractionAudio.Instance?.Keyboard();
                if (paused && confirmationPanel.activeSelf)
                    CancelLeave();
                else if (paused && settingsPanel.activeSelf)
                    CloseSettings();
                else if (paused)
                    Resume();
                else
                    Pause();
            }
        }

        private void UpdateInputScheme(Keyboard keyboard, Gamepad gamepad, bool start, bool back)
        {
            bool usedKeyboard = keyboard != null && keyboard.anyKey.wasPressedThisFrame;
            bool movedMouse = Mouse.current != null && Mouse.current.delta.ReadValue().sqrMagnitude >= 4f;
            if (usedKeyboard || movedMouse)
                SetInputScheme(0);
            if (gamepad != null && (start || back || gamepad.buttonSouth.wasPressedThisFrame || gamepad.dpad.ReadValue().sqrMagnitude > 0.25f || gamepad.leftStick.ReadValue().sqrMagnitude > 0.25f))
                SetInputScheme(gamepad is DualShockGamepad ? 2 : 1);
        }

        private void UpdateNavigation(Keyboard keyboard, Gamepad gamepad)
        {
            pendingNavigation = 0;
            if (!paused)
                return;

            Mouse pointer = Mouse.current;
            if (pointer != null && (pointer.delta.ReadValue().sqrMagnitude >= 4f || pointer.leftButton.wasPressedThisFrame))
            {
                // Mouse hover takes over from keyboard selection, so two buttons do not stay lit.
                keyboardNavigation = false;
                EventSystem.current.SetSelectedGameObject(null);
            }
            Vector2 direction = gamepad == null ? Vector2.zero : gamepad.dpad.ReadValue() + gamepad.leftStick.ReadValue();
            if (keyboard != null)
            {
                if (keyboard.downArrowKey.wasPressedThisFrame || keyboard.rightArrowKey.wasPressedThisFrame) direction.y = -1;
                if (keyboard.upArrowKey.wasPressedThisFrame || keyboard.leftArrowKey.wasPressedThisFrame) direction.y = 1;
            }
            if (direction.sqrMagnitude > .25f)
            {
                keyboardNavigation = true;
                if (EventSystem.current.currentSelectedGameObject == null)
                    pendingNavigation = direction.y > 0 ? -1 : 1;
            }
        }

        private void LateUpdate()
        {
            // Wait until Unity finishes reading input. Otherwise the first arrow press skips a button.
            if (!paused || closing || pendingNavigation == 0 || EventSystem.current.currentSelectedGameObject != null)
                return;
            GameObject first = resumeButton;
            if (settingsPanel.activeSelf)
                first = settings.FirstControl;
            else if (confirmationPanel.activeSelf)
                first = cancelButton;
            if (pausePanel.activeSelf && pendingNavigation < 0)
            {
                var buttons = pausePanel.GetComponentsInChildren<TerminalButton>();
                if (buttons.Length > 0) first = buttons[buttons.Length - 1].gameObject;
            }
            EventSystem.current.SetSelectedGameObject(first);
            pendingNavigation = 0;
        }

        public void Pause()
        {
            if (paused || TerminalTransition.IsActive || TerminalShipIntro.IsActive)
                return;
            // Menu sounds still play while AudioListener.pause stops gameplay sounds.
            GetComponent<TerminalInteractionAudio>().TakeFocus();
            previousTimeScale = Time.timeScale;
            previousAudioPause = AudioListener.pause;
            previousCursorLock = Cursor.lockState;
            previousCursorVisible = Cursor.visible;
            previousSelection = EventSystem.current.currentSelectedGameObject;
            paused = true;
            Time.timeScale = 0f;
            AudioListener.pause = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            pausePanel.SetActive(true);
            visibility.interactable = true;
            FadeTo(1f);
            keyboardNavigation = false;
            pendingNavigation = 0;
            EventSystem.current.SetSelectedGameObject(null);
            var subtitle = pausePanel.GetComponentsInChildren<TMP_Text>();
            foreach (var label in subtitle)
                if (label.text.StartsWith("SESSION HELD"))
                    label.text = "SESSION HELD IN MEMORY / ACTIVE TIME " + System.TimeSpan.FromSeconds(TerminalExtras.SessionSeconds).ToString(@"hh\:mm\:ss");
        }

        // Keep the resume click from reaching an instrument behind the menu on this frame.
        public void Resume()
        {
            if (!paused || closing)
                return;
            if (settingsPanel.activeSelf && settings.HasChanges)
            {
                settings.RequestLeave(Resume);
                return;
            }
            closing = true;
            visibility.interactable = false;
            FadeTo(0f);
        }

        public void OpenSettings()
        {
            pausePanel.SetActive(false);
            settingsPanel.SetActive(true);
            EventSystem.current.SetSelectedGameObject(settings.FirstControl);
        }

        public void CloseSettings()
        {
            settings.RequestLeave(() =>
            {
                settingsPanel.SetActive(false);
                pausePanel.SetActive(true);
                EventSystem.current.SetSelectedGameObject(keyboardNavigation ? settingsButton : null);
            });
        }

        private void FadeTo(float target)
        {
            if (fadeRoutine != null)
                StopCoroutine(fadeRoutine);
            fadeRoutine = StartCoroutine(Fade(target));
        }

        // Freeze immediately on opening; resume only after the overlay has cleared.
        private IEnumerator Fade(float target)
        {
            float from = visibility.alpha;
            float duration = GamePreferences.ReducedMotion ? 0.01f : 0.24f;
            for (float elapsed = 0; elapsed < duration; elapsed += Time.unscaledDeltaTime)
            {
                visibility.alpha = Mathf.SmoothStep(from, target, elapsed / duration);
                yield return null;
            }
            visibility.alpha = target;
            if (closing)
            {
                pausePanel.SetActive(false);
                confirmationPanel.SetActive(false);
                settingsPanel.SetActive(false);
                RestoreSession();
                resumeFrame = Time.frameCount;
                EventSystem.current.SetSelectedGameObject(previousSelection);
                closing = false;
            }
            fadeRoutine = null;
        }

        public void AskReturnToMenu()
        {
            AskLeave(false);
        }

        public void AskQuit()
        {
            AskLeave(true);
        }

        private void AskLeave(bool quit)
        {
            quitRequested = quit;
            leaveSelection = EventSystem.current.currentSelectedGameObject;
            pausePanel.SetActive(false);
            confirmationPanel.SetActive(true);
            confirmationText.text = quit ? "EXIT THE GAME?\nCURRENT SURVEY PROGRESS WILL BE LOST." : "RETURN TO MAIN MENU?\nCURRENT SURVEY PROGRESS WILL BE LOST.";
            EventSystem.current.SetSelectedGameObject(cancelButton);
        }

        public void CancelLeave()
        {
            confirmationPanel.SetActive(false);
            pausePanel.SetActive(true);
            EventSystem.current.SetSelectedGameObject(keyboardNavigation ? leaveSelection : null);
        }

        public void ConfirmLeave()
        {
            var hold = confirmationPanel.GetComponentInChildren<TerminalHoldConfirm>();
            if (hold != null && !hold.Completed) return;
            PlayerPrefs.Save();
            if (quitRequested)
                TerminalExtras.Shutdown();
            else
            {
                visibility.interactable = false;
                TerminalTransition.LoadScene(menuScene, RestoreSession);
            }
        }

        private void SetInputScheme(int scheme)
        {
            if (inputScheme == scheme)
                return;
            inputScheme = scheme;
            inputHint.text = scheme == 0 ? "ESC  RESUME     ARROWS  SELECT     ENTER  CONFIRM" :
                scheme == 2 ? "OPTIONS / CIRCLE  RESUME     CROSS  CONFIRM" : "START / B  RESUME     A  CONFIRM";
        }

        private void RestoreSession()
        {
            if (!paused)
                return;
            Time.timeScale = previousTimeScale;
            AudioListener.pause = previousAudioPause;
            Cursor.lockState = previousCursorLock;
            Cursor.visible = previousCursorVisible;
            paused = false;
        }

        private void OnDestroy()
        {
            RestoreSession();
            if (instance == this)
                instance = null;
        }
    }
}
