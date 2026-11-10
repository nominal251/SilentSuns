using System.Collections.Generic;
using UnityEngine;

public class PowerControlledScreen : MonoBehaviour
{
    [Header("Blackout")]
    [SerializeField] private Material blackoutMat;
    [SerializeField] private List<MeshRenderer> screens;

    private readonly List<Material> originalMats = new List<Material>();
    private bool isBlackedOut;

    [ContextMenu("System ON")]
    public void TriggerYes()
    {
        TriggerEMP(true);
    }

    [ContextMenu("System OFF")]
    public void TriggerNo()
    {
        TriggerEMP(false);
    }

    public void TriggerEMP(bool powered)
    {
        if (blackoutMat == null)
        {
            Debug.LogWarning(
                $"[PowerControlledScreen] No blackout material assigned on {name}",
                this);
            return;
        }

        if (!powered)
        {
            if (isBlackedOut) return; // don't overwrite saved originals

            originalMats.Clear();
            foreach (var screen in screens)
            {
                originalMats.Add(screen.sharedMaterial);
                screen.sharedMaterial = blackoutMat;
            }
            isBlackedOut = true;
        }
        else
        {
            if (!isBlackedOut) return;

            for (int i = 0; i < screens.Count; i++)
            {
                screens[i].sharedMaterial = originalMats[i];
            }
            originalMats.Clear();
            isBlackedOut = false;
        }
    }
}