using System;
using UnityEngine;

public abstract class Objective : MonoBehaviour
{
    [Header("Objective Information")]
    [SerializeField] private string title = "New Objective";
    [SerializeField, TextArea] private string description;
    [SerializeField] private bool triggersOnStart = true;
    [SerializeField, Min(1)] private int phases = 1;
    public Sprite objectiveIcon;
    public Color objectiveIconColor;

    //Do Not Touch Below: 
    public string Title => title;
    public string Description => description;
    public bool TriggersOnStart => triggersOnStart;
    public int Phases => phases;
    //Debug stuff start
    [field: SerializeField] public int CurrentPhase { get; private set; }
    //Debug stuff end
    public bool IsActive { get; private set; }
    public bool IsPaused { get; private set; }
    public bool IsComplete { get; private set; }
    public bool IsRunning => IsActive && !IsPaused;
    public event Action<Objective> Started;
    public event Action<Objective> PhaseChanged;
    public event Action<Objective> Completed;

    //Get current progress of objective
    public abstract string GetProgressText();
    //Triggers on system being aware of objective
    public virtual void OnRegistered() { }

    //Start the objective stuf and intialize it
    public void TriggerObjectiveStart()
    {
        if (IsActive || IsComplete) return;

        Debug.Log($"Objective started: {Title}");

        IsActive = true;
        IsPaused = false;
        CurrentPhase = 1;

        OnObjectiveStarted();
        OnPhaseStarted(CurrentPhase);
        Started?.Invoke(this);
    }
    //Objective Phase complete, next phase!
    protected void CompletePhase()
    {
        if (!IsRunning) return;

        if (CurrentPhase >= phases)
        {
            Complete();
            return;
        }

        Debug.Log($"Objective next phase: {Title}");

        CurrentPhase++;
        OnPhaseStarted(CurrentPhase);
        PhaseChanged?.Invoke(this);
    }

    //Triggers and completes the objective
    protected void Complete()
    {
        if (IsComplete || !IsActive) return;

        IsComplete = true;
        IsActive = false;

        OnObjectiveComplete();
        Completed?.Invoke(this);
    }

    protected virtual void OnObjectiveStarted() { }
    protected virtual void OnPhaseStarted(int phase) { }
    protected virtual void OnObjectiveComplete() { }
    protected virtual void OnObjectiveCancelled() { }
}