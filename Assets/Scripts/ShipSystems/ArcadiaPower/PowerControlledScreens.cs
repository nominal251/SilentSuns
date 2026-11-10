using UnityEngine;
using UnityEngine.UI;

public class PowerControlledScreen : MonoBehaviour
{
    [Header("Blackout Overlay")]
    [SerializeField] private Image blackoutOverlay;

    public void SetPowered(bool powered)
    {
        if (blackoutOverlay == null)
        {
            Debug.LogWarning(
                $"[PowerControlledScreen] No blackout overlay assigned on {name}",
                this);
            return;
        }

        blackoutOverlay.enabled = !powered;
    }
}