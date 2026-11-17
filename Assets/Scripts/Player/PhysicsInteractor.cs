using UnityEngine;

public class PhysicsInteractor : MonoBehaviour
{
    [SerializeField] private float interactDistance = 2f;
    [SerializeField] private float scrollSens = .05f;
    [SerializeField] private float handMin = .5f;
    [SerializeField] private float handMax = 2f;

    private InputHandler inputHandler;
    private GameObject camera;

    private PhysicsInteractable observedObject;
    private PhysicsInteractable heldObject;
    private GameObject hand;
    private float handPosition;
    public bool IsRotatingObject => heldObject != null && inputHandler != null && inputHandler.physicsRotateInput;

    private void Start()
    {
        inputHandler = InputHandler.instance;
        camera = transform.Find("Player Camera").gameObject;
        // The current player keeps the hand under its camera; older test scenes use the player root.
        hand = (camera.transform.Find("Hand") ?? transform.Find("Hand")).gameObject;
        handPosition = hand.transform.localPosition.z;
    }

    private void Update()
    {
        // Pause, settings, and the telescope intro must not pick up or move objects.
        if (SilentSuns.Menu.TerminalPause.BlocksGameplayInput || SilentSuns.Menu.TerminalToolInput.BlocksInput || inputHandler == null)
        {
            DropObject();
            return;
        }

        Ray ray = new Ray(camera.transform.position, camera.transform.forward);

        //raycast logic
        observedObject = null;
        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance))
        {
            if (hit.collider.TryGetComponent<PhysicsInteractable>(out PhysicsInteractable script))
                observedObject = script;
        }

        //pickup logic: hold to carry, release to drop
        if (heldObject == null)
        {
            if (inputHandler.physicsInteractPressed && observedObject != null)
            {
                heldObject = observedObject;
                heldObject.Released += HandleReleased;
                heldObject.Interact(hand);
            }
        }
        else if (!inputHandler.physicsInteractInput)
        {
            DropObject();
        }

        //manipulation logic
        if (heldObject == null) return;

        // The wheel moves in steps. A held key or controller button moves smoothly over time.
        float reachStep = inputHandler.physicsExtendIsScroll ? scrollSens : scrollSens * 30f * Time.deltaTime;
        handPosition += inputHandler.physicsExtendInput * reachStep;
        handPosition = Mathf.Clamp(handPosition, handMin, handMax);
        hand.transform.localPosition = new Vector3(hand.transform.localPosition.x, hand.transform.localPosition.y, handPosition);

        if (inputHandler.physicsRotateInput)
        {
            // Use the same sensitivity and invert setting as player look.
            Vector2 look = inputHandler.lookInput * inputHandler.lookSensitivity;
            hand.transform.Rotate(camera.transform.up, -look.x, Space.World);
            hand.transform.Rotate(camera.transform.right, look.y, Space.World);
        }
    }

    // The object let go by itself (it got stuck behind a wall)
    private void HandleReleased(PhysicsInteractable obj)
    {
        obj.Released -= HandleReleased;
        if (heldObject == obj) heldObject = null;
        if (hand != null) hand.transform.localRotation = Quaternion.identity;
    }

    private void DropObject()
    {
        if (heldObject != null)
        {
            PhysicsInteractable obj = heldObject;
            obj.Released -= HandleReleased;
            heldObject = null;
            obj.StopInteract();
        }
        if (hand != null) hand.transform.localRotation = Quaternion.identity;
    }

    private void OnDisable()
    {
        DropObject();
    }
}