using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovementOnPlanet : MonoBehaviour
{
    [Header("Look")]
    [SerializeField] private float mouseSens = 1f;
    [SerializeField] private float maxCameraAngle = 85f;

    [Header("Movement")]
    [SerializeField] private float moveForce = 25f;
    [SerializeField] private float sprintMultiplier = 2f;
    [SerializeField, Min(0f)] private float drag = 5f;

    private Rigidbody rBody;
    private Transform cameraTransform;
    private InputHandler inputHandler;

    private float yRotation = 0f;
    private float cameraXRotation = 0f;

    private void Awake()
    {
        rBody = GetComponent<Rigidbody>();
        rBody.useGravity = true;
        rBody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rBody.linearDamping = 0f;

        Cursor.lockState = CursorLockMode.Locked;
    }

    private void Start()
    {
        cameraTransform = transform.Find("Player Camera");
        inputHandler = InputHandler.instance;
    }

    private void Update()
    {
        HandleRotation();
    }

    private void FixedUpdate()
    {
        HandleMovement();
        ApplyHorizontalDrag();
    }

    private void ApplyHorizontalDrag()
    {
        Vector3 velocity = rBody.linearVelocity;
        float damping = Mathf.Clamp01(1f - drag * Time.fixedDeltaTime);

        // Scale only X and Z so gravity and jumps are unaffected.
        rBody.linearVelocity = new Vector3(velocity.x * damping, velocity.y, velocity.z * damping);
    }

    private void HandleRotation()
    {
        yRotation += inputHandler.lookInput.x * mouseSens;
        cameraXRotation -= inputHandler.lookInput.y * mouseSens;
        cameraXRotation = Mathf.Clamp(cameraXRotation, -maxCameraAngle, maxCameraAngle);

        transform.rotation = Quaternion.Euler(0f, yRotation, 0f);
        cameraTransform.localRotation = Quaternion.Euler(cameraXRotation, 0f, 0f);
    }

    private void HandleMovement()
    {
        Vector3 inputDirection = new Vector3(inputHandler.moveInput.x, 0f, inputHandler.moveInput.z);
        inputDirection.Normalize();

        float force = inputHandler.sprintInput ? moveForce * sprintMultiplier : moveForce;
        rBody.AddRelativeForce(inputDirection * force, ForceMode.Force);
    }
}