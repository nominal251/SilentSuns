using UnityEngine;

public class InteractOrigin : MonoBehaviour
{
    public float interactDistance = 3f;

    private Interactable currentInteractable;
    private Interactable heldInteractable;
    private InputHandler inputHandler;

    void Start()
    {
        inputHandler = InputHandler.instance;
    }

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
        if (inputHandler == null) return;
        if (inputHandler.interactPressed)
        {
            if (currentInteractable != null)
            {
                currentInteractable.Interact(gameObject);
                heldInteractable = currentInteractable;
                // Controls with an EventTrigger roll once when pressed, not every held frame.
                var eventTrigger = ((MonoBehaviour)currentInteractable).GetComponent<EventTrigger>();
                if (eventTrigger != null) eventTrigger.Fire(transform);
            }
        }

        // while holding interact
        if (inputHandler.interactInput && heldInteractable != null)
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
        if (inputHandler.interactReleased)
        {
            if (heldInteractable != null)
            {
                heldInteractable.StopInteract();
                heldInteractable = null;
            }
        }
    }
}
