
using UnityEngine;

public enum GameEventCategory
{
    mainStory,
    spook,
    violentSpook,
    storyRandom,
    cryoSleep
}

//Leave Trigger ID empty to allow a specific event to be invoked from any trigger
//useful for small chance spooks like Foxy Jumpscare, Random Audio Clips, etc etc
[CreateAssetMenu(menuName = "System Things/Events/Event Definition", fileName = "Event_")]
public class GameEventDefinition : ScriptableObject
{
    [Header("Identity")]
    public string eventId;

    public GameEventCategory[] categories = new GameEventCategory[]
    {
        GameEventCategory.mainStory
    };

    [TextArea]
    public string designerNotes;

    [Header("Eligibility")]
    [Range(0f, 100f)] public float minimumWeirdness = 0f;
    [Range(0f, 100f)] public float maximumWeirdness = 100f;
    [Min(0f)] public float selectionWeight = 1f;
    public string[] triggerIds;
    public bool oncePerRun;
    public bool oncePerSystem;
    [Min(0)] public int maxTriggersPerRun = 0; //0 means unlimited unless oncePerRun is enabled

    [Header("Implementation")]
    [Tooltip("Optional prefab whose component implements IGameEventSequence. EventStarted listeners can also respond to the event")]
    public GameObject eventPrefab;

    //Checks whether this event belongs to the requested category
    public bool HasCategory(GameEventCategory category)
    {
        if (categories == null)
            return false;

        foreach (GameEventCategory eventCategory in categories)
        {
            if (eventCategory == category)
                return true;
        }

        return false;
    }
}
