using UnityEngine;

public class PlayerMovementInSpace : MonoBehaviour
{
    [Header("Look Parameters")]
    [SerializeField] private float mouseSens = 1f;

    [Header("Zero G Movement Parameters")]
    [SerializeField] private float floatingMoveForce = 10f;
    [SerializeField] private float rollSpeed = 60f;
    [SerializeField] private float reorientSpeed = 2;
    [SerializeField, Min(0f)] private float drag = 5f;

    //Internal References
    private Rigidbody rBody;
    private PhysicsInteractor physicsInteractor;

    //External References
    private InputHandler inputHandler;

    private void Start()
    {
        inputHandler = InputHandler.instance;
        physicsInteractor = GetComponent<PhysicsInteractor>();
    }

    private void Update()
    {
        //if rotating object don't move the camera
        if (physicsInteractor != null && physicsInteractor.IsRotatingObject) return;
        HandleRotation();
    }

    private void FixedUpdate()
    {
        HandleMovement();
    }

    private void HandleRotation()
    {
        if (inputHandler.reorientInput)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(transform.forward), Time.deltaTime * reorientSpeed);
        }

        transform.rotation = Quaternion.AngleAxis(-inputHandler.lookInput.y * mouseSens * inputHandler.lookSensitivity, transform.right) * transform.rotation;
        transform.rotation = Quaternion.AngleAxis(inputHandler.lookInput.x * mouseSens * inputHandler.lookSensitivity, transform.up) * transform.rotation;
        transform.rotation = Quaternion.AngleAxis(inputHandler.rollInput * rollSpeed * Time.deltaTime, transform.forward) * transform.rotation;
    }

    private void HandleMovement()
    {
        Vector3 inputDirection = new Vector3(inputHandler.moveInput.x, inputHandler.moveInput.y, inputHandler.moveInput.z);
        inputDirection.Normalize();

        rBody.AddRelativeForce(inputDirection * floatingMoveForce, ForceMode.Force);
    }
}
