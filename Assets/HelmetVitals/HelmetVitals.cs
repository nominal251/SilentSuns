using System.Globalization;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(Canvas))]
public sealed class HelmetVitals : MonoBehaviour
{
    [SerializeField] private PlayerHealth source;
    [SerializeField] private VitalsMeter meter;
    [SerializeField] private TMP_Text percent;

    [Header("Direct camera mount")]
    [Tooltip("Bottom-left anchor in the camera viewport. A visor socket can position the prefab directly.")]
    [SerializeField] private Vector2 viewportAnchor = new Vector2(.14f, .25f);
    [SerializeField, Range(.04f, .12f)] private float viewportHeight = .089f;
    private Camera mount;
    private Canvas display;
    private float numberSize;
    private float shownHealth = float.NaN;

    private Vector2 meterOrigin;

    private void Awake()
    {
        numberSize = percent.fontSize;
        meterOrigin = meter.rectTransform.anchoredPosition;
    }

    private void OnEnable()
    {
        display = GetComponent<Canvas>();
        mount = transform.parent ? transform.parent.GetComponent<Camera>() : null;
        // A camera-mounted suit display is drawn after the scene's damage vignette.
        display.renderMode = mount ? RenderMode.ScreenSpaceOverlay : RenderMode.WorldSpace;
        if (mount) display.targetDisplay = mount.targetDisplay;
        if (!source) source = GetComponentInParent<PlayerHealth>();
        if (source)
        {
            source.OnHealthChanged += Refresh;
            Refresh();
        }
        display.enabled = source != null;
        PositionReadout();
    }

    private void OnDisable()
    {
        if (display) display.enabled = false;
        if (source) source.OnHealthChanged -= Refresh;
    }

    private void LateUpdate()
    {
        if (!source) return;
        if (percent.color != meter.SignalColor) percent.color = meter.SignalColor;
        PositionReadout();
    }

    private void PositionReadout()
    {
        RectTransform panel = meter.rectTransform;
        Vector2 position = meterOrigin + meter.ReadoutOffset;
        if (mount)
        {
            display.enabled = source && mount.isActiveAndEnabled;
            Rect viewport = mount.pixelRect;
            float scale = Mathf.Min(viewport.height * viewportHeight / panel.rect.height,
                viewport.width * .48f / panel.rect.width);
            panel.localScale = Vector3.one * scale;
            position = viewport.position + Vector2.Scale(viewport.size, viewportAnchor) + position * scale;
        }
        panel.anchoredPosition = position;
    }

    private void Refresh()
    {
        var state = source.Condition;
        float value = Mathf.Clamp(source.health, 0, 100);
        meter.SetReading(value, state);
        if (value == shownHealth) return;
        shownHealth = value;
        string digits = value.ToString("0.#", CultureInfo.InvariantCulture);
        if (value > 0 && digits == "0") digits = "<0.1";
        if (value < 100 && digits == "100") digits = ">99.9";
        percent.fontSize = digits.Length > 3 ? numberSize * 4f / (digits.Length + 1) : numberSize;
        percent.text = "<noparse>" + digits + "</noparse>%";
    }
}
