using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SilentSuns.Menu
{
    public class TerminalFailure : MonoBehaviour
    {
        public static bool Active
        {
            get; private set;
        }
        private Material wake;
        private Material crt;
        private RawImage image;
        private Texture2D capture;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            Active = false;
            SceneManager.sceneLoaded -= SceneLoaded;
            SceneManager.sceneLoaded += SceneLoaded;
        }
        private static void SceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != "GameOverScene")
            {
                Active = false;
                return;
            }
            Active = true;
            foreach (var root in scene.GetRootGameObjects())
                root.SetActive(false);
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
            foreach (var old in FindObjectsByType<EventSystem>(FindObjectsSortMode.None))
                old.gameObject.SetActive(false);
            var events = new GameObject("Failure input", typeof(EventSystem), typeof(InputSystemUIInputModule));
            events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Time.timeScale = 1;
            AudioListener.pause = false;
            var face = TerminalUI.Panel("Signal", rootUI.transform, 0, 0, 1, 1, Color.white);
            failure.wake = new Material(Resources.Load<Material>("TerminalWake"));
            face.material = failure.wake;
            failure.wake.SetFloat("_Reveal", 1);
            TerminalUI.Text("Invitation", face.transform, "still there?", .25f, .52f, .5f, .08f, 38).alignment = TMPro.TextAlignmentOptions.Center;
            TerminalUI.Button(face.transform, "TRY AGAIN / RESTART SURVEY", .28f, .69f, .44f, .09f, () => failure.Leave("SampleScene"));
            TerminalUI.Button(face.transform, "COME BACK / MAIN MENU", .28f, .81f, .44f, .09f, () => failure.Leave("MainMenu"));
        }
        public static void Begin(string destination)
        {
            if (Active)
                return;
            Active = true;
            var root = new GameObject("Failure transition", typeof(Canvas), typeof(CanvasScaler));
            root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            root.GetComponent<Canvas>().sortingOrder = 200;
            var failure = root.AddComponent<TerminalFailure>();
            failure.StartCoroutine(failure.Glitch(destination));
        }
        private IEnumerator Glitch(string destination)
        {
            if (Application.isBatchMode)
                yield return null;
            else
                yield return new WaitForEndOfFrame();
            capture = Application.isBatchMode ? new Texture2D(1, 1) : ScreenCapture.CaptureScreenshotAsTexture();
            image = TerminalUI.Rect("Lost signal", transform, 0, 0, 1, 1).gameObject.AddComponent<RawImage>();
            image.texture = capture;
            crt = new Material(TerminalUIAssets.Current.crtMaterial);
            image.material = crt;
            crt.SetFloat("_Power", 1);
            crt.SetFloat("_Warmup", 0);
            crt.SetFloat("_InterferenceStrength", GamePreferences.ReducedMotion ? 0 : 1);
            for (float t = 0; t < 1.8f; t += Time.unscaledDeltaTime)
            {
                float p = t / 1.8f;
                crt.SetFloat("_DisplayTime", Time.unscaledTime);
                crt.SetFloat("_HandoffFault", GamePreferences.ReducedMotion ? 0 : Mathf.Clamp01(p + Mathf.PerlinNoise(t * 11, 4) * .5f));
                image.color = new Color(1 - p, 1 - p, 1 - p, 1);
                yield return null;
            }
            SceneManager.LoadScene(string.IsNullOrEmpty(destination) ? "GameOverScene" : destination);
        }
        private void Update()
        {
            if (wake == null)
                return;
            float phase = Mathf.Repeat(Time.unscaledTime / 4, 1);
            wake.SetFloat("_Aspect", (float)Screen.width / Screen.height);
            wake.SetFloat("_Phase", phase);
            wake.SetFloat("_Pulse", GamePreferences.ReducedMotion ? .5f : .5f + .5f * Mathf.Cos(phase * Mathf.PI * 2));
            wake.SetFloat("_ReducedMotion", GamePreferences.ReducedMotion ? 1 : 0);
        }
        private void Leave(string scene)
        {
            Active = false;
            TerminalTransition.LoadScene(scene);
        }
        private void OnDestroy()
        {
            if (wake != null)
                Destroy(wake);
            if (crt != null)
                Destroy(crt);
            if (capture != null)
                Destroy(capture);
        }
    }
}
