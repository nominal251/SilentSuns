using UnityEngine;

public class AstronautPlayerScriptVersion1 : MonoBehaviour
{
    //Rigidbody movement and rotation, no fix for incidental z axis rotation

    [SerializeField] private float moveForce = 10f;
    [SerializeField] private float rollTorque = 5f;
    [SerializeField] private float mouseSens = 1f;

    private Rigidbody rBody;
    private InputHandler inputHandler;

    private void Awake()
    {
        rBody = GetComponent<Rigidbody>();
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void Start()
    {
        inputHandler = InputHandler.instance;
    }

    private void FixedUpdate()
    {
        HandleMove();
        HandleRotation();
    }

    private void HandleRotation()
    {
        if (inputHandler.reorientInput)
        {
            var springTorque = Vector3.Cross(rBody.transform.up, Vector3.up);
            var dampTorque = -rBody.angularVelocity;
            rBody.AddTorque(springTorque + dampTorque, ForceMode.Acceleration);
        }
        else
        {
            Vector3 rollDirection = new Vector3(-inputHandler.lookInput.y * mouseSens, inputHandler.lookInput.x * mouseSens, inputHandler.rollInput);
            rBody.AddRelativeTorque(rollDirection * rollTorque, ForceMode.Force);
        }
    }

    private void HandleMove()
    {
        Vector3 inputDirection = new Vector3(inputHandler.moveInput.x, inputHandler.moveInput.y, inputHandler.moveInput.z);
        inputDirection.Normalize();

        rBody.AddRelativeForce(inputDirection * moveForce, ForceMode.Force);
    }
}
