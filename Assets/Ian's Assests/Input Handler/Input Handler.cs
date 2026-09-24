using System.Net.Mime;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputHandler : MonoBehaviour
{
    [SerializeField] private InputActionAsset playerControls;

    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction rollAction;
    private InputAction reorientAction;

    public Vector3 moveInput { get; private set; }
    public Vector2 lookInput { get; private set; }
    public float rollInput { get; private set; }
    public bool reorientInput { get; private set; }

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

        moveAction = playerControls.FindActionMap("Player").FindAction("Move");
        lookAction = playerControls.FindActionMap("Player").FindAction("Look");
        rollAction = playerControls.FindActionMap("Player").FindAction("Roll");
        reorientAction = playerControls.FindActionMap("Player").FindAction("Reorient");
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
    }

    private void OnEnable()
    {
        moveAction.Enable();
        lookAction.Enable();
        rollAction.Enable();
        reorientAction.Enable();
    }

    private void OnDisable()
    {
        moveAction.Disable();
        lookAction.Disable();
        rollAction.Disable();
        reorientAction.Disable();
    }
}
