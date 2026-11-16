using UnityEngine;
using UnityEngine.Events;

public class Button : MonoBehaviour, Interactable
{
    public bool isToggle = false;
    public bool activated = false;

    public Material inactiveMat;
    public Material activeMat;

    public bool requiresEnable = false;
    private bool buttonActive;

    [Header("Events")]
    public UnityEvent onInteracted;

    private Renderer rend;

    private void Awake() => rend = GetComponentInChildren<Renderer>();

    public void Interact(GameObject interactionSource)
    {
        Debug.Log($"Button '{name}' Interact called", this);

        if (requiresEnable && !buttonActive)
            return;

        activated = isToggle ? !activated : true;
        RefreshMat();

        if (requiresEnable)
            buttonActive = false;

        onInteracted?.Invoke();
    }

    public void StopInteract()
    {
        if (!isToggle)
        {
            activated = false;
            RefreshMat();
        }
    }

    void RefreshMat()
    {
        Material mat = activated ? activeMat : inactiveMat;
        if (rend == null || mat == null) return;
        rend.material = mat;
    }

    public void ResetButton()
    {
        activated = false;
        RefreshMat();
    }

    public void LanderDoorDown()
    {
        buttonActive = true;
        RefreshMat();
    }
}