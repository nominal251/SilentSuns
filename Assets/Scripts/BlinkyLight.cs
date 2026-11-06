using UnityEngine;

public class BlinkyLight : MonoBehaviour
{
    public GameObject light;
    public bool blink;
    public float blinkInterval = 0.75f;

    private float timer = 0f;
    private bool warned;

    void Update()
    {
        if (!blink) return;
        if (light != null)
        {
            timer += Time.deltaTime;

            if (timer >= blinkInterval)
            {
                timer = 0f;
                light.SetActive(!light.activeSelf);
            }
        }
        else if (!warned)
        {
            warned = true;
            Debug.LogWarning(gameObject.name + " has no blinking light assigned.", this);
        }
    }
}