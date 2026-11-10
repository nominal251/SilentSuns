using UnityEngine;

public abstract class SurfaceEvent : ScriptableObject
{
    [Range(0f, 1f)] public float chance = 0.1f;
    public abstract void Execute(SurfaceEventManager manager);
}