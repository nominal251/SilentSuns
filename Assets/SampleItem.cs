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
        button.onInteracted.AddListener(HandleCollect);
        sampler = FindAnyObjectByType<Sampler>();
        if (sampler != null) sampler.Register(this);
        else Debug.LogWarning("SampleItem: no Sampler found.", this);
    }

    private void OnDestroy()
    {
        if (button != null) button.onInteracted.RemoveListener(HandleCollect);
    }

    private void HandleCollect()
    {
        Debug.Log($"SampleItem '{name}': interacted", this);
        if (sampler != null) sampler.TryLoad(this);
        HideGroup(); // sample despawns when collected
    }

    public void HideGroup()
    {
        (sampleGroup != null ? sampleGroup : gameObject).SetActive(false);
    }
}