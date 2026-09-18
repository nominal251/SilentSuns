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
        if (Keyboard.current.fKey.wasPressedThisFrame)
        {
            if (currentInteractable != null)
            {
                currentInteractable.Interact();
                heldInteractable = currentInteractable;
            }
        }

        // while holding F
        if (Keyboard.current.fKey.isPressed && heldInteractable != null)
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

        // on release F
        if (Keyboard.current.fKey.wasReleasedThisFrame)
        {
            if (heldInteractable != null)
            {
                heldInteractable.StopInteract();
                heldInteractable = null;
            }
        }
    }
}