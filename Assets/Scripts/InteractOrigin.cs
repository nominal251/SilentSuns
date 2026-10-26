using UnityEngine;
using UnityEngine.InputSystem;

public class InteractOrigin : MonoBehaviour
{
    public float interactDistance = 3f;

    private Interactable currentInteractable;
    private Interactable heldInteractable;

    void Update()
    {
        // Release held controls before pausing; menu clicks must not reach instruments.
        if (SilentSuns.Menu.TerminalPause.BlocksGameplayInput)
        {
            if (heldInteractable != null)
                heldInteractable.StopInteract();
            heldInteractable = null;
            currentInteractable = null;
            return;
        }

        Ray ray = new Ray(transform.position, transform.forward);

        currentInteractable = null;

        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance))
        {
            if (hit.collider.CompareTag("Interactable"))
            {
                currentInteractable = hit.collider.GetComponent<Interactable>();
            }
        }

        // start interaction
        // Use the same remappable action as the settings screen.
        var interact = SilentSuns.Menu.TerminalBindings.Controls?.FindAction("Interact");
        if (interact != null && interact.WasPressedThisFrame())
        {
            if (currentInteractable != null)
            {
                currentInteractable.Interact();
                heldInteractable = currentInteractable;
                // Controls with an EventTrigger roll once when pressed, not every held frame.
                var eventTrigger = ((MonoBehaviour)currentInteractable).GetComponent<EventTrigger>();
                if (eventTrigger != null) eventTrigger.Fire(transform);
            }
        }

        // while holding interact
        if (interact != null && interact.IsPressed() && heldInteractable != null)
        {
            // check if the object is still within range
            float distance = Vector3.Distance(
                transform.position,
                ((MonoBehaviour)heldInteractable).transform.position
            );

            if (distance > interactDistance)
            {
                heldInteractable.StopInteract();
                heldInteractable = null;
            }
        }

        // on release interact
        if (interact != null && interact.WasReleasedThisFrame())
        {
            if (heldInteractable != null)
            {
                heldInteractable.StopInteract();
                heldInteractable = null;
            }
        }
    }
}
