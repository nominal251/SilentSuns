using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SilentSuns.Menu
{
    public class TerminalFailure : MonoBehaviour
    {
        public static bool Active { get; private set; }
        public bool ChoicesReady { get; private set; }
        private Material wake, signal;
        private Texture2D lastView;
        private CanvasGroup choices;
        private TerminalButton restart;
        private bool leaving;
        private float lightStarted;
        private static float carriedLightStart = -1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            Active = false;
            carriedLightStart = -1;
            SceneManager.sceneLoaded -= SceneLoaded;
            SceneManager.sceneLoaded += SceneLoaded;
        }
        private static void SceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != "GameOverScene")
            {
                Active = false;
                carriedLightStart = -1;
                return;
            }
            Active = true;
            foreach (var root in scene.GetRootGameObjects()) root.SetActive(false);
            var camera = new GameObject("Failure camera", typeof(Camera), typeof(AudioListener)).GetComponent<Camera>();
            camera.tag = "MainCamera";
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.cullingMask = ~0;
            var rootUI = new GameObject("Lost connection", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            rootUI.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            rootUI.GetComponent<Canvas>().sortingOrder = 150;
            var scaler = rootUI.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600, 900);
            scaler.matchWidthOrHeight = .5f;
            var failure = rootUI.AddComponent<TerminalFailure>();
            foreach (var old in FindObjectsByType<EventSystem>(FindObjectsSortMode.None)) old.gameObject.SetActive(false);
            var events = new GameObject("Failure input", typeof(EventSystem), typeof(InputSystemUIInputModule));
            events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = false;
            Time.timeScale = 1;
            AudioListener.pause = false;

            var face = TerminalUI.Panel("Signal", rootUI.transform, 0, 0, 1, 1, Color.white);
            failure.wake = new Material(Resources.Load<Material>("TerminalWake"));
            face.material = failure.wake;
            // Keep the same light and pulse when the fully dark scene is replaced.
            bool lightArrived = carriedLightStart >= 0;
            failure.wake.SetFloat("_Reveal", lightArrived ? 1 : 0);
            failure.wake.SetFloat("_Approach", lightArrived ? 1.15f : .18f);
            failure.lightStarted = lightArrived ? carriedLightStart : Time.unscaledTime;
            carriedLightStart = -1;

            // Keep the controls hidden and unclickable until the light has settled.
            var controls = TerminalUI.Rect("Choices", rootUI.transform, 0, 0, 1, 1);
            failure.choices = controls.gameObject.AddComponent<CanvasGroup>();
            failure.choices.alpha = 0;
            failure.choices.interactable = false;
            failure.choices.blocksRaycasts = false;
            TerminalUI.Text("Invitation", controls, "still there?", .25f, .56f, .5f, .07f, 32).alignment = TMPro.TextAlignmentOptions.Center;
            failure.restart = TerminalUI.Button(controls, "TRY AGAIN / RESTART SURVEY", .32f, .70f, .36f, .065f, () => failure.Leave(TerminalGameSession.RestartScene));
            TerminalUI.Button(controls, "COME BACK / MAIN MENU", .32f, .79f, .36f, .065f, () => failure.Leave("MainMenu"));
            foreach (var button in controls.GetComponentsInChildren<TerminalButton>())
            {
                var colors = button.colors;
                colors.disabledColor = colors.normalColor;
                button.colors = colors;
            }
            failure.StartCoroutine(failure.RevealLight(lightArrived));
        }
        public static void Begin(string destination)
        {
            if (Active) return;
            TerminalGameSession.EndRun();
            Active = true;
            Cursor.visible = false;
            // Stop the player and world while the signal fades; UI animation uses unscaled time.
            Time.timeScale = 0;
            var root = new GameObject("Failure transition", typeof(Canvas));
            root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            root.GetComponent<Canvas>().sortingOrder = 200;
            var failure = root.AddComponent<TerminalFailure>();
            failure.StartCoroutine(failure.LoseSignal(destination));
        }
        private IEnumerator LoseSignal(string destination)
        {
            // Keep the actual last view on screen, so the ship's glitch effect has a picture to tear.
            if (Application.isBatchMode) yield return null;
            else yield return new WaitForEndOfFrame();
            lastView = CaptureLastView();
            TerminalUI.Panel("Black backing", transform, 0, 0, 1, 1, Color.black).raycastTarget = false;
            var image = TerminalUI.Rect("Lost signal", transform, 0, 0, 1, 1).gameObject.AddComponent<RawImage>();
            image.texture = lastView;
            image.raycastTarget = false;
            // This is the same material used by TerminalDisplay during the ship handoff.
            // Only this copy changes; startup and pause keep their own materials.
            signal = new Material(TerminalUIAssets.Current.crtMaterial);
            image.material = signal;
            signal.SetFloat("_Power", 1);
            signal.SetFloat("_Warmup", 0);
            signal.SetFloat("_ShipOverlay", 1);
            signal.SetFloat("_GameplayBlend", 0);
            signal.SetFloat("_SignalLevel", 1);
            signal.SetFloat("_Persistence", 0);
            signal.SetFloat("_Noise", 0);
            signal.SetFloat("_Flicker", 0);
            bool reduced = GamePreferences.ReducedMotion;
            float intensity = GamePreferences.CRTIntensity;
            signal.SetFloat("_InterferenceStrength", reduced ? 0 : intensity);
            var light = TerminalUI.Panel("Approaching light", transform, 0, 0, 1, 1, Color.white);
            light.raycastTarget = false;
            wake = new Material(Resources.Load<Material>("TerminalWake"));
            light.material = wake;
            wake.SetFloat("_OverScene", 1);
            lightStarted = Time.unscaledTime;
            float nextSlip = 0, fault = 0, brightness = 1;
            // The light arrives during the glitch. Darkness then closes around it.
            for (float elapsed = 0; elapsed < 6.3f; elapsed += Time.unscaledDeltaTime)
            {
                float buildup = Mathf.SmoothStep(0, 1, Mathf.Clamp01(elapsed / 2.6f));
                float blackout = Mathf.SmoothStep(0, 1, Mathf.Clamp01((elapsed - 2.1f) / 4.2f));
                if (elapsed >= nextSlip)
                {
                    nextSlip = elapsed + Random.Range(.09f, .3f);
                    fault = Random.Range(.65f, 1);
                    brightness = Random.value < .35f ? Random.Range(.06f, .2f) : Random.Range(.45f, .85f);
                }
                float dimming = reduced ? .45f : Mathf.Lerp(1, brightness, intensity);
                signal.SetFloat("_DisplayTime", reduced ? 0 : elapsed);
                signal.SetFloat("_HandoffFault", reduced ? 0 : buildup * fault);
                signal.SetFloat("_GameplayBlend", reduced ? 0 : buildup * (1 - dimming) * .35f);
                signal.SetFloat("_SignalLevel", Mathf.Lerp(1, dimming, buildup));
                signal.SetFloat("_ScanStrength", TerminalUIAssets.Current.crtSettings.scanlines * buildup * intensity);
                signal.SetFloat("_Curvature", TerminalUIAssets.Current.crtSettings.curvature * buildup);
                float approach = Mathf.SmoothStep(0, 1, Mathf.Clamp01((elapsed - 1.1f) / 5.2f));
                wake.SetFloat("_Reveal", Mathf.SmoothStep(0, 1, Mathf.Clamp01((elapsed - 1.1f) / 3)));
                wake.SetFloat("_Approach", Mathf.Lerp(.18f, 1.15f, approach));
                Vector2 origin = reduced ? new Vector2(.5f, .56f) : Vector2.Lerp(new Vector2(.65f, .38f), new Vector2(.5f, .56f), approach);
                wake.SetVector("_Origin", new Vector4(origin.x, origin.y, 0, 0));
                wake.SetFloat("_Blackout", blackout);
                yield return null;
            }
            wake.SetFloat("_Blackout", 1);
            wake.SetFloat("_Reveal", 1);
            wake.SetFloat("_Approach", 1.15f);
            wake.SetVector("_Origin", new Vector4(.5f, .56f, 0, 0));
            signal.SetFloat("_Power", 0);
            signal.SetFloat("_GameplayBlend", 0);
            yield return null;
            carriedLightStart = lightStarted;
            SceneManager.LoadScene(string.IsNullOrEmpty(destination) ? "GameOverScene" : destination);
        }
        private Texture2D CaptureLastView()
        {
            if (!Application.isBatchMode) return ScreenCapture.CaptureScreenshotAsTexture();
            // Automated previews have no screen to capture. Render the frozen player camera once.
            var camera = Camera.main;
            if (camera == null) return Texture2D.blackTexture;
            var target = RenderTexture.GetTemporary(1920, 1080, 24);
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            float previousAspect = camera.aspect;
            try
            {
                camera.aspect = 16f / 9f;
                camera.targetTexture = target;
                RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
                RenderTexture.active = target;
                var picture = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
                picture.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
                picture.Apply();
                return picture;
            }
            finally
            {
                camera.targetTexture = previousTarget;
                camera.aspect = previousAspect;
                RenderTexture.active = previousActive;
                RenderTexture.ReleaseTemporary(target);
            }
        }
        private IEnumerator RevealLight(bool alreadyVisible)
        {
            // Normal deaths already revealed the light. Direct scene loads still get a slow arrival.
            float duration = alreadyVisible ? 1.6f : 4.8f;
            for (float elapsed = 0; elapsed < duration; elapsed += Time.unscaledDeltaTime)
            {
                float approach = alreadyVisible ? 1 : Mathf.SmoothStep(0, 1, Mathf.Clamp01((elapsed - .25f) / 3.6f));
                wake.SetFloat("_Reveal", alreadyVisible ? 1 : Mathf.SmoothStep(0, 1, Mathf.Clamp01((elapsed - .25f) / 2.8f)));
                wake.SetFloat("_Approach", Mathf.Lerp(.18f, 1.15f, approach));
                Vector2 origin = GamePreferences.ReducedMotion ? new Vector2(.5f, .56f) : Vector2.Lerp(new Vector2(.65f, .38f), new Vector2(.5f, .56f), approach);
                wake.SetVector("_Origin", new Vector4(origin.x, origin.y, 0, 0));
                choices.alpha = Mathf.SmoothStep(0, 1, Mathf.Clamp01((elapsed - (duration - 1.2f)) / 1.2f));
                yield return null;
            }
            wake.SetFloat("_Reveal", 1);
            wake.SetFloat("_Approach", 1.15f);
            wake.SetVector("_Origin", new Vector4(.5f, .56f, 0, 0));
            choices.alpha = 1;
            choices.interactable = choices.blocksRaycasts = true;
            ChoicesReady = true;
            Cursor.visible = true;
        }
        private void Update()
        {
            if (wake == null) return;
            float phase = Mathf.Repeat((Time.unscaledTime - lightStarted) / 4, 1);
            wake.SetFloat("_Aspect", (float)Screen.width / Screen.height);
            wake.SetFloat("_Phase", phase);
            wake.SetFloat("_Pulse", GamePreferences.ReducedMotion ? .5f : .5f + .5f * Mathf.Cos(phase * Mathf.PI * 2));
            wake.SetFloat("_ReducedMotion", GamePreferences.ReducedMotion ? 1 : 0);
            // Arrow keys or a controller can focus the choices after the reveal.
            if (ChoicesReady && EventSystem.current != null && EventSystem.current.currentSelectedGameObject == null &&
                (Keyboard.current?.anyKey.wasPressedThisFrame == true || Gamepad.current?.dpad.ReadValue().sqrMagnitude > .1f || Gamepad.current?.leftStick.ReadValue().sqrMagnitude > .3f))
                EventSystem.current.SetSelectedGameObject(restart.gameObject);
        }
        private void Leave(string scene)
        {
            if (!ChoicesReady || leaving) return;
            leaving = true;
            choices.interactable = choices.blocksRaycasts = false;
            // Active stays true until the next scene, so movement cannot sneak through the fade.
            TerminalTransition.LoadScene(scene, scene != "MainMenu" ? TerminalGameSession.StartNewRun : null);
        }
        private void OnDestroy()
        {
            if (wake != null) Destroy(wake);
            if (signal != null) Destroy(signal);
            if (lastView != null && lastView != Texture2D.blackTexture) Destroy(lastView);
        }
    }
}
