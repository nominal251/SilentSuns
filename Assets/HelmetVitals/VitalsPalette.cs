using UnityEngine;

[CreateAssetMenu(menuName = "Silent Suns/Vitals palette")]
public sealed class VitalsPalette : ScriptableObject
{
    public Color good = new Color32(162, 255, 179, 255);
    public Color caution = new Color32(255, 234, 73, 255);
    public Color danger = new Color32(240, 119, 124, 255);
    public Color glass = new Color32(5, 11, 28, 255);
    public Color lettering = new Color32(229, 236, 245, 255);
    public Color frame = new Color32(220, 234, 255, 255);
    public Color emptyCell = new Color32(35, 54, 103, 255);

    public Color Signal(HealthCondition condition) => condition switch
    {
        HealthCondition.Caution => caution,
        HealthCondition.Danger => danger,
        _ => good
    };
}
