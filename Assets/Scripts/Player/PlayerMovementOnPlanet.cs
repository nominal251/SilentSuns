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

    [Header("Stamina")]
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float staminaConsumptionRate = 10f;
    [SerializeField] private float staminaRegenRate = 10f;
    [SerializeField] private float staminaRegenCooldown = 1f;
    [SerializeField] private float staminaExhaustionCooldown = 3.5f;

    private Rigidbody rBody;
    private Transform cameraTransform;
    private InputHandler inputHandler;

    private float yRotation = 0f;
    private float cameraXRotation = 0f;

    [Header("Debug")]
    [SerializeField] private float currentStamina;
    [SerializeField] private float currentRegenCooldown;
    [SerializeField] private float currentExhaustionCooldown;
    [SerializeField] private bool canRun = true;

    private void Awake()
    {
        rBody = GetComponent<Rigidbody>();
        rBody.useGravity = true;
        rBody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rBody.linearDamping = 0f;

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
        HandleRotation();
        ManageStamina();
    }

    private void FixedUpdate()
    {
        HandleMovement();
        ApplyHorizontalDrag();
    }

    private void HandleRotation()
    {
        yRotation += inputHandler.lookInput.x * mouseSens;
        cameraXRotation -= inputHandler.lookInput.y * mouseSens;
        cameraXRotation = Mathf.Clamp(cameraXRotation, -maxCameraAngle, maxCameraAngle);

        transform.rotation = Quaternion.Euler(0f, yRotation, 0f);
        cameraTransform.localRotation = Quaternion.Euler(cameraXRotation, 0f, 0f);
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

    private void HandleMovement()
    {
        Vector3 inputDirection = new Vector3(inputHandler.moveInput.x, 0f, inputHandler.moveInput.z);
        inputDirection.Normalize();

        float force = (inputHandler.sprintInput && canRun) ? moveForce * sprintMultiplier : moveForce;
        rBody.AddRelativeForce(inputDirection * force, ForceMode.Force);
    }

    private void ApplyHorizontalDrag()
    {
        Vector3 velocity = rBody.linearVelocity;
        float damping = Mathf.Clamp01(1f - drag * Time.fixedDeltaTime);

        // Scale only X and Z so gravity and jumps are unaffected.
        rBody.linearVelocity = new Vector3(velocity.x * damping, velocity.y, velocity.z * damping);
    }
}