using UnityEngine;

public class SampleItem : Objective
{
    [SerializeField] private GameObject sampleGroup;

    public Button button;
    private Sampler sampler;

    private void Awake()
    {
        if (button != null) button.onInteracted.AddListener(HandleCollect);
        else Debug.LogWarning("SampleItem: no Button on this object.", this);
    }

    private void Start()
    {
        sampler = FindAnyObjectByType<Sampler>();
    }

    private void OnDestroy()
    {
        if (button != null) button.onInteracted.RemoveListener(HandleCollect);
    }

    public override string GetProgressText() =>
        IsComplete ? "Sample collected" : "Collect sample";

    private void HandleCollect()
    {
        if (IsComplete || !IsRunning) return;
        if (sampler == null || !sampler.Collect()) return;

        Complete();
        HideGroup();
    }

    public void HideGroup()
    {
        (sampleGroup != null ? sampleGroup : gameObject).SetActive(false);
    }
}