using UnityEngine;

public class SampleItem : MonoBehaviour
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

    private void HandleCollect()
    {
        if (sampler == null) return;
        sampler.Collect();
        HideGroup();
    }

    public void HideGroup()
    {
        (sampleGroup != null ? sampleGroup : gameObject).SetActive(false);
    }
}