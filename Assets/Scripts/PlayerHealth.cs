using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PlayerHealth : MonoBehaviour
{
    public float health /*{ get; private set; }*/ = 100f;

    [SerializeField] private Volume globalVolume;
    private Vignette vignette;

    [Header("Health state breakpoints")]
    [SerializeField][Range(0, 100)] private float cautionBreakpoint;
    [SerializeField][Range(0, 100)] private float dangerBreakpoint;

    [Header("Vignette intensity at breakpoints.")]
    [SerializeField] [Range(0, 1)] private float goodIntensity;
    [SerializeField] [Range(0, 1)] private float cautionIntensity;
    [SerializeField] [Range(0, 1)] private float dangerIntensity;


    private void Awake()
    {
        if (globalVolume.profile.TryGet<Vignette>(out vignette))
        {
            UpdateVignette();
        }
        else
        {
            Debug.LogError("Vignette missing from the Volume Profile!");
        }
    }

    public void TakeDamage(float damageTaken)
    {
        health -= damageTaken;
        UpdateVignette();
    }

    public void HealDamage(float damageHealed)
    {
        health += damageHealed;
        if(health > 100)
        {
            health = 100;
        }
        UpdateVignette();
    }

    private void UpdateVignette()
    {
        if(health > cautionBreakpoint)
        {
            vignette.intensity.value = goodIntensity;
        }
        else if (health > dangerBreakpoint)
        {
            vignette.intensity.value = cautionIntensity;
        }
        else
        {
            vignette.intensity.value = dangerIntensity;
        }
    }
}
