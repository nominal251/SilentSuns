using UnityEngine;
using UnityEngine.UI;

namespace SilentSuns.Menu
{
    // Renders the BIOS UI onto the glass. Each computer owns its material and texture.
    public class MasterComputerDisplay : MonoBehaviour
    {
        [SerializeField] private Renderer screen;
        [Range(0, 1)] [SerializeField] private float effectStrength = .6f;
        [SerializeField] private Texture startupFace;
        [Range(0, 1)] [SerializeField] private float intrusionChance = .001f;
        public MasterComputerUI UI { get; private set; }
        public Camera SourceCamera { get; private set; }
        public RenderTexture Texture { get; private set; }
        public Material EffectMaterial { get; private set; }
        public Renderer Screen => screen;
        private GameObject content;
        private Material previousMaterial;
        private float nextFault, rippleStarted = -100, rippleDuration = 1, rippleAmount;
        private const int ContentLayer = 26;
        private static int nextScreen;

        public void Configure(Renderer surface) => screen = surface;
        public void ConfigureStartup(Texture image) => startupFace = image;

        private void Awake()
        {
            if (screen == null) { enabled = false; return; }
            content = new GameObject("Master computer content", typeof(RectTransform), typeof(Canvas));
            var rect = (RectTransform)content.transform;
            // Keep pixels separate from the model's scale. Space out screens so their cameras cannot overlap.
            rect.position = new Vector3(0, 0, -20000 - nextScreen++ * 30);
            rect.rotation = Quaternion.identity;
            rect.localScale = Vector3.one * .01f;
            rect.sizeDelta = new Vector2(1280, 720);
            var canvas = content.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            content.layer = ContentLayer;
            SourceCamera = new GameObject("Master screen camera", typeof(Camera)).GetComponent<Camera>();
            SourceCamera.transform.SetParent(content.transform, false);
            SourceCamera.transform.position = rect.position + Vector3.back * 10;
            SourceCamera.transform.rotation = Quaternion.identity;
            SourceCamera.orthographic = true;
            SourceCamera.orthographicSize = 3.6f;
            SourceCamera.aspect = 16f / 9;
            SourceCamera.nearClipPlane = .1f;
            SourceCamera.farClipPlane = 20;
            SourceCamera.cullingMask = 1 << ContentLayer;
            SourceCamera.clearFlags = CameraClearFlags.SolidColor;
            SourceCamera.backgroundColor = TerminalUI.Blue;
            SourceCamera.allowHDR = false;
            SourceCamera.depth = -30;
            Texture = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32) { name = "Master computer screen", antiAliasing = 2 };
            Texture.Create();
            SourceCamera.targetTexture = Texture;
            canvas.worldCamera = SourceCamera;
            UI = content.AddComponent<MasterComputerUI>();
            UI.ConfigureStartup(startupFace, intrusionChance);
            content.AddComponent<MasterComputerRaycaster>().Display = this;
            EffectMaterial = new Material(TerminalUIAssets.Current.crtMaterial);
            EffectMaterial.name = "Master computer CRT (instance)";
            EffectMaterial.mainTexture = Texture;
            EffectMaterial.SetFloat("_ShipOverlay", 1);
            EffectMaterial.SetFloat("_GameplayBlend", 0);
            EffectMaterial.SetFloat("_ContentFit", 1);
            EffectMaterial.SetFloat("_Persistence", 0);
            EffectMaterial.SetFloat("_Dormant", 0);
            EffectMaterial.SetFloat("_Power", 0);
            previousMaterial = screen.sharedMaterial;
            screen.sharedMaterial = EffectMaterial;
            nextFault = Time.unscaledTime + 7;
        }

