using UnityEngine;
using UnityEngine.Rendering;

public class AstronautPlayerScript : MonoBehaviour
{
    [SerializeField] private bool isGravityOn = false;

    [Header("General Parameters")]
    [SerializeField] private float mouseSens = 1f;

    [Header("Zero G Movement Parameters")]
    [SerializeField] private float floatingMoveForce = 10f;
    [SerializeField] private float rollSpeed = 60f;
    [SerializeField] private float reorientSpeed = 30f;

    [Header("Gravity Movement Paramenters")]
    [SerializeField] private float groundedMoveForce = 25f;

    //references
    private Rigidbody rBody;
    private GameObject camera;
    private InputHandler inputHandler;
    
    //internal variables
    private float xRotation = 0f;
    private float yRotation = 0f;
    private float zRotation = 0f;
    private bool debugDeteced = false; //can remove

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
            rBody.freezeRotation = true;
        }
        else
        {
            rBody.useGravity = isGravityOn;
            rBody.freezeRotation = false;
        }
    }

    private void Update()
    {
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
        yRotation += inputHandler.lookInput.x * mouseSens;
        float cameraXRotation = -inputHandler.lookInput.y * mouseSens;
        cameraXRotation = Mathf.Clamp(cameraXRotation, -85f, 85f);

        transform.rotation = Quaternion.Euler(0, yRotation, 0);
        camera.transform.Rotate(cameraXRotation, 0, 0);
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
            if (transform.rotation.z % 180 < 0)
            {
                zRotation += reorientSpeed * Time.deltaTime;
            }
            else if (transform.rotation.z % 180 > 0)
            {
                zRotation -= reorientSpeed * Time.deltaTime;
            }

            if (transform.rotation.x % 180 < 0)
            {
                xRotation += reorientSpeed * Time.deltaTime;
            }
            else if (transform.rotation.x % 180 > 0)
            {
                xRotation -= reorientSpeed * Time.deltaTime;
            }
        }
        else
        {
            transform.rotation = Quaternion.AngleAxis(-inputHandler.lookInput.y * mouseSens, transform.right) * transform.rotation;
            transform.rotation = Quaternion.AngleAxis(inputHandler.lookInput.x * mouseSens, transform.up) * transform.rotation;
            transform.rotation = Quaternion.AngleAxis(inputHandler.rollInput * rollSpeed * Time.deltaTime, transform.forward) * transform.rotation;
        }
    }

    private void HandleMovementNoGravity()
    {
        Vector3 inputDirection = new Vector3(inputHandler.moveInput.x, inputHandler.moveInput.y, inputHandler.moveInput.z);
        inputDirection.Normalize();

        rBody.AddRelativeForce(inputDirection * floatingMoveForce, ForceMode.Force);
    }

    public void SetGravity(bool incomingState)
    {
        if(isGravityOn != incomingState)
        {
            isGravityOn = incomingState;
            if (isGravityOn)
            {
                //To Do: add smooth transition for z axis back to 0
                rBody.useGravity = isGravityOn;
                rBody.freezeRotation = true;

            }
            else
            {
                rBody.useGravity = isGravityOn;
                rBody.freezeRotation = false;

                transform.rotation = Quaternion.Euler(camera.transform.eulerAngles.x, transform.rotation.y, 0);
                camera.transform.localRotation = Quaternion.identity;
            }
        }
    }
}
