using System.Net.Mime;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputHandler : MonoBehaviour
{
    [SerializeField] private InputActionAsset playerControls;

    private InputAction debugAction;
    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction rollAction;
    private InputAction reorientAction;
    private InputAction sprintAction;
    private InputAction interactAction;
    private InputAction physicsInteractAction;
    private InputAction physicsRotateAction;
    private InputAction physicsExtendAction;

    public bool debugInput { get; private set; }
    public bool debugPressed => debugAction.WasPerformedThisFrame();
    public Vector3 moveInput { get; private set; }
    public Vector2 lookInput { get; private set; }
    public float rollInput { get; private set; }
    public bool reorientInput { get; private set; }
    public bool sprintInput { get; private set; }
    public bool interactInput { get; private set; }
    public bool interactPressed => interactAction.WasPerformedThisFrame();
    public bool physicsInteractInput { get; private set; }
    public bool physicsInteractPressed => physicsInteractAction.WasPerformedThisFrame();
    public bool physicsRotateInput { get; private set; }
    public float physicsExtendInput { get; private set; }


    public static InputHandler instance { get; private set; }

    private void Awake()
    {
        if(instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        debugAction = playerControls.FindActionMap("Player").FindAction("Debug");
        moveAction = playerControls.FindActionMap("Player").FindAction("Move");
        lookAction = playerControls.FindActionMap("Player").FindAction("Look");
        rollAction = playerControls.FindActionMap("Player").FindAction("Roll");
        reorientAction = playerControls.FindActionMap("Player").FindAction("Reorient");
        sprintAction = playerControls.FindActionMap("Player").FindAction("Sprint");
        interactAction = playerControls.FindActionMap("Player").FindAction("Interact");
        physicsInteractAction = playerControls.FindActionMap("Player").FindAction("Physics Interact");
        physicsRotateAction = playerControls.FindActionMap("Player").FindAction("Physics Rotate");
        physicsExtendAction = playerControls.FindActionMap("Player").FindAction("Physics Extend");
        RegisterInputActions();
    }

    private void RegisterInputActions()
    {
        moveAction.performed += context => moveInput = context.ReadValue<Vector3>();
        moveAction.canceled += context => moveInput = Vector3.zero;

        lookAction.performed += context => lookInput = context.ReadValue<Vector2>();
        lookAction.canceled += context => lookInput = Vector2.zero;

        rollAction.performed += context => rollInput = context.ReadValue<float>();
        rollAction.canceled += context => rollInput = 0;

        reorientAction.performed += context => reorientInput = true;
        reorientAction.canceled += context => reorientInput = false;

        sprintAction.performed += context => sprintInput = true;
        sprintAction.canceled += context => sprintInput = false;

        interactAction.performed += context => interactInput = true;
        interactAction.canceled += context => interactInput = false;

        physicsInteractAction.performed += context => physicsInteractInput = true;
        physicsInteractAction.canceled += context => physicsInteractInput = false;

        physicsRotateAction.performed += context => physicsRotateInput = true;
        physicsRotateAction.canceled += context => physicsRotateInput = false;

        debugAction.performed += context => debugInput = true;
        debugAction.canceled += context => debugInput = false;

        physicsExtendAction.performed += context => physicsExtendInput = context.ReadValue<float>();
        physicsExtendAction.canceled += context => physicsExtendInput = 0;
    }

    private void OnEnable()
    {
        debugAction.Enable();
        moveAction.Enable();
        lookAction.Enable();
        rollAction.Enable();
        reorientAction.Enable();
        sprintAction.Enable();
        interactAction.Enable();
        physicsInteractAction.Enable();
        physicsRotateAction.Enable();
        physicsExtendAction.Enable();
    }

    private void OnDisable()
    {
        debugAction.Disable();
        moveAction.Disable();
        lookAction.Disable();
        rollAction.Disable();
        reorientAction.Disable();
        sprintAction.Disable();
        interactAction.Disable();
        physicsInteractAction.Disable();
        physicsRotateAction.Disable();
        physicsExtendAction.Disable();
    }
}
