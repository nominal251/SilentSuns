using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class SineWave : MonoBehaviour
{
    public float frequency = 2f;
    public float magnitude = 1f;
    public float xOffset = 0f;

    public float xLength = 10f;
    public int resolution = 100;

    private LineRenderer lineRenderer;

    void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
    }

    void Start()
    {
        GenerateWave();
    }

    void Update()
    {
        GenerateWave();
    }

    void GenerateWave()
    {
        lineRenderer.positionCount = resolution;

        for (int i = 0; i < resolution; i++)
        {
            float t = (float)i / (resolution - 1);

            float x = t * xLength;

            float y = Mathf.Sin((x + xOffset) * frequency) * magnitude;

            lineRenderer.SetPosition(i, new Vector3(x, y, 0f) + transform.position);
        }
    }
}