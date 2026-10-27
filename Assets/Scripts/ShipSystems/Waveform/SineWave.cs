using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class SineWave : MonoBehaviour
{

    public float scrollSpeed = 2f;

    public float frequency = 2f;
    public float magnitude = 1f;
    public float offset = 0f;

    public float xLength = 10f;
    public int resolution = 100;

    public float magButtonSens = 0.1f;
    public float freqButtonSens = 0.1f;
    public float offsetButtonSens = 0.1f;

    private LineRenderer lineRenderer;

    [ContextMenu("VisualizeSineWave")]
    public void EditorVisualize()
    {
        lineRenderer = GetComponent<LineRenderer>();
        GenerateWave();
    }

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

    public void GenerateWave()
    {
        offset -= scrollSpeed * Time.deltaTime;

        lineRenderer.positionCount = resolution;

        for (int i = 0; i < resolution; i++)
        {
            float t = (float)i / (resolution - 1);

            float x = t * xLength;

            float y = Mathf.Sin(x * frequency + offset) * magnitude;

            lineRenderer.SetPosition(i, new Vector3(x, y, 0f) + transform.position);
        }
    }
}