using System;
using System.Collections.Generic;
using UnityEngine;

public class ObjectiveSystem : MonoBehaviour
{
    [SerializeField] private List<Objective> objectives = new List<Objective>();
    public IReadOnlyList<Objective> Objectives => objectives;
    public event Action<Objective> ObjectiveCompleted;
    public event Action AllObjectivesCompleted;

    //Collect objectives at the start
    private void Awake()
    {
        CollectObjectives();
    }

    //Complete objective on destroy
    private void OnDestroy()
    {
        foreach (Objective objective in objectives)
        {
            if (objective != null)
                objective.Completed -= HandleObjectiveCompleted;
        }
    }

    //Get all objectives in scene and add them to an array
    private void CollectObjectives()
    {
        objectives.Clear();

        Objective[] found = FindObjectsByType<Objective>(FindObjectsSortMode.None);

        foreach (Objective objective in found)
            Register(objective);
    }

    //Objective spawn and add at runtime
    public void Register(Objective objective)
    {
        //If no objective, return and do nothing
        if (objective == null) return;

        objectives.Add(objective);
        objective.Completed += HandleObjectiveCompleted;
        objective.OnRegistered();

        if (objective.TriggersOnStart)
            objective.TriggerObjectiveStart();
    }

    //Trigger objective to do something once complete
    private void HandleObjectiveCompleted(Objective objective)
    {
        Debug.Log($"Objective complete: {objective.Title}");
        ObjectiveCompleted?.Invoke(objective);

        if (objectives.TrueForAll(o => o.IsComplete))
        {
            Debug.Log("All objectives complete!");
            AllObjectivesCompleted?.Invoke();
        }
    }
}