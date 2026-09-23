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