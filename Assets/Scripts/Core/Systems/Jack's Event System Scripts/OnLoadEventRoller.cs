using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OnLoadEventRoller : MonoBehaviour
{
    [SerializeField] private EventManager eventManager;
    [SerializeField] private string triggerId = "onLoad";
    [SerializeField] private List<GameEventDefinition> events = new List<GameEventDefinition>();
    [SerializeField, Min(0)] private int maxEventsOnLoad = 1;

    private IEnumerator Start()
    {
        if (eventManager == null) eventManager = FindAnyObjectByType<EventManager>();
        if (eventManager == null)
        {
            Debug.LogWarning("[OnLoadEventRoller] EventManager not found", this);
            yield break;
        }

        // Rolls are blocked while the manager is suspended
        yield return new WaitUntil(() => !eventManager.Suspended && Time.timeScale > 0f);

        List<GameEventDefinition> pool = new List<GameEventDefinition>(events);
        int played = 0;

        while (pool.Count > 0 && played < maxEventsOnLoad)
        {
            int index = Random.Range(0, pool.Count);
            GameEventDefinition pick = pool[index];
            pool.RemoveAt(index);

            if (pick != null && eventManager.TryPlaySpecific(pick, triggerId, transform))
                played++;
        }

        Debug.Log($"[OnLoadEventRoller] Played {played}/{maxEventsOnLoad} events on load", this);
    }
}