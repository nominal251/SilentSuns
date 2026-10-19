using UnityEngine;
using UnityEngine.Rendering;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private bool isGravityOn = false;

    [Header("General Parameters")]
    [SerializeField] private float mouseSens = 1f;

    [Header("Zero G Movement Parameters")]
    [SerializeField] private float floatingMoveForce = 10f;
    [SerializeField] private float rollSpeed = 60f;
    [SerializeField] private float reorientSpeed = 2;

    [Header("Gravity Movement Paramenters")]
    [SerializeField] private float groundedMoveForce = 25f;
    [SerializeField] private float maxCameraAngle = 85f;

    private Rigidbody rBody;
    private GameObject camera;
    private InputHandler inputHandler;
    
    private float yRotation = 0f;
    private float cameraXRotation = 0f;

    private bool debugDeteced = false; 

    private void Awake()
    {
        rBody = GetComponent<Rigidbody>();
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void Start()
    {
        camera = transform.Find("Player Camera").gameObject;
        inputHandler = InputHandler.instance;

        if (isGravityOn)
        {
            rBody.useGravity = isGravityOn;
            rBody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }
        else
        {
            rBody.useGravity = isGravityOn;
            rBody.constraints = RigidbodyConstraints.None;
        }
    }

    private void Update()
    {
        // Mouse look uses raw input, so it also needs to stop while the menu is open.
        if (SilentSuns.Menu.TerminalPause.BlocksGameplayInput)
            return;

        if (inputHandler.debugInput)
        {
            debugDeteced = true;
        }

        if (isGravityOn)
        {
            HandleRotationGravity();
        }
        else
        {
            HandleRotationNoGravity();
        }
    }

    private void FixedUpdate()
    {
        if (SilentSuns.Menu.TerminalPause.BlocksGameplayInput)
            return;

        if (debugDeteced)
        {
            SetGravity(!isGravityOn);
            debugDeteced = false;
        }

        if (isGravityOn)
        {
            HandleMovementGravity();
        }
        else
        {
            HandleMovementNoGravity();
        }
    }

    private void HandleRotationGravity()
    {
        yRotation += inputHandler.lookInput.x * mouseSens * SilentSuns.Menu.GamePreferences.MouseSensitivity;
        cameraXRotation -= inputHandler.lookInput.y * mouseSens * SilentSuns.Menu.GamePreferences.MouseSensitivity;
        cameraXRotation = Mathf.Clamp(cameraXRotation, -maxCameraAngle, maxCameraAngle);

        transform.rotation = Quaternion.Euler(0, yRotation, 0);
        camera.transform.localRotation = Quaternion.Euler(cameraXRotation, 0, 0);
    }

    private void HandleMovementGravity()
    {
        Vector3 inputDirection = new Vector3(inputHandler.moveInput.x, 0, inputHandler.moveInput.z);
        inputDirection.Normalize();

        rBody.AddRelativeForce(inputDirection * groundedMoveForce, ForceMode.Force);
    }

    private void HandleRotationNoGravity()
    {
        if (inputHandler.reorientInput)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(transform.forward), Time.deltaTime * reorientSpeed);
        }

        transform.rotation = Quaternion.AngleAxis(-inputHandler.lookInput.y * mouseSens * SilentSuns.Menu.GamePreferences.MouseSensitivity, transform.right) * transform.rotation;
        transform.rotation = Quaternion.AngleAxis(inputHandler.lookInput.x * mouseSens * SilentSuns.Menu.GamePreferences.MouseSensitivity, transform.up) * transform.rotation;
        transform.rotation = Quaternion.AngleAxis(inputHandler.rollInput * rollSpeed * Time.deltaTime, transform.forward) * transform.rotation;
    }

    private void HandleMovementNoGravity()
    {
        Vector3 inputDirection = new Vector3(inputHandler.moveInput.x, inputHandler.moveInput.y, inputHandler.moveInput.z);
        inputDirection.Normalize();

        rBody.AddRelativeForce(inputDirection * floatingMoveForce, ForceMode.Force);
    }

    public void SetGravity(bool incomingState) //to do: smooth transition between states
    {
        if(isGravityOn != incomingState)
        {
            isGravityOn = incomingState;
            if (isGravityOn)
            {
                rBody.useGravity = isGravityOn;
                rBody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            }
            else
            {
                rBody.useGravity = isGravityOn;
                rBody.constraints = RigidbodyConstraints.None;
            }
        }
    }
}