        private void LateUpdate()
        {
            if (UI == null) return;
            var profile = TerminalUIAssets.Current.crtSettings;
            float strength = effectStrength * GamePreferences.CRTIntensity;
            bool still = GamePreferences.ReducedMotion || TerminalPause.BlocksGameplayInput || TerminalSettings.EffectsSuspended;
            float now = Time.unscaledTime;
            if (still || !UI.InUse || !UI.Ready || UI.Transitioning)
            {
                // Give the reader a quiet stretch after startup or a page change.
                rippleStarted = -100;
                nextFault = now + 9;
            }
            else if (now >= nextFault)
            {
                rippleStarted = now;
                rippleDuration = Random.Range(.7f, 1.1f);
                rippleAmount = Random.Range(.45f, .8f);
                nextFault = now + Random.Range(12, 24);
            }
            float phase = UI.Transitioning ? UI.TransitionProgress : Mathf.Clamp01((now - rippleStarted) / rippleDuration);
            float envelope = Mathf.Sin(phase * Mathf.PI);
            float ripple = still || !UI.InUse ? 0 : envelope * (UI.Transitioning ? .95f : rippleAmount);
            // A small sync hiccup rides the ripple; the much stronger scare stays separate.
            float slip = UI.Transitioning && phase > .30f && phase < .48f ? .35f : 0;
            EffectMaterial.SetVector("_DisturbanceRegion", new Vector4(.5f, Mathf.Lerp(-.1f, 1.1f, phase), .6f, .04f));
            EffectMaterial.SetFloat("_SyncRipple", ripple);
            EffectMaterial.SetFloat("_SyncPhase", phase);
            EffectMaterial.SetFloat("_SignalLevel", 1 - ripple * strength * (UI.Transitioning ? .18f : .035f));
            EffectMaterial.SetFloat("_ClearDisplay", TerminalSettings.EffectsSuspended ? 1 : 0);
            EffectMaterial.SetFloat("_Power", UI.Power);
            EffectMaterial.SetFloat("_Warmup", GamePreferences.ReducedMotion ? 0 : UI.Warmup);
            EffectMaterial.SetFloat("_DisplayTime", still ? 0 : Time.unscaledTime);
            EffectMaterial.SetFloat("_ScanStrength", profile.scanlines * strength);
            EffectMaterial.SetFloat("_ScanLines", profile.scanLineCount);
            EffectMaterial.SetFloat("_Glow", profile.glow * strength);
            EffectMaterial.SetFloat("_Curvature", profile.curvature * effectStrength);
            EffectMaterial.SetFloat("_Vignette", profile.vignette * effectStrength);
            EffectMaterial.SetFloat("_Convergence", profile.convergence * strength);
            EffectMaterial.SetFloat("_Noise", still ? 0 : profile.noise * strength);
            EffectMaterial.SetFloat("_Flicker", still ? 0 : profile.flicker * strength);
            EffectMaterial.SetFloat("_InterferenceStrength", still ? 0 : strength);
            EffectMaterial.SetFloat("_Disturbance", still ? 0 : slip);
            EffectMaterial.SetFloat("_HandoffFault", still ? 0 : UI.FaultStrength);
            EffectMaterial.SetFloat("_DiagnosticGlitch", 0);
        }

        public bool TryGetContentPoint(Vector2 pointer, out Vector2 point)
        {
            point = default;
            if (UI == null || !UI.InUse || !UI.Ready || TerminalPause.BlocksGameplayInput || Camera.main == null) return false;
            Ray ray = Camera.main.ScreenPointToRay(pointer);
            var plane = new Plane(screen.transform.forward, screen.transform.position);
            if (!plane.Raycast(ray, out float distance) || distance > 5) return false;
            Vector3 local = screen.transform.InverseTransformPoint(ray.GetPoint(distance));
            if (Mathf.Abs(local.x) > .5f || Mathf.Abs(local.y) > .5f) return false;
            point = new Vector2((local.x + .5f) * Texture.width, (local.y + .5f) * Texture.height);
            return true;
        }

        private void OnDestroy()
        {
            if (screen != null) screen.sharedMaterial = previousMaterial;
            if (SourceCamera != null) SourceCamera.targetTexture = null;
            if (Texture != null) { Texture.Release(); Destroy(Texture); }
            if (EffectMaterial != null) Destroy(EffectMaterial);
            if (content != null) Destroy(content);
        }
    }
}
