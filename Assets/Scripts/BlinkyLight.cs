using UnityEngine;

public class BlinkyLight : MonoBehaviour
{
    public GameObject light;
    public bool blink;
    public float blinkInterval = 0.75f;

    private float timer = 0f;

    void Update()
    {
        if (blink)
        {
            timer += Time.deltaTime;

            if (timer >= blinkInterval)
            {
                timer = 0f;
                light.SetActive(!light.activeSelf);
            }
        }
    }
}