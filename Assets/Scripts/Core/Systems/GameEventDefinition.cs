using System;
using UnityEngine;
using UnityEngine.SceneManagement;

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

    [Header("Scene Restrictions")]
    public bool limitedToScenes;
    [Tooltip("Scene names exactly as they appear in Build Settings. The event is allowed if any currently loaded scene is listed")]
    public string[] whitelistedScenes;

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

    //Checks whether any currently loaded scene is whitelisted for this event
    public bool IsAllowedInLoadedScenes()
    {
        if (!limitedToScenes)
            return true;

        if (whitelistedScenes == null || whitelistedScenes.Length == 0)
            return false;

        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);

            if (!scene.isLoaded)
                continue;

            foreach (string allowed in whitelistedScenes)
            {
                if (string.Equals(allowed, scene.name, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }

        return false;
    }
}