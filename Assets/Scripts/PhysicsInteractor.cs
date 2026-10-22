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

    private void Start()
    {
        inputHandler = InputHandler.instance;
        camera = transform.Find("Player Camera").gameObject;
        hand = transform.Find("Hand").gameObject;
        handPosition = hand.transform.localPosition.z;
    }
    private void Update()
    {
        Ray ray = new Ray(transform.position, camera.transform.forward);

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
                heldObject.StopInteract();
                heldObject = null;
                hand.transform.rotation = Quaternion.Euler(0, 0, 0);
            }
            else if (heldObject == null && observedObject != null)
            {
                heldObject = observedObject;
                heldObject.Interact(hand);
            }
        }

        //manipulation logic
        handPosition += (inputHandler.physicsExtendInput * scrollSens);
        handPosition = Mathf.Clamp(handPosition, handMin, handMax);
        hand.transform.localPosition = new Vector3(hand.transform.localPosition.x, hand.transform.localPosition.y, handPosition);
        if(heldObject != null)
        {
            if (inputHandler.physicsRotateInput)
            {
                hand.transform.rotation = Quaternion.Euler(hand.transform.rotation.eulerAngles.x - inputHandler.lookInput.y, hand.transform.rotation.eulerAngles.y + inputHandler.lookInput.x, hand.transform.rotation.eulerAngles.z);
            }
        }
    }
}
