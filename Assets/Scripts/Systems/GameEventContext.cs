using UnityEngine;

public struct GameEventContext
{
    public string TriggerId { get; }
    public Transform Source { get; }
    public Vector3 WorldPosition { get; }

    public GameEventContext(string triggerId, Transform source)
    {
        TriggerId = triggerId;
        Source = source;
        WorldPosition = source != null ? source.position : Vector3.zero;
    }
}

//Implement this on an event prefab to start it
public interface IGameEventSequence
{
    void Begin(GameEventContext context);
}
