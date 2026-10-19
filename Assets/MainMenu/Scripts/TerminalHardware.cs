using UnityEngine;

namespace SilentSuns.Menu
{
    public class TerminalHardware : MonoBehaviour
    {
        [SerializeField] private Renderer indicator;
        [SerializeField] private Transform powerSwitch;
        [SerializeField] private Renderer bezel;
        [SerializeField] private Renderer glassCoating;
        private Material indicatorMaterial;
        private Vector3 switchPosition;
        private MaterialPropertyBlock screenProperties;
        private float blinkStart;
        private TerminalMenu.PowerState state;
        public bool Powered => state != TerminalMenu.PowerState.WaitingForTouch;
        public Color IndicatorColor { get; private set; }
        public bool IndicatorLit { get; private set; }
        public float Illumination { get; private set; }

        private void Awake()
        {
            indicatorMaterial = indicator.material;
            indicatorMaterial.EnableKeyword("_EMISSION");
            switchPosition = powerSwitch.localPosition;
            if (screenProperties == null) screenProperties = new MaterialPropertyBlock();
            ApplyState(TerminalMenu.PowerState.WaitingForTouch);
            SetIllumination(0f);
        }

        // Power state belongs to the menu; the hardware only presents that state.
        public void ApplyState(TerminalMenu.PowerState value)
        {
            if (value == TerminalMenu.PowerState.PoweringOn) blinkStart = Time.unscaledTime;
            state = value;
            powerSwitch.localPosition = switchPosition + (Powered ? Vector3.forward * .025f : Vector3.zero);
            UpdateIndicator();
        }

        private void Update() { UpdateIndicator(); }

        private void UpdateIndicator()
        {
            bool starting = state == TerminalMenu.PowerState.PoweringOn || state == TerminalMenu.PowerState.Booting;
            IndicatorColor = state == TerminalMenu.PowerState.WaitingForTouch ? new Color(.7f,.025f,.008f) :
                starting || state == TerminalMenu.PowerState.PoweringDown ? new Color(.85f,.35f,.012f) : new Color(.07f,.65f,.025f);
            IndicatorLit = !starting || GamePreferences.ReducedMotion || (Time.unscaledTime - blinkStart) % .9f < .45f;
            indicatorMaterial.SetColor("_BaseColor", IndicatorColor * (IndicatorLit ? .4f : .04f));
            indicatorMaterial.SetColor("_EmissionColor", IndicatorLit ? IndicatorColor * 1.4f : Color.black);
        }

        public void SetIllumination(float amount)
        {
            Illumination = Mathf.Max(0f,amount);
            screenProperties.SetFloat("_Power", Illumination);
            bezel.SetPropertyBlock(screenProperties);
            glassCoating.enabled = Illumination > 0f;
        }

        public void SetScreenTexture(Texture texture)
        {
            if (screenProperties == null) screenProperties = new MaterialPropertyBlock();
            screenProperties.SetTexture("_TerminalTexture", texture);
            if (texture != null)
                screenProperties.SetVector("_TerminalTexture_TexelSize", new Vector4(1f / texture.width, 1f / texture.height, texture.width, texture.height));
            bezel.SetPropertyBlock(screenProperties);
        }

        public void SetTextLightTexture(Texture texture)
        {
            if (screenProperties == null) screenProperties = new MaterialPropertyBlock();
            screenProperties.SetTexture("_TextLightTexture", texture);
            if (bezel != null) bezel.SetPropertyBlock(screenProperties);
        }

        private void OnDestroy()
        {
            if (indicatorMaterial != null) Destroy(indicatorMaterial);
        }
    }
}
