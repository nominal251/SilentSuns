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
        hand = transform.Find("Hand").gameObject;
        handPosition = hand.transform.localPosition.z;
    }
    private void Update()
    {
        // Pause, settings, and the telescope intro must not pick up or move objects.
        if (SilentSuns.Menu.TerminalPause.BlocksGameplayInput || inputHandler == null) return;
        Ray ray = new Ray(camera.transform.position, camera.transform.forward);

        //raycast logic
        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance))
        {
            if(hit.collider.TryGetComponent<PhysicsInteractable>(out PhysicsInteractable script))
            {
                observedObject = script;
            }
            else
            {
                observedObject = null;
            }

        }
        else
        {
            observedObject = null;
        }

        //pickup logic
        if (inputHandler.physicsInteractPressed)
        {
            if (heldObject != null)
            {
                DropObject();
            }
            else if (heldObject == null && observedObject != null)
            {
                heldObject = observedObject;
                heldObject.Interact(hand);
            }
        }

        //manipulation logic
        if (heldObject == null) return;
        // The wheel moves in steps. A held key or controller button moves smoothly over time.
        float reachStep = inputHandler.physicsExtendIsScroll ? scrollSens : scrollSens * 30f * Time.deltaTime;
        handPosition += inputHandler.physicsExtendInput * reachStep;
        handPosition = Mathf.Clamp(handPosition, handMin, handMax);
        hand.transform.localPosition = new Vector3(hand.transform.localPosition.x, hand.transform.localPosition.y, handPosition);
        if(heldObject != null)
        {
            if (inputHandler.physicsRotateInput)
            {
                // Use the same sensitivity and invert setting as player look.
                Vector2 look = inputHandler.lookInput * inputHandler.lookSensitivity;
                hand.transform.rotation = Quaternion.Euler(hand.transform.rotation.eulerAngles.x - look.y, hand.transform.rotation.eulerAngles.y + look.x, hand.transform.rotation.eulerAngles.z);
            }
        }
    }

    private void DropObject()
    {
        if (heldObject != null) heldObject.StopInteract();
        heldObject = null;
        if (hand != null) hand.transform.localRotation = Quaternion.identity;
    }

    private void OnDisable()
    {
        DropObject();
    }
}
