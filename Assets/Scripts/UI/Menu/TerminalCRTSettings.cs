using UnityEngine;

namespace SilentSuns.Menu
{
    [CreateAssetMenu(menuName = "Silent Suns/CRT Settings")]
    public class TerminalCRTSettings : ScriptableObject
    {
        [Header("Phosphors")]
        [Range(0f, 0.65f)] public float scanlines = 0.52f;
        [Range(240f, 720f)] public float scanLineCount = 360f;
        [Range(0f, 0.3f)] public float glow = 0.16f;
        [Range(0f, 0.3f)] public float shadowMask = 0.18f;
        [Range(0f, 0.1f)] public float persistence = 0.045f;
        [Header("Glass")]
        [Range(0f, 0.01f)] public float curvature = 0.0025f;
        [Range(0f, 0.3f)] public float vignette = 0.14f;
        [Range(0f, 0.5f)] public float convergence = 0.28f;
        [Header("Electronics")]
        [Range(0f, 0.02f)] public float noise = 0.002f;
        [Range(0f, 0.02f)] public float flicker = 0.003f;
        [Tooltip("Average disturbances per second. Zero disables them.")]
        [Range(0f, 1f)] public float glitchFrequency = 0.32f;
    }
}
