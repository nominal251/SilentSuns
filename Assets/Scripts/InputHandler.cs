using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-50)]
public class InputHandler : MonoBehaviour
{
    [SerializeField] private InputActionAsset playerControls;

    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction rollAction;
    private InputAction reorientAction;
    private InputAction debugAction;
    private InputAction sprintAction;
    private InputAction interactAction;
    private InputAction physicsInteractAction;
    private InputAction physicsRotateAction;
    private InputAction physicsExtendAction;

    public Vector3 moveInput { get; private set; }
    public Vector2 lookInput { get; private set; }
    private bool controllerLook;
    public float lookSensitivity => controllerLook ? SilentSuns.Menu.GamePreferences.ControllerSensitivity : SilentSuns.Menu.GamePreferences.MouseSensitivity;
    public float rollInput { get; private set; }
    public bool reorientInput { get; private set; }
    public bool sprintInput { get; private set; }
    public bool debugInput { get; private set; }
    public bool debugPressed => debugAction.WasPerformedThisFrame();
    // Both interaction scripts read these actions from the same copy that Settings edits.
    public bool interactInput => interactAction.IsPressed();
    public bool interactPressed => interactAction.WasPressedThisFrame();
    public bool interactReleased => interactAction.WasReleasedThisFrame();
    public bool physicsInteractInput => physicsInteractAction.IsPressed();
    public bool physicsInteractPressed => physicsInteractAction.WasPressedThisFrame();
    public bool physicsRotateInput => physicsRotateAction.IsPressed();
    public float physicsExtendInput => physicsExtendAction.ReadValue<float>();
    public bool physicsExtendIsScroll => physicsExtendAction.activeControl?.path.Contains("/scroll/") == true;

    public static InputHandler instance { get; private set; }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Settings edits this runtime copy and saves its binding overrides.
        playerControls = SilentSuns.Menu.TerminalBindings.Initialize(playerControls);
        moveAction = playerControls.FindActionMap("Player").FindAction("Move");
        lookAction = playerControls.FindActionMap("Player").FindAction("Look");
        rollAction = playerControls.FindActionMap("Player").FindAction("Roll");
        reorientAction = playerControls.FindActionMap("Player").FindAction("Reorient");
        sprintAction = playerControls.FindActionMap("Player").FindAction("Sprint");
        debugAction = playerControls.FindActionMap("Player").FindAction("Debug");
        interactAction = playerControls.FindAction("Interact");
        physicsInteractAction = playerControls.FindAction("Physics Interact");
        physicsRotateAction = playerControls.FindAction("Physics Rotate");
        physicsExtendAction = playerControls.FindAction("Physics Extend");
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
    }

    private void Update()
    {
        debugInput = debugAction.triggered;
        // Apply controller settings here; mouse delta keeps its existing action scaling.
        Vector2 controller = Gamepad.current != null ? Gamepad.current.rightStick.ReadUnprocessedValue() : Vector2.zero;
        float deadZone = SilentSuns.Menu.GamePreferences.DeadZone;
        moveInput = moveAction.ReadValue<Vector3>();
        if (moveAction.activeControl?.device is Gamepad movePad &&
            moveAction.activeControl.path.Contains("/leftStick/") && movePad.leftStick.ReadUnprocessedValue().magnitude <= deadZone)
            moveInput = new Vector3(0, moveInput.y, 0);
        if (controller.magnitude > deadZone)
        {
            controllerLook = true;
            controller = controller.normalized * Mathf.InverseLerp(deadZone, 1, controller.magnitude);
            lookInput = controller * (100f * Time.deltaTime);
        }
        else { controllerLook = false; lookInput = lookAction.ReadValue<Vector2>(); }
        if (SilentSuns.Menu.GamePreferences.InvertLook) lookInput = new Vector2(lookInput.x, -lookInput.y);
    }

    private void OnEnable()
    {
        if (instance != this)
            return;

        moveAction.Enable();
        lookAction.Enable();
        rollAction.Enable();
        reorientAction.Enable();
        sprintAction.Enable();
        debugAction.Enable();
        playerControls.FindAction("ControllerLook").Enable();
        interactAction.Enable();
        physicsInteractAction.Enable();
        physicsRotateAction.Enable();
        physicsExtendAction.Enable();
    }

    private void OnDisable()
    {
        // A duplicate is destroyed before its actions are assigned.
        moveAction?.Disable();
        lookAction?.Disable();
        rollAction?.Disable();
        reorientAction?.Disable();
        sprintAction?.Disable();
        debugAction?.Disable();
        if (instance == this) playerControls?.Disable();
    }
}
