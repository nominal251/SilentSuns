using SilentSuns.Menu;
using UnityEngine;
using UnityEngine.UI;

public sealed class SuitDamageVignette : MonoBehaviour
{
    private static readonly int Strength = Shader.PropertyToID("_Strength");
    private static readonly int Impact = Shader.PropertyToID("_Impact");
    private static readonly int Aspect = Shader.PropertyToID("_Aspect");
    private static readonly int Critical = Shader.PropertyToID("_Critical");
    private static readonly int Pulse = Shader.PropertyToID("_Pulse");
    private Camera playerCamera;
    private Canvas display;
    private RawImage image;
    private Material ink;
    private float targetStrength;
    private float strength;
    private float impact;
    private float targetCritical;
    private float critical;
    private float pulsePhase;
    private bool initialized;

    public static SuitDamageVignette Create(Camera camera, Material template)
    {
        var overlay = new GameObject("Damage vignette", typeof(RectTransform), typeof(Canvas));
        overlay.transform.SetParent(camera.transform, false);
        var effect = overlay.AddComponent<SuitDamageVignette>();
        effect.playerCamera = camera;
        effect.display = overlay.GetComponent<Canvas>();
        effect.display.renderMode = RenderMode.ScreenSpaceOverlay;
        effect.display.sortingOrder = -100;
        var edges = new GameObject("Screen edges", typeof(RectTransform), typeof(RawImage));
        edges.transform.SetParent(overlay.transform, false);
        effect.image = edges.GetComponent<RawImage>();
        effect.image.raycastTarget = false;
        effect.image.rectTransform.offsetMin = effect.image.rectTransform.offsetMax = Vector2.zero;
        effect.ink = new Material(template);
        effect.image.material = effect.ink;
        return effect;
    }

    public void SetReading(float intensity, float damage, float criticalLevel)
    {
        targetStrength = Mathf.Clamp01(intensity);
        targetCritical = targetStrength * criticalLevel;
        if (!initialized || GamePreferences.ReducedMotion)
        {
            strength = targetStrength;
            critical = targetCritical;
        }
        initialized = true;
        if (damage > 0 && !GamePreferences.ReducedMotion)
            impact = Mathf.Max(impact, Mathf.Sqrt(Mathf.Clamp01(damage / 75f)));
    }

    private void LateUpdate()
    {
        display.enabled = playerCamera.isActiveAndEnabled && !playerCamera.targetTexture;
        display.targetDisplay = playerCamera.targetDisplay;
        Rect viewport = playerCamera.rect;
        image.rectTransform.anchorMin = viewport.min;
        image.rectTransform.anchorMax = viewport.max;
        float dt = Time.deltaTime;
        float pulse = 0.4f;
        if (GamePreferences.ReducedMotion)
        {
            impact = 0;
            strength = targetStrength;
            critical = targetCritical;
            pulsePhase = 0;
        }
        else
        {
            impact = Mathf.MoveTowards(impact, 0, dt * 1.8f);
            strength = Mathf.MoveTowards(strength, targetStrength, dt * (strength < targetStrength ? 4f : 1.1f));
            critical = Mathf.MoveTowards(critical, targetCritical, dt * (critical < targetCritical ? 2f : .9f));
            pulsePhase = critical > 0 ? Mathf.Repeat(pulsePhase + dt / 4.2f, 1f) : 0f;
            pulse += .09f * Mathf.Sin(pulsePhase * 2f * Mathf.PI);
        }
        ink.SetFloat(Strength, strength);
        ink.SetFloat(Impact, impact * impact);
        ink.SetFloat(Critical, critical);
        ink.SetFloat(Pulse, pulse);
        ink.SetFloat(Aspect, playerCamera.pixelWidth / (float)Mathf.Max(1, playerCamera.pixelHeight));
        image.enabled = strength > 0 || impact > 0 || critical > 0;
    }

    private void OnDestroy()
    {
        if (ink) Destroy(ink);
    }
}
