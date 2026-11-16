using UnityEngine;

public class FloppyDisk : MonoBehaviour
{
    [Header("Disk Data")]
    public FloppyData floppyData;

    public bool IsHeld { get; private set; }
    public bool IsInserted { get; private set; }

    public void SetHeld(bool held)
    {
        IsHeld = held;
    }

    public void SetInserted(bool inserted)
    {
        IsInserted = inserted;
    }
}