using System;
using System.Collections.Generic;
using UnityEngine;

public class ObjectiveSystem : MonoBehaviour
{
    [SerializeField] private List<Objective> objectives = new List<Objective>();
    [SerializeField] private bool collectOnAwake = true;
    [SerializeField] private bool suspended;
    private bool completionReported;
    private GameManager game;

    public IReadOnlyList<Objective> Objectives => objectives;
    public bool Suspended => suspended;
    // An empty list or a missing objective must not finish the survey.
    public bool AllComplete => objectives.Count > 0 && objectives.TrueForAll(o => o != null && o.IsComplete);
    public event Action<Objective> ObjectiveCompleted;
    public event Action AllObjectivesCompleted;

    private void Awake()
    {
        // Test scenes can collect everything. The ship registers its four surveys explicitly.
        objectives.Clear();
        if (collectOnAwake) suspended = true;
        if (collectOnAwake)
            foreach (Objective objective in FindObjectsByType<Objective>(FindObjectsSortMode.None))
                Register(objective);
    }

    private void Start()
    {
        if (!collectOnAwake) return;
        game = SilentSuns.Menu.TerminalGameSession.Manager;
        if (game != null) game.RunStarted += ResetObjectives;
    }

    private void Update()
    {
        // The ship tracker handles its own group. Standalone surface objectives use the same menu gate.
        if (!collectOnAwake) return;
        var game = SilentSuns.Menu.TerminalGameSession.Manager;
        bool playing = game == null || game.Phase == GameManager.GamePhase.PlanetaryExploration ||
            game.Phase == GameManager.GamePhase.SystemExploration || game.Phase == GameManager.GamePhase.SystemComplete;
        SetSuspended(!playing || SilentSuns.Menu.TerminalPause.BlocksGameplayInput);
    }

    private void OnDestroy()
    {
        if (game != null) game.RunStarted -= ResetObjectives;
        foreach (Objective objective in objectives)
            if (objective != null)
                objective.Completed -= HandleObjectiveCompleted;
    }

    public void Register(Objective objective)
    {
        if (objective == null || objectives.Contains(objective)) return;
        objectives.Add(objective);
        completionReported = false;
        objective.Completed += HandleObjectiveCompleted;
        objective.OnRegistered();
        objective.SetPaused(suspended);
        if (!suspended && objective.TriggersOnStart)
            objective.TriggerObjectiveStart();
    }

    public void SetSuspended(bool value)
    {
        if (suspended == value) return;
        suspended = value;
        foreach (Objective objective in objectives)
        {
            if (objective == null) continue;
            objective.SetPaused(value);
            if (!value && objective.TriggersOnStart)
                objective.TriggerObjectiveStart();
        }
    }

    public void ResetObjectives()
    {
        if (collectOnAwake) suspended = true;
        completionReported = false;
        foreach (Objective objective in objectives)
        {
            if (objective == null) continue;
            objective.ResetObjective();
            objective.SetPaused(suspended);
            if (!suspended && objective.TriggersOnStart)
                objective.TriggerObjectiveStart();
        }
    }

    private void HandleObjectiveCompleted(Objective objective)
    {
        Debug.Log($"Objective complete: {objective.Title}");
        ObjectiveCompleted?.Invoke(objective);
        if (AllComplete && !completionReported)
        {
            completionReported = true;
            Debug.Log("All objectives complete!");
            AllObjectivesCompleted?.Invoke();
        }
    }
}
