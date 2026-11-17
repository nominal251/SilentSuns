using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovementOnPlanet : MonoBehaviour
{
    public bool crouching { get; private set; }

    //Visible Internal Variables
    [Header("Look Parameters")]
    [SerializeField] private float mouseSens = 1f;
    [SerializeField] private float maxCameraAngle = 85f;

    [Header("Movement Parameters")]
    [SerializeField] private float moveForce = 25f;
    [SerializeField] private float crouchMultiplier = .5f;
    [SerializeField] private float sprintMultiplier = 2f;

    [Header("Jump Parameters")]
    [SerializeField] private float jumpHeight = 1.2f;
    [SerializeField] private float jumpCooldown = 0.2f;
    [SerializeField] private float groundCheckDistance = 0.15f;
    [SerializeField] private LayerMask groundMask = ~0;

    [Header("Crouch Parameters")]
    [SerializeField] private float crouchingHeight = 1.2f;
    [SerializeField] private float standingHeight = 2f;
    [SerializeField] private float crouchTransitionDuration = 1f;

    [Header("Stamina Parameters")]
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float staminaConsumptionRate = 10f;
    [SerializeField] private float staminaRegenRate = 10f;
    [SerializeField] private float staminaRegenCooldown = 1f;
    [SerializeField] private float staminaExhaustionCooldown = 3.5f;

    //Internal References
    private Rigidbody rBody;
    private CapsuleCollider bodyCollider;
    private Transform cameraTransform;
    private PhysicsInteractor physicsInteractor;

    //External References
    private InputHandler inputHandler;

    //Hidden Internal Variables
    private float yRotation = 0f;
    private float cameraXRotation = 0f;
    private float crouchLerpDuration = 0f;
    private float currentStamina;
    private float currentRegenCooldown;
    private float currentExhaustionCooldown;
    private bool canRun = true;
    private float jumpPressedTime = -10f;
    private float lastJumpTime = -10f;

    private void Awake()
    {
        rBody = GetComponent<Rigidbody>();
        rBody.useGravity = true;
        rBody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        bodyCollider = GetComponentInChildren<CapsuleCollider>();
        physicsInteractor = GetComponent<PhysicsInteractor>();

        currentStamina = maxStamina;

        Cursor.lockState = CursorLockMode.Locked;
    }

    private void Start()
    {
        cameraTransform = transform.Find("Player Camera");
        inputHandler = InputHandler.instance;
    }

    private void Update()
    {
        if (inputHandler.jumpPressed)
        {
            jumpPressedTime = Time.time;
        }
        crouching = inputHandler.crouchInput;

        ManageStamina();
        //if you are rotating object, don't move the camera
        if (physicsInteractor != null && physicsInteractor.IsRotatingObject) return;
        HandleRotation();
    }

    private void FixedUpdate()
    {
        HandleMovement();
        HandleCrouch();
        HandleJump();
    }

    private void ManageStamina()
    {
        if (canRun && inputHandler.sprintInput && (inputHandler.moveInput.magnitude > 0))
        {
            currentStamina -= staminaConsumptionRate * Time.deltaTime;
            currentRegenCooldown = staminaRegenCooldown;
        }
        else if (currentRegenCooldown < 0 && !(currentStamina == maxStamina))
        {
            currentStamina += staminaRegenRate * Time.deltaTime;
            if(currentStamina > maxStamina)
            {
                currentStamina = maxStamina;
            }
        }

        if (currentStamina < 0)
        {
            currentStamina = 0;
            canRun = false;
            currentExhaustionCooldown = staminaExhaustionCooldown;
        }
        else if (currentExhaustionCooldown < 0 && !canRun)
        {
            canRun = true;
        }

        currentExhaustionCooldown -= Time.deltaTime;
        currentRegenCooldown -= Time.deltaTime;
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

        float force = (inputHandler.sprintInput && canRun) ? moveForce * sprintMultiplier : crouching ? moveForce * crouchMultiplier : moveForce;
        rBody.AddRelativeForce(inputDirection * force, ForceMode.Force);
    }

    private void HandleCrouch()
    {
        float lerpPercentage = crouchLerpDuration / crouchTransitionDuration;
        cameraTransform.localPosition = Vector3.Lerp(new Vector3(0, standingHeight/4, 0), new Vector3(0, crouchingHeight/4, 0), lerpPercentage);
        bodyCollider.height = Mathf.Lerp(standingHeight, crouchingHeight, lerpPercentage);

        if (crouching)
        {
            crouchLerpDuration = Mathf.Clamp(crouchLerpDuration + Time.deltaTime, 0f, crouchTransitionDuration);
        }
        else
        {
            crouchLerpDuration = Mathf.Clamp(crouchLerpDuration - Time.deltaTime, 0f, crouchTransitionDuration);
        }
    }

    private void HandleJump()
    {
        if (Time.time - jumpPressedTime > 0.15f) return;
        if (Time.time - lastJumpTime < jumpCooldown) return;
        if (!IsGrounded()) return;

        float jumpSpeed = Mathf.Sqrt(2f * Mathf.Abs(Physics.gravity.y) * jumpHeight);
        rBody.AddForce(Vector3.up * jumpSpeed, ForceMode.VelocityChange);

        lastJumpTime = Time.time;
        jumpPressedTime = -10f;
    }

    private bool IsGrounded()
    {
        if (bodyCollider == null) return true;

        Bounds bounds = bodyCollider.bounds;
        float radius = Mathf.Min(bounds.extents.x, bounds.extents.z) * 0.9f;
        float distance = bounds.extents.y - radius + groundCheckDistance;
        return Physics.SphereCast(bounds.center, radius, Vector3.down, out _, distance,
            groundMask, QueryTriggerInteraction.Ignore);
    }
}