using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    public bool RotationLock = true;

    public float moveForce = 10f;
    public float verticalForce = 10f;
    public float rollTorque = 5f;
    public float mouseSens = 1f;

    private Rigidbody rb;

    private float LYaw;
    private float LPitch;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        if (RotationLock == false)
        {
            UnlockedMouseLook();
        }
        else
        {
            LockedMouseLook();
        }
    }

    void LockedMouseLook()
    {
        Vector2 mouseDelta = Mouse.current.delta.ReadValue();

        LYaw += mouseDelta.x * mouseSens;
        LPitch -= mouseDelta.y * mouseSens;

        LPitch = Mathf.Clamp(LPitch, -90f, 90f);

        transform.rotation = Quaternion.Euler(LPitch, LYaw, 0f);
    }

    void UnlockedMouseLook()
    {
        Vector2 mouseDelta = Mouse.current.delta.ReadValue();

        float yaw = mouseDelta.x * mouseSens;
        float pitch = -mouseDelta.y * mouseSens;

        transform.Rotate(pitch, yaw, 0f, Space.Self);
    }

    void FixedUpdate()
    {
        // forward / backward
        if (Keyboard.current.wKey.isPressed)
            rb.AddForce(transform.forward * moveForce, ForceMode.Force);

        if (Keyboard.current.sKey.isPressed)
            rb.AddForce(-transform.forward * moveForce, ForceMode.Force);

        // left / right
        if (Keyboard.current.aKey.isPressed)
            rb.AddForce(-transform.right * moveForce, ForceMode.Force);

        if (Keyboard.current.dKey.isPressed)
            rb.AddForce(transform.right * moveForce, ForceMode.Force);

        // up / down
        if (Keyboard.current.spaceKey.isPressed)
            rb.AddForce(transform.up * verticalForce, ForceMode.Force);

        if (Keyboard.current.cKey.isPressed)
            rb.AddForce(-transform.up * verticalForce, ForceMode.Force);

        // roll (if rotation lock is off)
        if (Keyboard.current.qKey.isPressed && RotationLock == false)
            rb.AddRelativeTorque(Vector3.forward * rollTorque, ForceMode.Force);

        if (Keyboard.current.eKey.isPressed && RotationLock == false)
            rb.AddRelativeTorque(-Vector3.forward * rollTorque, ForceMode.Force);
    }
}