using UnityEngine;

public class CollectItemsObjective : Objective
{
    [Header("Collect Settings")]
    [Tooltip("Items required for each phase. Element 0 = phase 1, element 1 = phase 2, etc. Keep the length equal to Phases.")]
    [SerializeField] private int[] requiredPerPhase = { 5, 3 };
    [SerializeField] private int currentAmount;

    //Items needed for whichever phase we're currently in
    private int RequiredAmount
    {
        get
        {
            if (requiredPerPhase == null || requiredPerPhase.Length == 0) return 1;

            int index = Mathf.Clamp(CurrentPhase - 1, 0, requiredPerPhase.Length - 1);
            return Mathf.Max(1, requiredPerPhase[index]);
        }
    }

    //Add progress
    public void AddProgress(int amount = 1)
    {
        if (!IsRunning) return;

        currentAmount += amount;

        if (currentAmount >= RequiredAmount)
        {
            currentAmount = RequiredAmount;
            CompletePhase(); //Moves to next phase, or completes the objective after the last one
        }
    }

    //Triggers at the start of every phase (including phase 1), so progress resets each time
    protected override void OnPhaseStarted(int phase)
    {
        currentAmount = 0;
        Debug.Log($"{Title}: phase {phase}/{Phases} - collect {RequiredAmount}");
    }

    //Triggers on completion of objective
    protected override void OnObjectiveComplete()
    {
        Debug.Log($"{Title} finished!");
    }

    //Checks progress of current task
    public override string GetProgressText()
    {
        return $"{Title} (Phase {CurrentPhase}/{Phases}): {currentAmount}/{RequiredAmount}";
    }

    //Debug
    [ContextMenu("Example Add Progress")]
    public void AddProgressTest()
    {
        AddProgress(1);
    }
}