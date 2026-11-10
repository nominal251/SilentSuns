using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Sampler : Objective
{
    //Button hook up and analysis text
    public Button analyzeButton;
    public TextMeshPro statusText;
    [SerializeField, Min(1f)] private float analysisSeconds = 20f;

    //All samples
    private readonly List<SampleItem> samples = new List<SampleItem>();
    private SampleItem loaded;
    private bool analyzing;
    private float remaining;
    private int analyzed;
    public SampleItem Loaded => loaded;

    public void Register(SampleItem item)
    {
        if (!samples.Contains(item)) samples.Add(item);
    }

    private void Start()
    {
        analyzeButton.onInteracted.AddListener(HandlePress);
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
        Debug.Log($"Sampler: analyze pressed (running={IsRunning}, analyzing={analyzing}, loaded={loaded != null})", this);

        if (!IsRunning || analyzing || loaded == null) return;

        analyzing = true;
        remaining = analysisSeconds;
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
        samples.Remove(loaded);
        Destroy(loaded.gameObject);
        loaded = null;

        if (analyzed >= analyzed + samples.Count)
            CompletePhase();
    }

    public override string GetProgressText() =>
        $"{Title}: {analyzed}/{analyzed + samples.Count}";

    protected override void OnObjectiveReset()
    {
        analyzed = 0;
        analyzing = false;
        loaded = null;
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