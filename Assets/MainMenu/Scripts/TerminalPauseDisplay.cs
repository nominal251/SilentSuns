using UnityEngine;
using UnityEngine.UI;

namespace SilentSuns.Menu
{
    // Draw the pause menu into a transparent texture so CRT effects stay inside the panel.
    [RequireComponent(typeof(Canvas))]
    public class TerminalPauseDisplay : MonoBehaviour
    {
        [SerializeField] private Material screenMaterial;
        [SerializeField] private TerminalCRTSettings settings;
        private Camera source;
        private Canvas presentation;
        private RawImage face;
        private Image dimmer;
        private RenderTexture texture;
        private Material material;
        private CanvasGroup visibility;
        private Canvas canvas;
        private float nextFault;
        private float faultUntil;
        public Camera SourceCamera => source;
        public RenderTexture PanelTexture => texture;
        public Canvas Presentation => presentation;

        private void Start()
        {
            // TerminalPause owns the buttons. This script only changes how they are drawn.
            canvas = GetComponent<Canvas>();
            visibility = GetComponent<CanvasGroup>();
            GetComponent<CanvasScaler>().enabled = false;
            canvas.renderMode = RenderMode.WorldSpace;
            transform.position = new Vector3(0, 0, -10000);
            transform.localScale = Vector3.one * .01f;
            foreach (Transform item in GetComponentsInChildren<Transform>(true)) item.gameObject.layer = 28;
            foreach (Image item in GetComponentsInChildren<Image>(true))
                if (item.name == "Dim") item.gameObject.SetActive(false);

            source = new GameObject("Pause panel source", typeof(Camera)).GetComponent<Camera>();
            source.transform.SetParent(transform, false);
            source.transform.position = transform.position + Vector3.back * 10;
            source.transform.rotation = Quaternion.identity;
            source.orthographic = true;
            source.nearClipPlane = .1f;
            source.farClipPlane = 20;
            source.cullingMask = 1 << 28;
            source.clearFlags = CameraClearFlags.SolidColor;
            source.backgroundColor = Color.clear;
            source.depth = -20;
            source.allowHDR = false;
            canvas.worldCamera = source;
            if (Camera.main != null) Camera.main.cullingMask &= ~(1 << 28);

            presentation = new GameObject("Pause display", typeof(Canvas)).GetComponent<Canvas>();
            presentation.renderMode = RenderMode.ScreenSpaceOverlay;
            presentation.sortingOrder = 100;
            dimmer = Layer<Image>("Gameplay dimmer");
            face = Layer<RawImage>("CRT panel only");
            material = new Material(screenMaterial);
            material.SetFloat("_RespectSourceAlpha", 1);
            material.SetFloat("_Curvature", 0);
            material.SetFloat("_Persistence", 0);
            material.SetFloat("_Warmup", 0);
            material.SetFloat("_Power", 1);
            face.material = material;
            Resize();
        }

        private T Layer<T>(string name) where T : Graphic
        {
            var item = new GameObject(name, typeof(RectTransform), typeof(T));
            item.transform.SetParent(presentation.transform, false);
            var rect = (RectTransform)item.transform;
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var graphic = item.GetComponent<T>();
            graphic.raycastTarget = false;
            return graphic;
        }

        private void LateUpdate()
        {
            if (source == null) return;
            Resize();
            bool shown = visibility.alpha > 0;
            source.enabled = shown;
            face.enabled = shown;
            dimmer.color = new Color(0, 0, 0, visibility.alpha * .58f);
            bool reduced = GamePreferences.ReducedMotion;
            if (shown && !reduced && Time.unscaledTime >= nextFault)
            {
                faultUntil = Time.unscaledTime + .18f;
                nextFault = Time.unscaledTime + Random.Range(3f, 6f);
                material.SetVector("_DisturbanceRegion", new Vector4(.5f, Random.Range(.25f, .75f), .6f, .025f));
            }
            material.SetFloat("_Disturbance", !reduced && Time.unscaledTime < faultUntil ? .65f : 0);
            material.SetFloat("_InterferenceStrength", reduced ? 0 : 1);
            material.SetFloat("_DisplayTime", reduced ? 0 : Time.unscaledTime);
            material.SetFloat("_ScanStrength", settings.scanlines);
            material.SetFloat("_ScanLines", settings.scanLineCount);
            material.SetFloat("_PhosphorStrength", settings.shadowMask);
            material.SetFloat("_Glow", settings.glow);
            material.SetFloat("_Noise", reduced ? 0 : settings.noise);
            material.SetFloat("_Flicker", reduced ? 0 : settings.flicker);
        }

        private void Resize()
        {
            int width = Mathf.Max(1, Screen.width), height = Mathf.Max(1, Screen.height);
            if (texture != null && texture.width == width && texture.height == height) return;
            Release();
            texture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            texture.name = "Pause panel RGBA";
            texture.Create();
            face.texture = texture;
            source.targetTexture = texture;
            source.aspect = (float)width / height;
            source.orthographicSize = Mathf.Max(4.5f, 8f / source.aspect);
            ((RectTransform)transform).sizeDelta = new Vector2(source.orthographicSize * 200 * source.aspect, source.orthographicSize * 200);
        }

        private void Release()
        {
            if (source != null) source.targetTexture = null;
            if (texture != null) { texture.Release(); Destroy(texture); }
        }

        private void OnDestroy()
        {
            Release();
            if (material != null) Destroy(material);
            if (presentation != null) Destroy(presentation.gameObject);
        }
    }
}
