
using UnityEngine;

public class EventTrigger : MonoBehaviour
{
    [SerializeField] private EventManager eventManager;
    [SerializeField] private string triggerId = "any";
    [SerializeField] private GameEventDefinition specificEvent;

    [Header("Category Selection")]
    [SerializeField] private bool rollForCategory;
    [SerializeField] private GameEventCategory categoryToRoll = GameEventCategory.mainStory;

    [SerializeField] private bool alsoRollForSpook;

    //Attempts the configured specific event and any enabled category rolls
    public void Fire()
    {
        if (eventManager == null)
        {
            Debug.LogWarning("[EventTrigger] Trigger failed | EventManager is not assigned", this);
            return;
        }

        Debug.Log($"[EventTrigger] Fired | Trigger ID: {triggerId}");

        if (specificEvent != null)
        {
            bool success = eventManager.TryPlaySpecific(specificEvent, triggerId, transform);
            Debug.Log($"[EventTrigger] Specific event result | Event: {specificEvent.eventId} | Success: {success}");
        }

        if (rollForCategory)
        {
            bool success = eventManager.TryTriggerCategory(categoryToRoll, triggerId, transform);
            Debug.Log($"[EventTrigger] Category roll result | Category: {categoryToRoll} | Success: {success}");
        }

        if (alsoRollForSpook)
        {
            bool success = eventManager.TryTriggerSpook(triggerId, transform);
            Debug.Log($"[EventTrigger] Spook roll result | Success: {success}");
        }
    }
}
