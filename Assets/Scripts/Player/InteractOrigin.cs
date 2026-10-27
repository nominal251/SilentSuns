using UnityEngine;
using UnityEngine.InputSystem;

public class InteractOrigin : MonoBehaviour
{
    public float interactDistance = 3f;

    private Interactable currentInteractable;
    private Interactable heldInteractable;

    void Update()
    {
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
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (currentInteractable != null)
            {
                currentInteractable.Interact();
                heldInteractable = currentInteractable;
            }
        }

        // while holding interact
        if (Mouse.current.leftButton.isPressed && heldInteractable != null)
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
        if (Mouse.current.leftButton.wasReleasedThisFrame)
        {
            if (heldInteractable != null)
            {
                heldInteractable.StopInteract();
                heldInteractable = null;
            }
        }
    }
}