using UnityEngine;

public class Planet : MonoBehaviour
{
    public string name;

    [Header("Sine Parameters for Waveform Scanner")]
    public float sineMag;
    public float sineFreq;
    public float sineOffset;

    [Header("Parameters for Spectrogram")]
    public Sprite spectrogramImage;
    public AudioClip spectrogramAudio;

    [Header("Parameters for Comp Scanner (Placeholder)")]
    public string atmoText1;
    public string atmoText2;
    public string compText1;
    public string compText2;

    [Header("Objective Completion Trackers")]
    // OBJECTIVE COMPLETION TRACKERS
    public bool waveformComplete = false;
    //public bool imageComplete = false;
    public bool compComplete = false;
    public bool probeComplete = false;

    public float Radius
    {
        get
        {
            SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();

            if (spriteRenderer != null)
            {
                return spriteRenderer.bounds.extents.x;
            }

            return 0f;
        }
    }
}