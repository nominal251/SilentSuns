using UnityEngine;

public class Button : MonoBehaviour, Interactable
{
    public bool isToggle = false;
    public bool activated = false;

    public Material inactiveMat;
    public Material activeMat;

    public void Interact()
    {
        if (isToggle)
        {
            activated = !activated;
            RefreshMat();
        }
        else
        {
            activated = true;
            RefreshMat();
        }
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
        if (activated)
        {
            GetComponent<Renderer>().material = activeMat;
        }
        else
        {
            GetComponent<Renderer>().material = inactiveMat;
        }
    }
}