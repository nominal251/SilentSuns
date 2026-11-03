using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace SilentSuns.Menu
{
    // Draws the UI into a texture, adds CRT effects, and puts it on the telescope screen.
    public class TerminalDisplay : MonoBehaviour
    {
        [Header("Screen")]
        [SerializeField] private Camera sourceCamera;
        [SerializeField] private Camera displayCamera;
        [SerializeField] private Material screenMaterial;
        [SerializeField] private TerminalCRTSettings settings;
        [SerializeField] private Shader textLightShader;
        [SerializeField] private Shader shipSpillShader;
        private Material textLightMaterial;
        private RenderTexture textLightTexture;
        private RenderTexture screenTexture;
        private RenderTexture previousFrame;
        private Material material;
        private bool reducedMotion;
        private float powerOnTime = -10f;
        private float nextDisturbance;
        private float disturbanceEnd;
        private float disturbanceDuration;
        public float Warmup { get; private set; }
        private Vector4 disturbanceRegion;
        private bool hasHistory;
        private Renderer shipScreen;
        private Material[] instrumentMaterials;
        private TerminalShipScreenLight spill;
        private Vector3 uvOrigin, uvRight, uvUp;
        private float contentAspect = 16f / 9f;
        private float gameplayBlend;
        private bool dormant;
        private float signalLevel = 1;
        private float wakePulse;
        private float standbyWarmup;
        public bool IsShipScreen => shipScreen != null;
        public Vector3 ScreenRight => shipScreen.transform.TransformVector(uvRight);
        public Vector3 ScreenUp => shipScreen.transform.TransformVector(uvUp);
        public Renderer ShipScreen => shipScreen;
        public Material EffectMaterial => material;
        private readonly System.Random random = new System.Random();
        private float Curvature => TerminalSettings.EffectsSuspended ? 0 : settings.curvature;

        public Camera SourceCamera => sourceCamera;
        public float Illumination { get; private set; }

        // Keep the ship's original mesh and render-texture instrument. Only its temporary material changes.
        public void BindToShip(Renderer target, Camera camera)
        {
            shipScreen = target;
            instrumentMaterials = target.sharedMaterials;
            displayCamera = camera;
            ReadScreenCoordinates(target.GetComponent<MeshFilter>().sharedMesh);
            contentAspect = ScreenRight.magnitude / ScreenUp.magnitude;
            sourceCamera.orthographicSize = 8f / contentAspect;
            sourceCamera.aspect = contentAspect;

            // Keep the real telescope material underneath. The intro fades away to reveal it.
            var layers = new Material[instrumentMaterials.Length + 1];
            System.Array.Copy(instrumentMaterials, layers, instrumentMaterials.Length);
            layers[instrumentMaterials.Length] = material;
            target.sharedMaterials = layers;
            material.SetFloat("_ShipOverlay", 1);
            material.SetFloat("_ContentFit", 1);
            material.SetFloat("_GameplayBlend", 0);
            ResizeTexture();
        }

        // UVs say where a texture sits on a mesh. Read three corners to find the screen's axes.
        // The same coordinates are used for drawing and mouse clicks.
        private void ReadScreenCoordinates(Mesh mesh)
        {
            int[] indices = mesh.triangles;
            Vector3 a = mesh.vertices[indices[0]], b = mesh.vertices[indices[1]], c = mesh.vertices[indices[2]];
            Vector2 ta = mesh.uv[indices[0]], tb = mesh.uv[indices[1]], tc = mesh.uv[indices[2]];
            Vector2 ab = tb - ta, ac = tc - ta;
            float determinant = ab.x * ac.y - ab.y * ac.x;
            uvRight = ((b - a) * ac.y - (c - a) * ab.y) / determinant;
            uvUp = ((c - a) * ab.x - (b - a) * ac.x) / determinant;
            uvOrigin = a - uvRight * ta.x - uvUp * ta.y;
        }

        public void SetGameplayBlend(float value)
        {
            gameplayBlend = Mathf.Clamp01(value);
            material.SetFloat("_GameplayBlend", gameplayBlend);
        }

        public void SetDormant(bool value, float pulse = 0)
        {
            dormant = value;
            wakePulse = value ? pulse : 0;
            material.SetFloat("_Dormant", value ? 1 : 0);
            material.SetFloat("_WakePulse", pulse);
        }

        public void SetHandoffSignal(float fault, float brightness)
        {
            signalLevel = Mathf.Clamp01(brightness);
            material.SetFloat("_HandoffFault", reducedMotion ? 0 : fault);
            material.SetFloat("_SignalLevel", signalLevel);
        }

        public void SetStandbyWarmup(float amount) { standbyWarmup = Mathf.Clamp01(amount); }

        public void RestoreInstrument()
        {
            spill?.Dispose(); spill = null;
            if (shipScreen != null) shipScreen.sharedMaterials = instrumentMaterials;
            Illumination = 0;
            sourceCamera.enabled = false;
            enabled = false;
        }

        public void SetPower(float amount)
        {
            Illumination = Mathf.Clamp01(amount);
            sourceCamera.enabled = Illumination > 0f;
            if (material != null) material.SetFloat("_Power", Illumination);
            if (Illumination == 0f) hasHistory = false;
        }
        public float DiagnosticGlitch { get; set; }
        public bool ReducedMotion
        {
            get { return reducedMotion; }
            set { reducedMotion = value; }
        }

        // Prepare the screen material. TerminalShipIntro connects it to the telescope after loading.
        private void Awake()
        {
            material = new Material(screenMaterial);
            if (textLightShader != null) textLightMaterial = new Material(textLightShader);
            ResizeTexture();
            ScheduleDisturbance();
            SetPower(0f);
            RenderPipelineManager.endCameraRendering += RememberFrame;
        }

        // Use unscaled time so the display also works while gameplay is paused.
        private void Update()
        {
            if (displayCamera == null) return;
            ResizeTexture();
            ApplyCRTSettings();
            float disturbance = UpdateGlitches();
            UpdateWarmup();
            UpdateScreenLight(disturbance);
        }

        // Copy DefaultCRT.asset settings to the shader that draws the screen.
        private void ApplyCRTSettings()
        {
            reducedMotion = GamePreferences.ReducedMotion;
            material.SetFloat("_ClearDisplay", TerminalSettings.EffectsSuspended ? 1 : 0);
            material.SetFloat("_Curvature", settings.curvature);
            material.SetFloat("_ScanStrength", settings.scanlines * GamePreferences.CRTIntensity);
            material.SetFloat("_ScanLines", settings.scanLineCount);
            material.SetFloat("_DisplayTime", reducedMotion ? 0f : Time.unscaledTime);
            material.SetFloat("_PhosphorStrength", settings.shadowMask);
            material.SetFloat("_Glow", settings.glow * GamePreferences.CRTIntensity);
            material.SetFloat("_Vignette", settings.vignette);
            material.SetFloat("_Convergence", settings.convergence);
            material.SetFloat("_Noise", settings.noise * GamePreferences.CRTIntensity);
            material.SetFloat("_Flicker", reducedMotion ? 0f : settings.flicker * GamePreferences.CRTIntensity);
            material.SetFloat("_InterferenceStrength", reducedMotion ? 0f : GamePreferences.CRTIntensity);
            material.SetFloat("_Persistence", reducedMotion || !hasHistory ? 0f : settings.persistence);
        }

        private float UpdateGlitches()
        {
            if (TerminalSettings.EffectsSuspended)
            {
                disturbanceEnd = 0;
                ScheduleDisturbance();
                material.SetFloat("_Disturbance", 0);
                material.SetFloat("_DiagnosticGlitch", 0);
                return 0;
            }
            if (Illumination > .99f && standbyWarmup <= 0 && !reducedMotion && settings.glitchFrequency > 0f && Time.unscaledTime >= nextDisturbance)
            {
                disturbanceDuration = .16f + (float)random.NextDouble() * .12f;
                disturbanceEnd = Time.unscaledTime + disturbanceDuration;
                disturbanceRegion = new Vector4(.5f, .025f + (float)random.NextDouble() * .95f, .6f, .018f + (float)random.NextDouble() * .035f);
                ScheduleDisturbance();
            }
            float disturbance = settings.glitchFrequency > 0f ? Mathf.Clamp01((disturbanceEnd - Time.unscaledTime) / Mathf.Max(.01f, disturbanceDuration)) : 0f;
            material.SetVector("_DisturbanceRegion", disturbanceRegion);
            material.SetFloat("_Disturbance", reducedMotion ? 0f : disturbance);
            material.SetFloat("_DiagnosticGlitch", reducedMotion ? 0f : Mathf.Min(DiagnosticGlitch, .8f));
            return disturbance;
        }

        private void UpdateWarmup()
        {
            if (reducedMotion)
                Warmup = 0;
            else if (dormant)
                Warmup = standbyWarmup;
            else
                Warmup = Mathf.Clamp01(1f - (Time.unscaledTime - powerOnTime) / TerminalMenu.WarmupDuration);
            material.SetFloat("_Warmup", Warmup);
        }

        private void UpdateScreenLight(float disturbance)
        {
            float variation = reducedMotion || TerminalSettings.EffectsSuspended ? 1f :
                1f + Mathf.Sin(Time.unscaledTime * 1.37f) * Mathf.Sin(Time.unscaledTime * .31f) * settings.flicker;
            float light = Illumination * variation * (1f - disturbance * .09f) * (1 - gameplayBlend) * signalLevel;
            // TerminalShipScreenLight draws the blue glow and text light on the existing casing.
            spill?.SetPower(light);
            spill?.SetInvitation(dormant ? .2f + wakePulse * .8f : 0);
        }

        private void ScheduleDisturbance()
        {
            nextDisturbance = Time.unscaledTime + (0.65f + (float)random.NextDouble() * 0.7f) / Mathf.Max(0.001f, settings.glitchFrequency);
        }

        // Unity calls this after a camera renders. Save one old frame for the faint text trails.
        private void RememberFrame(ScriptableRenderContext context, Camera camera)
        {
            if (camera == sourceCamera && textLightMaterial != null && textLightTexture != null && Illumination > 0f)
            {
                Graphics.Blit(screenTexture, textLightTexture, textLightMaterial);
                textLightTexture.GenerateMips();
            }
            if (camera != displayCamera || previousFrame == null || Illumination <= 0f)
                return;
            Graphics.Blit(screenTexture, previousFrame);
            hasHistory = true;
        }

        // The raster expands and focus settles during the same power ramp as the enclosure light.
        public void PowerOn()
        {
            powerOnTime = Time.unscaledTime;
            ScheduleDisturbance();
        }

        private void ResizeTexture()
        {
            // TerminalShipIntro supplies the screen and camera after the ship loads.
            if (displayCamera == null || shipScreen == null) return;
            float pixels = Vector2.Distance(displayCamera.WorldToScreenPoint(shipScreen.bounds.center - ScreenRight * .5f), displayCamera.WorldToScreenPoint(shipScreen.bounds.center + ScreenRight * .5f));
            int width = Mathf.Clamp(Mathf.CeilToInt(pixels / 64f) * 64, 640, 3840);
            int height = Mathf.RoundToInt(width / contentAspect);
            if (screenTexture != null && screenTexture.width == width && screenTexture.height == height)
                return;
            spill?.Dispose(); spill = null;
            ReleaseTexture();
            screenTexture = new RenderTexture(width, height, 24);
            screenTexture.name = "Terminal Screen";
            screenTexture.filterMode = FilterMode.Bilinear;
            screenTexture.useMipMap = true;
            screenTexture.autoGenerateMips = true;
            screenTexture.Create();
            textLightTexture = new RenderTexture(width, screenTexture.height, 0, RenderTextureFormat.ARGB32);
            textLightTexture.name = "Terminal Text Illumination";
            textLightTexture.filterMode = FilterMode.Trilinear;
            textLightTexture.useMipMap = true;
            textLightTexture.autoGenerateMips = false;
            textLightTexture.Create();
            previousFrame = new RenderTexture(width, screenTexture.height, 0);
            previousFrame.name = "Terminal Previous Frame";
            previousFrame.filterMode = FilterMode.Bilinear;
            previousFrame.Create();
            material.SetTexture("_PreviousTex", previousFrame);
            hasHistory = false;
            sourceCamera.targetTexture = screenTexture;
            sourceCamera.aspect = contentAspect;
            material.SetTexture("_MainTex", screenTexture);
            if (IsShipScreen)
                spill = new TerminalShipScreenLight(shipScreen, shipSpillShader, screenTexture, textLightTexture, ScreenRight, ScreenUp);
        }

        // TerminalRaycaster calls this to turn a mouse position into a button position on the screen.
        public bool TryGetContentPoint(Vector2 screenPoint, out Vector2 contentPoint)
        {
            contentPoint = Vector2.zero;
            if (Illumination < 0.99f || shipScreen == null || displayCamera == null || screenTexture == null) return false;
            Ray ray = displayCamera.ScreenPointToRay(screenPoint);
            var plane = new Plane(Vector3.Cross(ScreenRight, ScreenUp), shipScreen.bounds.center);
            if (!plane.Raycast(ray, out float distance)) return false;
            Vector3 local = shipScreen.transform.InverseTransformPoint(ray.GetPoint(distance)) - uvOrigin;
            Vector2 uv = new Vector2(Vector3.Dot(local, uvRight) / uvRight.sqrMagnitude, Vector3.Dot(local, uvUp) / uvUp.sqrMagnitude);
            Vector2 centered = uv * 2f - Vector2.one;
            centered *= 1f + Curvature * Mathf.Pow(centered.sqrMagnitude * 0.5f, 2f);
            uv = (centered + Vector2.one) * 0.5f;
            contentPoint = new Vector2(uv.x * screenTexture.width, uv.y * screenTexture.height);
            return uv.x >= 0f && uv.x <= 1f && uv.y >= 0f && uv.y <= 1f;
        }

        // Used by visual checks and any screen-space labels anchored to terminal controls.
        public Vector2 ContentToScreenPoint(Vector3 worldPoint)
        {
            if (shipScreen == null || displayCamera == null) return Vector2.zero;
            Vector3 viewport = sourceCamera.WorldToViewportPoint(worldPoint);
            Vector2 content = new Vector2(viewport.x, viewport.y) * 2f - Vector2.one;
            Vector2 curved = content;
            for (int i = 0; i < 8; i++)
                curved = content / (1f + Curvature * Mathf.Pow(curved.sqrMagnitude * 0.5f, 2f));
            Vector2 uv = (curved + Vector2.one) * 0.5f;
            return displayCamera.WorldToScreenPoint(shipScreen.transform.TransformPoint(uvOrigin + uvRight * uv.x + uvUp * uv.y));
        }

        private void OnDestroy()
        {
            spill?.Dispose();
            if (shipScreen != null) shipScreen.sharedMaterials = instrumentMaterials;
            RenderPipelineManager.endCameraRendering -= RememberFrame;
            ReleaseTexture();
            if (material != null)
                Destroy(material);
            if (textLightMaterial != null) Destroy(textLightMaterial);
        }

        private void ReleaseTexture()
        {
            if (screenTexture == null)
                return;
            if (sourceCamera != null)
                sourceCamera.targetTexture = null;
            screenTexture.Release();
            Destroy(screenTexture);
            if (textLightTexture != null)
            {
                textLightTexture.Release();
                Destroy(textLightTexture);
            }
            if (previousFrame != null)
            {
                previousFrame.Release();
                Destroy(previousFrame);
            }
        }
    }
}
