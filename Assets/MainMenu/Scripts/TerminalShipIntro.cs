using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SilentSuns.Menu
{
    // Loads the ship, puts the menu on its telescope, then gives control back to the player.
    public class TerminalShipIntro : MonoBehaviour
    {
        private static TerminalShipIntro active;
        private static int releaseFrame = -1;
        public static bool IsActive => active != null;
        public static bool BlocksInput => IsActive || Time.frameCount <= releaseFrame;
        public static bool Ready => active != null && active.ready;
        public static bool HintVisible { get; private set; }
        private bool ready;
        private bool handingOff;
        private bool released;
        private TerminalMenu menu;
        private TerminalDisplay display;
        private Camera playerCamera;
        private Rigidbody body;
        private bool wasKinematic;
        private Vector3 cameraLocalPosition;
        private Quaternion cameraLocalRotation;
        private float cameraFov, cameraNear, previousTimeScale;
        private Canvas hint;
        private Canvas[] playerHud;
        private GameObject events;
        private GameObject gameplayEvents;
        private TerminalRoomLighting roomLighting;
        public float RoomLightLevel => roomLighting != null ? roomLighting.Level : 1;
        public float Illumination => display != null ? display.Illumination : 0;
        public Renderer Screen { get; private set; }

        private const float HandoffDuration = 6.2f;
        private const float RecoveryRamp = .18f;
        private const string TelescopeScreenPath = "Telescope/ProbeLauncherScreen";
        private readonly System.Collections.Generic.List<(float end, HandoffLook look)> recovery = new System.Collections.Generic.List<(float, HandoffLook)>();

        // These four settings change together during the flickering transition.
        private struct HandoffLook
        {
            public float telescopeVisibility;
            public float glitchStrength;
            public float screenBrightness;
            public float roomBrightness;

            public HandoffLook(float telescopeVisibility, float glitchStrength, float screenBrightness, float roomBrightness)
            {
                this.telescopeVisibility = telescopeVisibility;
                this.glitchStrength = glitchStrength;
                this.screenBrightness = screenBrightness;
                this.roomBrightness = roomBrightness;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            active = null;
            HintVisible = false;
            releaseFrame = -1;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        // Unity calls this after loading a scene. Starting SampleScene by itself skips the intro.
        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != "SampleScene" && scene.name != "MainMenu" && active != null)
            {
                Destroy(active.gameObject);
                active = null;
            }
            if (scene.name == "SampleScene" && active != null)
            {
                if (!active.ready) active.SetUpShipScreen();
                else { Destroy(active.gameObject); active = null; }
                return;
            }
            if (scene.name != "MainMenu") return;
            if (active != null) { active.gameObject.SetActive(false); Destroy(active.gameObject); }
            var root = new GameObject("Telescope startup session");
            DontDestroyOnLoad(root);
            active = root.AddComponent<TerminalShipIntro>();
            active.StartCoroutine(active.LoadShipForIntro());
        }

        private IEnumerator LoadShipForIntro()
        {
            previousTimeScale = Time.timeScale > 0 ? Time.timeScale : 1;
            Time.timeScale = 0;
            foreach (GameObject sceneRoot in SceneManager.GetSceneByName("MainMenu").GetRootGameObjects())
            {
                menu = sceneRoot.GetComponentInChildren<TerminalMenu>();
                if (menu != null) break;
            }
            display = menu.GetComponentInChildren<TerminalDisplay>(true);
            // Keep the UI while MainMenu unloads. Its camera draws a texture for the telescope.
            menu.transform.SetParent(transform, true);
            display.SourceCamera.transform.SetParent(transform, true);
            menu.transform.position += Vector3.back * 10000;
            display.SourceCamera.transform.position += Vector3.back * 10000;
            var listener = display.SourceCamera.GetComponent<AudioListener>();
            if (listener != null) listener.enabled = false;
            foreach (GameObject sceneRoot in SceneManager.GetSceneByName("MainMenu").GetRootGameObjects())
                if (sceneRoot.GetComponent<EventSystem>() != null) { events = sceneRoot; break; }
            events.transform.SetParent(transform, true);
            Camera bootstrapCamera = Camera.main;
            bootstrapCamera.cullingMask = 0;
            bootstrapCamera.backgroundColor = Color.black;
            bootstrapCamera.tag = "Untagged";
            yield return SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Single);
        }

        private void SetUpShipScreen()
        {
            // Use the menu's input system for now. Two active EventSystems would handle each click twice.
            foreach (EventSystem input in FindObjectsByType<EventSystem>(FindObjectsSortMode.None))
                if (input.gameObject != events && input.gameObject.scene == SceneManager.GetActiveScene())
                {
                    gameplayEvents = input.gameObject;
                    gameplayEvents.SetActive(false);
                    break;
                }
            // This path is the existing screen in SampleScene. Update it if the team renames that object.
            var target = GameObject.Find(TelescopeScreenPath);
            playerCamera = Camera.main;
            if (target == null || playerCamera == null)
            {
                Debug.LogError("Telescope startup requires the existing Telescope/ProbeLauncherScreen and player camera.");
                return;
            }
            Screen = target.GetComponent<Renderer>();
            // Save the normal lighting before TerminalDisplay adds the screen glow.
            roomLighting = new TerminalRoomLighting(Screen, playerCamera);
            display.BindToShip(Screen, playerCamera);
            menu.ConfigureShipScreen(display.ScreenRight.magnitude / display.ScreenUp.magnitude);
            cameraLocalPosition = playerCamera.transform.localPosition;
            cameraLocalRotation = playerCamera.transform.localRotation;
            cameraFov = playerCamera.fieldOfView;
            cameraNear = playerCamera.nearClipPlane;
            Vector3 normal = -Vector3.Cross(display.ScreenRight, display.ScreenUp).normalized;
            Quaternion facing = Quaternion.LookRotation(-normal, display.ScreenUp.normalized);
            body = playerCamera.GetComponentInParent<Rigidbody>();
            if (body != null)
            {
                wasKinematic = body.isKinematic;
                body.isKinematic = true;
                body.transform.SetPositionAndRotation(Screen.bounds.center + normal * 2.35f, facing);
            }
            playerHud = System.Array.FindAll(FindObjectsByType<Canvas>(FindObjectsSortMode.None), IsGameplayHud);
            foreach (Canvas hud in playerHud) hud.enabled = false;
            playerCamera.cullingMask &= ~(1 << 31);
            playerCamera.nearClipPlane = .01f;
            playerCamera.fieldOfView = 48;
            ready = true;
            FitCamera();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private static bool IsGameplayHud(Canvas canvas)
        {
            return canvas.isRootCanvas && canvas.enabled &&
                canvas.renderMode != RenderMode.WorldSpace &&
                canvas.GetComponent<TerminalPause>() == null &&
                canvas.GetComponent<TerminalTransition>() == null;
        }

        public static void FitCamera()
        {
            if (!Ready || active.handingOff) return;
            var self = active;
            // Settings may close after the scene has already removed its screen and camera.
            if (self.Screen == null || self.playerCamera == null || self.display == null) return;
            Vector3 normal = -Vector3.Cross(self.display.ScreenRight, self.display.ScreenUp).normalized;
            float tangent = Mathf.Tan(self.playerCamera.fieldOfView * .5f * Mathf.Deg2Rad);
            float distance = Mathf.Max(self.display.ScreenUp.magnitude * .5f / tangent, self.display.ScreenRight.magnitude * .5f / (tangent * self.playerCamera.aspect)) * (1.06f + GamePreferences.SafeArea * 2);
            self.playerCamera.transform.SetPositionAndRotation(self.Screen.bounds.center + normal * distance, Quaternion.LookRotation(-normal, self.display.ScreenUp.normalized));
        }

        private void LateUpdate()
        {
            if (!released) Time.timeScale = 0;
        }

        public static void BeginSurvey()
        {
            if (!Ready || active.handingOff) return;
            active.handingOff = true;
            active.StartCoroutine(active.HandOverToGameplay());
        }

        // TerminalMenu calls this after fading out the menu audio. The ship is already loaded.
        private IEnumerator HandOverToGameplay()
        {
            Vector3 start = playerCamera.transform.localPosition;
            Quaternion rotation = playerCamera.transform.localRotation;
            float fov = playerCamera.fieldOfView;
            bool reduced = GamePreferences.ReducedMotion;
            // Reduced effects changes the flicker, not the time needed to enter the ship.
            float duration = HandoffDuration;
            PlanRecovery();
            for (float t = 0; t < duration; t += Time.unscaledDeltaTime)
            {
                float phase = t / duration;
                float progress = Mathf.SmoothStep(0, 1, phase);
                playerCamera.transform.localPosition = Vector3.Lerp(start, cameraLocalPosition, progress);
                playerCamera.transform.localRotation = Quaternion.Slerp(rotation, cameraLocalRotation, progress);
                playerCamera.fieldOfView = Mathf.Lerp(fov, cameraFov, progress);
                HandoffLook look;
                if (reduced)
                    look = new HandoffLook(progress, 0, 1, progress);
                else
                    look = GetHandoffLook(phase);

                // TerminalDisplay changes the screen. TerminalRoomLighting changes the room.
                display.SetGameplayBlend(look.telescopeVisibility);
                display.SetHandoffSignal(look.glitchStrength, look.screenBrightness);
                roomLighting.SetLevel(look.roomBrightness);
                yield return null;
            }
            ReleaseControl();
            yield return ShowHint();
            Destroy(gameObject);
        }

        // Phase goes from 0 to 1 over the transition. Each stage is one failed recovery attempt.
        private HandoffLook GetHandoffLook(float phase)
        {
            if (phase < RecoveryRamp)
            {
                float ramp = Mathf.SmoothStep(0, 1, phase / RecoveryRamp);
                return new HandoffLook(0, ramp * .7f, Mathf.Lerp(1, .65f, ramp), ramp * .015f);
            }
            foreach (var beat in recovery)
                if (phase < beat.end) return beat.look;
            float settle = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.83f, 1, phase));
            return new HandoffLook(1, (1 - settle) * .25f, 1, Mathf.Lerp(.18f, 1, settle));
        }

        private void PlanRecovery()
        {
            recovery.Clear();
            float end = RecoveryRamp;
            int attempt = 0;
            // Pick each attempt once, not every frame. Screen and lamps share the same cue.
            while (end < .83f)
            {
                // Each attempt gets a different length, but always loses power again.
                // This keeps the whole transition readable even with an unlucky random seed.
                float visible = attempt == 2 ? .97f : Random.Range(.68f, 1);
                end = Mathf.Min(.83f, end + Random.Range(.055f, .115f));
                recovery.Add((end, new HandoffLook(visible, Random.Range(.6f, 1),
                    Random.Range(.5f, .95f), Random.Range(.11f, .23f))));
                end = Mathf.Min(.83f, end + Random.Range(.04f, .085f));
                recovery.Add((end, new HandoffLook(Random.Range(0, .14f), Random.Range(.5f, 1),
                    Random.Range(.015f, .10f), Random.Range(0, .02f))));
                attempt++;
            }
        }

        // Restore the things we temporarily changed. PlayerMovement and InteractOrigin
        // stop blocking input once this intro finishes.
        private void ReleaseControl()
        {
            if (released) return;
            released = true;
            roomLighting?.Dispose();
            if (display != null) display.RestoreInstrument();
            if (playerCamera != null)
            {
                playerCamera.transform.localPosition = cameraLocalPosition;
                playerCamera.transform.localRotation = cameraLocalRotation;
                playerCamera.fieldOfView = cameraFov;
                playerCamera.nearClipPlane = cameraNear;
            }
            if (body != null) body.isKinematic = wasKinematic;
            if (playerHud != null) foreach (Canvas hud in playerHud) if (hud != null) hud.enabled = true;
            Time.timeScale = previousTimeScale;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            if (events != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
                events.SetActive(false);
                Destroy(events);
            }
            if (gameplayEvents != null) gameplayEvents.SetActive(true);
            var pauseMenu = FindFirstObjectByType<TerminalPause>();
            if (pauseMenu != null) pauseMenu.GetComponent<TerminalInteractionAudio>().TakeFocus();
            if (display != null) Destroy(display.SourceCamera.gameObject);
            if (menu != null) Destroy(menu.gameObject);
            releaseFrame = Time.frameCount + 1;
            if (active == this) active = null;
            SceneManager.MoveGameObjectToScene(gameObject, SceneManager.GetActiveScene());
        }

        private void CreatePauseHint(TMP_FontAsset font)
        {
            hint = new GameObject("First survey ESC notice", typeof(Canvas), typeof(CanvasScaler), typeof(CanvasGroup)).GetComponent<Canvas>();
            hint.renderMode = RenderMode.ScreenSpaceOverlay;
            hint.sortingOrder = 90;
            var scaler = hint.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600, 900);
            scaler.matchWidthOrHeight = .5f;
            var panel = new GameObject("BIOS notice", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(hint.transform, false);
            var rect = (RectTransform)panel.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(1, 1);
            rect.anchoredPosition = new Vector2(-28, -28);
            rect.sizeDelta = new Vector2(550, 108);
            panel.GetComponent<Image>().color = new Color32(13, 19, 83, 248);
            panel.GetComponent<Image>().raycastTarget = false;
            var outline = panel.AddComponent<Outline>();
            outline.effectColor = new Color32(156, 171, 224, 255);
            outline.effectDistance = Vector2.one;
            var label = new GameObject("Message", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
            label.transform.SetParent(panel.transform, false);
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = new Vector2(20, 14);
            label.rectTransform.offsetMax = new Vector2(-20, -12);
            label.font = font;
            label.fontSize = 25;
            label.color = new Color32(221, 229, 255, 255);
            label.text = "<size=19>OPERATOR NOTICE / INPUT READY</size>\nYou can press ESC to access your pause menu at any time";
            label.raycastTarget = false;
        }

        private IEnumerator ShowHint()
        {
            var pauseMenu = FindFirstObjectByType<TerminalPause>();
            if (pauseMenu == null)
                yield break;

            // Use the same font as the pause menu so the notice matches it.
            TMP_FontAsset font = pauseMenu.GetComponentInChildren<TMP_Text>(true).font;
            CreatePauseHint(font);
            HintVisible = true;
            var group = hint.GetComponent<CanvasGroup>();
            // Pause hides the notice and stops its timer, so the instruction is readable after resuming.
            for (float t = 0; t < 6;)
            {
                bool paused = pauseMenu != null && pauseMenu.IsPaused;
                group.alpha = paused ? 0 : Mathf.Clamp01(Mathf.Min(t * 5, (6 - t) * 3));
                if (!paused) t += Time.unscaledDeltaTime;
                yield return null;
            }
            HintVisible = false;
            Destroy(hint.gameObject);
        }

        private void OnDestroy()
        {
            roomLighting?.Dispose();
            if (!released)
            {
                if (display != null) display.RestoreInstrument();
                if (gameplayEvents != null) gameplayEvents.SetActive(true);
                Time.timeScale = previousTimeScale;
                if (active == this) active = null;
            }
            if (hint != null) Destroy(hint.gameObject);
            HintVisible = false;
        }
    }
}
