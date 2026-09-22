using UnityEngine;

public class AstronautPlayerScriptVersion2 : MonoBehaviour
{
    //Rigidbody movement and normal rotation, fix for incidental z axis rotation

    [SerializeField] private float moveForce = 10f;
    [SerializeField] private float degreesPerSecond = 60f;
    [SerializeField] private float mouseSens = 4f;

    private float xRotation = 0f;
    private float yRotation = 0f;
    private float zRotation = 0f;

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

    private void Update()
    {
        HandleRotation();
    }

    private void FixedUpdate()
    {
        HandleMove();
    }

    private void HandleRotation()
    {
        xRotation -= inputHandler.lookInput.y * mouseSens;
        yRotation += inputHandler.lookInput.x * mouseSens;
        zRotation += inputHandler.rollInput * degreesPerSecond * Time.deltaTime;

        gameObject.transform.rotation = Quaternion.Euler(xRotation, yRotation, zRotation);
    }

    private void HandleMove()
    {
        Vector3 inputDirection = new Vector3(inputHandler.moveInput.x, inputHandler.moveInput.y, inputHandler.moveInput.z);
        inputDirection.Normalize();

        rBody.AddRelativeForce(inputDirection * moveForce, ForceMode.Force);
    }
}
