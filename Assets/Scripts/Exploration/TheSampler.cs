using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Sampler : Objective
{
    public Button analyzeButton;
    public TextMeshPro statusText;
    [SerializeField, Min(1f)] private float analysisSeconds = 20f;

    private ObjectiveSystem system;
    private int totalRequired;
    private int stored;
    private int analyzed;
    private bool analyzing;
    private bool pendingStart;
    private float remaining;

    private void Start()
    {
        if (analyzeButton != null) analyzeButton.onInteracted.AddListener(HandlePress);
        SceneSwap.SurfaceEntered += HandleSurfaceEntered;

        foreach (var s in FindObjectsByType<ObjectiveSystem>(FindObjectsInactive.Include))
        {
            if (s.gameObject.scene == SceneSwap.SurfaceScene) continue;
            system = s;
            system.Register(this); // ignores duplicates
            break;
        }
    }

    private void OnDestroy()
    {
        if (analyzeButton != null) analyzeButton.onInteracted.RemoveListener(HandlePress);
        SceneSwap.SurfaceEntered -= HandleSurfaceEntered;
    }

    private static int CountSamplesInSurface()
    {
        Scene surface = SceneSwap.SurfaceScene;
        if (!surface.IsValid()) return 0;

        int count = 0;
        foreach (GameObject root in surface.GetRootGameObjects())
            count += root.GetComponentsInChildren<SampleItem>(true).Length;
        return count;
    }

    private void HandleSurfaceEntered()
    {
        int count = CountSamplesInSurface();
        if (count == 0) return; // no samples on this planet

        if (IsActive && !IsComplete)
        {
            totalRequired += count; // still working through earlier samples
            return;
        }

        ResetObjective();           // clears progress (and the pause flag)
        SetPaused(system != null && system.Suspended); // restore the system's pause state
        totalRequired = count;
        pendingStart = true;        // starts in Update once unpaused
    }

    public bool Collect()
    {
        if (!IsRunning) return false;
        stored++;
        return true;
    }

    private void HandlePress()
    {
        if (!IsRunning || analyzing || stored <= 0) return;
        stored--;
        analyzing = true;
        remaining = analysisSeconds;
    }

    private void Update()
    {
        if (pendingStart && !IsPaused)
        {
            pendingStart = false;
            TriggerObjectiveStart();
        }

        if (analyzing && IsRunning) // frozen while paused
        {
            remaining -= Time.deltaTime;
            if (remaining <= 0f) FinishAnalysis();
        }
        RefreshText();
    }

    private void FinishAnalysis()
    {
        analyzing = false;
        analyzed++;
        if (analyzed >= totalRequired) CompletePhase();
    }

    public override string GetProgressText() => $"{Title}: {analyzed}/{totalRequired}";

    protected override void OnObjectiveReset()
    {
        analyzed = 0; stored = 0; totalRequired = 0; analyzing = false; pendingStart = false;
    }

    private void RefreshText()
    {
        if (statusText == null) return;

        string header = $"SAMPLES ANALYZED: {analyzed}/{totalRequired}\n";

        if (IsComplete)       statusText.text = header + "ALL ANALYSES COMPLETE";
        else if (analyzing)
        {
            int t = Mathf.CeilToInt(remaining);
            statusText.text = header + $"ANALYZING...  {t / 60:00}:{t % 60:00}";
        }
        else if (stored > 0)  statusText.text = header + "SAMPLES READY\nPRESS TO ANALYZE";
        else if (IsActive)    statusText.text = header + "COLLECT SAMPLES";
        else                  statusText.text = "NO SAMPLES TO ANALYZE";
    }
}