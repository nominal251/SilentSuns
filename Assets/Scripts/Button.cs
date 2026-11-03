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

    public void Interact(GameObject interactionSource)
    {
        if (requiresEnable && !buttonActive)
            return;

        if (isToggle)
            activated = !activated;
        else
            activated = true;

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
        GetComponent<Renderer>().material = activated ? activeMat : inactiveMat;
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