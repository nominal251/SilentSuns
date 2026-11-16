using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Sampler : Objective
{
    //Button hook up and analysis text
    public Button analyzeButton;
    public TextMeshPro statusText;
    [SerializeField, Min(1f)] private float analysisSeconds = 20f;

    //All samples
    private readonly List<SampleItem> samples = new List<SampleItem>();
    private int totalRequired;
    private int stored;
    private int analyzed;
    private SampleItem loaded;
    private bool analyzing;
    private float remaining;
    public SampleItem Loaded => loaded;

    public void Register(SampleItem item)
    {
        if (!samples.Contains(item)) samples.Add(item);
    }

    private void Start()
    {
        analyzeButton.onInteracted.AddListener(HandlePress);
        SceneSwap.SurfaceEntered += HandleSurfaceEntered;

        foreach (var system in FindObjectsByType<ObjectiveSystem>(FindObjectsInactive.Include))
        {
            if (system.gameObject.scene == SceneSwap.SurfaceScene) continue;
            system.Register(this);
            break;
        }
    }

    private void OnDestroy()
    {
        if (analyzeButton != null) analyzeButton.onInteracted.RemoveListener(HandlePress);
    }

    //Incase we dont want buttons
    //Needs a trigger collider
    private void OnTriggerEnter(Collider other)
    {
        if (!IsRunning || loaded != null) return;

        SampleItem item = other.GetComponentInParent<SampleItem>();
        if (item != null) TryLoad(item);

        loaded = item;
    }

    //Debug stuff with interaction system
    private void HandlePress()
    {
        if (!IsRunning || analyzing || stored <= 0) return;
        stored--;
        analyzing = true;
        remaining = analysisSeconds;
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

    public void Collect()
    {
        if (IsActive) stored++;
    }

    private void HandleSurfaceEntered()
    {
        int count = CountSamplesInSurface();
        if (count == 0) return; //no samples here

        if (IsActive && !IsComplete)
        {
            totalRequired += count;
            return;
        }

        ResetObjective();
        totalRequired = count;
        TriggerObjectiveStart();
    }

    private void Update()
    {
        if (analyzing && IsRunning) //when paused freeze the timer
        {
            remaining -= Time.deltaTime;
            if (remaining <= 0f) FinishAnalysis();
        }
        RefreshText();
    }

    //Complete objective when analysis finished
    private void FinishAnalysis()
    {
        analyzing = false;
        analyzed++;
        if (analyzed >= totalRequired) CompletePhase();
    }

    public override string GetProgressText() => $"{Title}: {analyzed}/{totalRequired}";

    protected override void OnObjectiveReset()
    {
        analyzed = 0; stored = 0; totalRequired = 0; analyzing = false;
    }

    //Show progress
    private void RefreshText()
    {
        if (statusText == null) return;

        int total = analyzed + samples.Count;
        string header = $"SAMPLES ANALYZED: {analyzed}/{total}\n";

        if (IsComplete)       statusText.text = header + "ALL ANALYSES COMPLETE";
        else if (analyzing)
        {
            int t = Mathf.CeilToInt(remaining);
            statusText.text = header + $"ANALYZING...  {t / 60:00}:{t % 60:00}";
        }
        else if (loaded != null) statusText.text = header + "SAMPLE LOADED\nPRESS TO ANALYZE";
        else                     statusText.text = header + "PLACE SAMPLE IN MACHINE";
    }

    public bool TryLoad(SampleItem item)
    {
        if (!IsRunning || loaded != null || !samples.Contains(item)) return false;
        loaded = item;
        return true;
    }
}