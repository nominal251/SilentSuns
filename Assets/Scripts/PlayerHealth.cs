using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public enum HealthCondition { Good, Caution, Danger }

public class PlayerHealth : MonoBehaviour
{
    public float health = 100f;
    public event Action OnHealthChanged;

    [SerializeField] private Volume globalVolume;
    [SerializeField] private Material damageVignetteMaterial;
    private Vignette volumeVignette;
    private SuitDamageVignette suitVignette;

    [Header("Health state breakpoints")]
    [SerializeField, Range(0, 100)] private float cautionBreakpoint;
    [SerializeField, Range(0, 100)] private float dangerBreakpoint;

    [Header("Vignette intensity at breakpoints.")]
    [SerializeField, Range(0, 1)] private float goodIntensity;
    [SerializeField, Range(0, 1)] private float cautionIntensity;
    [SerializeField, Range(0, 1)] private float dangerIntensity;

    public HealthCondition Condition
    {
        get
        {
            if (health > cautionBreakpoint) return HealthCondition.Good;
            if (health > dangerBreakpoint) return HealthCondition.Caution;
            return HealthCondition.Danger;
        }
    }

    private float lastHealth = float.NaN;
    private HealthCondition lastCondition;
    private float lastIntensity = float.NaN;

    private void Awake()
    {
        if (globalVolume && globalVolume.profile) globalVolume.profile.TryGet(out volumeVignette);
        var playerCamera = GetComponentInChildren<Camera>(true);
        if (damageVignetteMaterial && playerCamera)
        {
            suitVignette = SuitDamageVignette.Create(playerCamera, damageVignetteMaterial);
            if (volumeVignette != null) volumeVignette.intensity.value = 0;
        }

        RefreshFeedback();
    }

    private void OnDestroy()
    {
        if (suitVignette) Destroy(suitVignette.gameObject);
    }

    public void TakeDamage(float damageTaken)
    {
        health -= damageTaken;
        RefreshFeedback();
    }

    public void HealDamage(float damageHealed)
    {
        health = Mathf.Min(100f, health + damageHealed);
        RefreshFeedback();
    }

    // Inspector edits and legacy callers can write the public health field directly.
    private void LateUpdate() => RefreshFeedback();

    private void RefreshFeedback()
    {
        var condition = Condition;
        float intensity = condition switch
        {
            HealthCondition.Good => goodIntensity,
            HealthCondition.Caution => cautionIntensity,
            _ => dangerIntensity
        };
        if (health == lastHealth && condition == lastCondition && intensity == lastIntensity) return;
        if (suitVignette)
        {
            float critical = condition == HealthCondition.Danger
                ? Mathf.Lerp(.28f, 1f, 1f - Mathf.Clamp01(health / Mathf.Max(1f, dangerBreakpoint))) : 0f;
            suitVignette.SetReading(intensity, float.IsNaN(lastHealth) ? 0 : lastHealth - health, critical);
        }
        else if (volumeVignette != null)
            volumeVignette.intensity.value = intensity;
        lastIntensity = intensity;
        lastHealth = health;
        lastCondition = condition;
        OnHealthChanged?.Invoke();
    }
}
