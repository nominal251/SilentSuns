
using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public class SineWaveController : MonoBehaviour
{
    [Header("Wave Settings")]

    public float frequency = 10f;
    public float magnitude = 0.1f;
    public float offset = 0f;

    private Renderer meshRenderer;
    private MaterialPropertyBlock propertyBlock;

    void Awake()
    {
        meshRenderer = GetComponent<Renderer>();

        propertyBlock = new MaterialPropertyBlock();

        UpdateShaderProperties();
    }

    public void UpdateShaderProperties()
    {
        meshRenderer.GetPropertyBlock(propertyBlock);

        propertyBlock.SetFloat("_Frequency", frequency);
        propertyBlock.SetFloat("_Magnitude", magnitude);
        propertyBlock.SetFloat("_Offset", offset);

        meshRenderer.SetPropertyBlock(propertyBlock);
    }

    public void SetFrequency(float value)
    {
        frequency = value;
        UpdateShaderProperties();
    }

    public void SetMagnitude(float value)
    {
        magnitude = value;
        UpdateShaderProperties();
    }

    public void SetOffset(float value)
    {
        offset = value;
        UpdateShaderProperties();
    }
}