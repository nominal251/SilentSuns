using UnityEngine;

[CreateAssetMenu(
    fileName = "NewFloppyData",
    menuName = "Silent Suns/Floppy Data"
)]
public class FloppyData : ScriptableObject
{
    [Header("Disk Information")]
    public string diskName;
    public string logTitle;
    public string author;
    public string dateRecorded;

    [Header("Log Contents")]
    [TextArea(10, 30)]
    public string logText;
}