
using System;
using System.Collections.Generic;
using UnityEngine;

public class EventManager : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private List<GameEventDefinition> eventDefinitions = new List<GameEventDefinition>();

    [Header("Spook Frequency")]
    [SerializeField, Range(0f, 1f)] private float spookChanceAtZeroWeirdness = 0.03f;
    [SerializeField, Range(0f, 1f)] private float spookChanceAtMaxWeirdness = 1f;
    [SerializeField, Min(0f)] private float minimumSecondsBetweenSpooks = 30f;

    private readonly HashSet<string> playedOncePerRun = new HashSet<string>();
    private readonly Dictionary<string, int> runCounts = new Dictionary<string, int>();
    private readonly Dictionary<string, int> systemCounts = new Dictionary<string, int>();

    private int trackedSystemNumber = -1;
    private float lastSpookTime = float.NegativeInfinity;

    public event Action<GameEventDefinition, GameEventContext> EventStarted;

    //Exposes the current weirdness value for development diagnostics
    public float CurrentWeirdness => gameManager != null ? gameManager.Weirdness : -1f;

    //Returns the event definitions currently registered with this manager
    public IReadOnlyList<GameEventDefinition> EventDefinitions => eventDefinitions;

    //Attempts to play a specific event 
    public bool TryPlaySpecific(GameEventDefinition definition, string triggerId, Transform source = null)
    {
        EnsureSystemTracking();

        Debug.Log($"[EventManager] Specific event requested: {GetEventName(definition)} | Trigger: {triggerId}");

        if (!IsEligible(definition, triggerId, true))
        {
            Debug.LogWarning($"[EventManager] EVENT FAILED | {GetEventName(definition)} | Reason: Event was not eligible");
            return false;
        }

        return StartEvent(definition, triggerId, source, true);
    }

    //Rolls for a random eligible event in the requested category as it normally would
    public bool TryTriggerCategory(GameEventCategory category, string triggerId, Transform source = null)
    {
        EnsureSystemTracking();

        Debug.Log($"[EventManager] NORMAL CATEGORY ROLL | Category: {category} | Trigger: {triggerId}");
        LogWeirdnessCalculation();

        if (gameManager == null)
        {
            Debug.LogError("[EventManager] EVENT FAILED | GameManager is not assigned");
            return false;
        }

        if (category == GameEventCategory.spook)
        {
            float elapsed = Time.time - lastSpookTime;

            Debug.Log($"[EventManager] Spook cooldown | Elapsed: {elapsed:F2}s | Required: {minimumSecondsBetweenSpooks:F2}s");

            if (elapsed < minimumSecondsBetweenSpooks)
            {
                Debug.LogWarning("[EventManager] EVENT FAILED | Spook cooldown is still active");
                return false;
            }
        }

        List<GameEventDefinition> candidates = GetEligibleCandidates(category, triggerId, true);

        Debug.Log($"[EventManager] Eligible candidates for {category}: {candidates.Count}");

        if (candidates.Count == 0)
        {
            Debug.LogWarning($"[EventManager] EVENT FAILED | No eligible events found for category {category}");
            return false;
        }

        if (category == GameEventCategory.spook)
        {
            float weirdness01 = Mathf.Clamp01(gameManager.Weirdness / 100f);
            float chance = Mathf.Lerp(
                spookChanceAtZeroWeirdness,
                spookChanceAtMaxWeirdness,
                weirdness01
            );

            float roll = UnityEngine.Random.value;

            Debug.Log(
                $"[EventManager] Spook probability calculation | Weirdness: {gameManager.Weirdness:F2} | Normalized: {weirdness01:F3} | Chance at zero: {spookChanceAtZeroWeirdness:P1} | Chance at max: {spookChanceAtMaxWeirdness:P1} | Final chance: {chance:P1} | Random roll: {roll:F3}"
            );

            if (roll >= chance)
            {
                Debug.Log($"[EventManager] EVENT FAILED | Spook probability roll did not pass | Roll: {roll:F3} | Chance: {chance:F3}");
                return false;
            }
        }

        GameEventDefinition selected = ChooseWeighted(candidates);

        if (selected == null)
        {
            Debug.LogWarning($"[EventManager] EVENT FAILED | Weighted selection returned no event for {category}");
            return false;
        }

        Debug.Log($"[EventManager] Event rolled: {selected.eventId} | Category: {category}");

        return TryPlaySpecific(selected, triggerId, source);
    }

    //Rolls for a spook using the normal category selection rules
    public bool TryTriggerSpook(string triggerId, Transform source = null)
    {
        return TryTriggerCategory(GameEventCategory.spook, triggerId, source);
    }

    //Rolls for a category while bypassing normal eligibility rules and the spook cooldown and probability; Dev Thing only.
    public bool TryDevTestCategory(GameEventCategory category, string triggerId, Transform source = null)
    {
        EnsureSystemTracking();

        Debug.Log($"[EventManager] FORCED DEV TEST | Category: {category} | Trigger: {triggerId}");
        Debug.Log("[EventManager] Forced test bypasses weirdness limits, trigger restrictions, run limits, system limits, and spook probability and cooldown");

        LogWeirdnessCalculation();

        List<GameEventDefinition> candidates = GetEligibleCandidates(category, triggerId, false);

        Debug.Log($"[EventManager] Forced test candidates for {category}: {candidates.Count}");

        if (candidates.Count == 0)
        {
            Debug.LogWarning($"[EventManager] EVENT FAILED | No events with positive selection weight found for category {category}");
            return false;
        }

        GameEventDefinition selected = ChooseWeighted(candidates);

        if (selected == null)
        {
            Debug.LogWarning($"[EventManager] EVENT FAILED | Forced weighted selection returned no event for {category}");
            return false;
        }

        Debug.Log($"[EventManager] FORCED EVENT ROLLED | {selected.eventId} | Category: {category}");

        return StartEvent(selected, triggerId, source, false);
    }

    //Starts an event after it has passed normal eligibility or been selected by the forced test path
    private bool StartEvent(GameEventDefinition definition, string triggerId, Transform source, bool updateSpookCooldown)
    {
        if (definition == null)
        {
            Debug.LogError("[EventManager] EVENT FAILED | Cannot start a null definition");
            return false;
        }

        GameEventContext context = new GameEventContext(triggerId, source);

        RecordPlay(definition);

        Debug.Log($"[EventManager] EVENT SUCCEEDED | {definition.eventId} | Trigger: {triggerId} | Position: {context.WorldPosition}");

        EventStarted?.Invoke(definition, context);

        if (definition.eventPrefab != null)
        {
            GameObject instance = Instantiate(
                definition.eventPrefab,
                context.WorldPosition,
                Quaternion.identity
            );

            Debug.Log($"[EventManager] Prefab spawned | Event: {definition.eventId} | Prefab: {definition.eventPrefab.name} | Position: {context.WorldPosition}", instance);

            MonoBehaviour[] behaviours = instance.GetComponentsInChildren<MonoBehaviour>(true);
            bool sequenceFound = false;

            foreach (MonoBehaviour behaviour in behaviours)
            {
                if (!(behaviour is IGameEventSequence sequence))
                    continue;

                Debug.Log($"[EventManager] Starting sequence | Event: {definition.eventId} | Component: {behaviour.GetType().Name}", behaviour);

                sequenceFound = true;

                try
                {
                    sequence.Begin(context);
                    Debug.Log($"[EventManager] Sequence Begin returned successfully | Event: {definition.eventId}");
                }
                catch (Exception exception)
                {
                    Debug.LogError($"[EventManager] SEQUENCE FAILED | Event: {definition.eventId} | Exception: {exception}");
                }

                break;
            }

            if (!sequenceFound)
            {
                Debug.LogWarning($"[EventManager] Event prefab '{definition.eventPrefab.name}' has no component implementing IGameEventSequence | EventStarted listeners were still called", instance);
            }
        }
        else
        {
            Debug.Log($"[EventManager] No prefab assigned | Event: {definition.eventId} | EventStarted listeners can still respond");
        }

        if (definition.HasCategory(GameEventCategory.spook) && updateSpookCooldown)
        {
            lastSpookTime = Time.time;
            Debug.Log($"[EventManager] Spook cooldown timestamp updated | Time: {lastSpookTime:F2}");
        }

    else if (definition.HasCategory(GameEventCategory.spook))
    {
        Debug.Log("[EventManager] Spook cooldown unchanged | Forced development test");
    }

        return true;
    }

    //Logs the current weirdness value and its normalized value used in the spook chance calculation
    private void LogWeirdnessCalculation()
    {
        if (gameManager == null)
        {
            Debug.LogWarning("[EventManager] Weirdness calculation unavailable | GameManager is not assigned");
            return;
        }

        float weirdness01 = Mathf.Clamp01(gameManager.Weirdness / 100f);
        float chance = Mathf.Lerp(
            spookChanceAtZeroWeirdness,
            spookChanceAtMaxWeirdness,
            weirdness01
        );

        Debug.Log($"[EventManager] Weirdness calculation | Raw: {gameManager.Weirdness:F2} | Normalized: Clamp01({gameManager.Weirdness:F2} / 100) = {weirdness01:F3} | Spook chance: Lerp({spookChanceAtZeroWeirdness:F3}, {spookChanceAtMaxWeirdness:F3}, {weirdness01:F3}) = {chance:P1}");
    }

    //Returns all events in a category and optionally filters them through normal eligibility rules
    private List<GameEventDefinition> GetEligibleCandidates(
        GameEventCategory category,
        string triggerId,
        bool checkEligibility)
    {
        List<GameEventDefinition> result = new List<GameEventDefinition>();

        foreach (GameEventDefinition definition in eventDefinitions)
        {
            if (definition == null)
            {
                Debug.LogWarning("[EventManager] Skipping null event definition in the Event Definitions list");
                continue;
            }

            if (!definition.HasCategory(category))
            {
                Debug.Log($"[EventManager] Candidate skipped | {definition.eventId} | Does not belong to {category}");
                continue;
            }

            if (definition.selectionWeight <= 0f)
            {
                Debug.Log($"[EventManager] Candidate skipped | {definition.eventId} | Selection weight is zero or negative");
                continue;
            }

            if (checkEligibility && !IsEligible(definition, triggerId, true))
            {
                Debug.Log($"[EventManager] Candidate skipped | {definition.eventId} | Failed eligibility checks");
                continue;
            }

            Debug.Log($"[EventManager] Candidate accepted | {definition.eventId} | Weight: {definition.selectionWeight:F2} | Forced: {!checkEligibility}");
            result.Add(definition);
        }

        return result;
    }

    //Checks every normal gameplay restriction and reports the reason an event is blocked
    private bool IsEligible(GameEventDefinition definition, string triggerId, bool logDetails)
    {
        if (definition == null)
        {
            if (logDetails) Debug.LogWarning("[EventManager] Eligibility failed | Definition is null");
            return false;
        }

        if (string.IsNullOrWhiteSpace(definition.eventId))
        {
            if (logDetails) Debug.LogWarning("[EventManager] Eligibility failed | Event ID is empty");
            return false;
        }

        if (gameManager == null)
        {
            if (logDetails) Debug.LogWarning("[EventManager] Eligibility failed | GameManager is not assigned");
            return false;
        }

        float weirdness = gameManager.Weirdness;

        if (weirdness < definition.minimumWeirdness || weirdness > definition.maximumWeirdness)
        {
            if (logDetails) Debug.Log($"[EventManager] Eligibility failed | {definition.eventId} | Weirdness {weirdness:F2} is outside range {definition.minimumWeirdness:F2}-{definition.maximumWeirdness:F2}");
            return false;
        }

        if (!TriggerMatches(definition, triggerId))
        {
            if (logDetails) Debug.Log($"[EventManager] Eligibility failed | {definition.eventId} | Trigger ID '{triggerId}' is not allowed");
            return false;
        }

        string id = definition.eventId;

        if (definition.oncePerRun && playedOncePerRun.Contains(id))
        {
            if (logDetails) Debug.Log($"[EventManager] Eligibility failed | {id} | Already played once this run");
            return false;
        }

        if (definition.maxTriggersPerRun > 0 && GetCount(runCounts, id) >= definition.maxTriggersPerRun)
        {
            if (logDetails) Debug.Log($"[EventManager] Eligibility failed | {id} | Maximum triggers per run reached");
            return false;
        }

        if (definition.oncePerSystem && GetCount(systemCounts, id) >= 1)
        {
            if (logDetails) Debug.Log($"[EventManager] Eligibility failed | {id} | Already played in this system");
            return false;
        }

        if (definition.selectionWeight <= 0f)
        {
            if (logDetails) Debug.Log($"[EventManager] Eligibility failed | {id} | Selection weight is zero or negative");
            return false;
        }

        if (logDetails) Debug.Log($"[EventManager] Eligibility passed | {id}");

        return true;
    }

    //Checks whether a trigger ID matches the event's allowed trigger IDs
    private static bool TriggerMatches(GameEventDefinition definition, string triggerId)
    {
        if (definition.triggerIds == null || definition.triggerIds.Length == 0)
            return true;

        foreach (string allowed in definition.triggerIds)
        {
            if (allowed == "*" || string.Equals(allowed, triggerId, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    //Records event usage for once-per-run, maximum-trigger, and once-per-system rules
    private void RecordPlay(GameEventDefinition definition)
    {
        string id = definition.eventId;

        if (definition.oncePerRun)
            playedOncePerRun.Add(id);

        runCounts[id] = GetCount(runCounts, id) + 1;
        systemCounts[id] = GetCount(systemCounts, id) + 1;

        Debug.Log($"[EventManager] Play recorded | {id} | Run count: {runCounts[id]} | System count: {systemCounts[id]}");
    }

    //Clears system-specific event history when the current system changes
    private void EnsureSystemTracking()
    {
        if (gameManager == null || trackedSystemNumber == gameManager.CurrentSystemNumber)
            return;

        trackedSystemNumber = gameManager.CurrentSystemNumber;
        systemCounts.Clear();

        Debug.Log($"[EventManager] System tracking reset | Current system: {trackedSystemNumber}");
    }

    //Returns a recorded count or zero when the event has not been recorded
    private static int GetCount(Dictionary<string, int> counts, string id)
    {
        return counts.TryGetValue(id, out int count) ? count : 0;
    }

    //Chooses one event randomly, with each event's selection weight affecting its probability
    private static GameEventDefinition ChooseWeighted(List<GameEventDefinition> candidates)
    {
        float totalWeight = 0f;

        foreach (GameEventDefinition candidate in candidates)
            totalWeight += Mathf.Max(0f, candidate.selectionWeight);

        Debug.Log($"[EventManager] Weighted selection | Candidate count: {candidates.Count} | Total weight: {totalWeight:F2}");

        if (totalWeight <= 0f)
            return null;

        float choice = UnityEngine.Random.value * totalWeight;

        Debug.Log($"[EventManager] Weighted selection random point: {choice:F3}");

        foreach (GameEventDefinition candidate in candidates)
        {
            choice -= Mathf.Max(0f, candidate.selectionWeight);

            Debug.Log($"[EventManager] Weighted selection check | {candidate.eventId} | Remaining point: {choice:F3}");

            if (choice <= 0f)
                return candidate;
        }

        return candidates[candidates.Count - 1];
    }

    //Returns a readable event name for diagnostic messages
    private static string GetEventName(GameEventDefinition definition)
    {
        return definition == null ? "<null definition>" : definition.eventId;
    }

    //Clears all recorded run and system history and resets the spook cooldown
    public void ClearRunHistory()
    {
        playedOncePerRun.Clear();
        runCounts.Clear();
        systemCounts.Clear();

        trackedSystemNumber = gameManager != null ? gameManager.CurrentSystemNumber : -1;
        lastSpookTime = float.NegativeInfinity;

        Debug.Log("[EventManager] Run history cleared");
    }
}
