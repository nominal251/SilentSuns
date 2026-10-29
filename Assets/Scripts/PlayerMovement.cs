using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SocialPlatforms.Impl;

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
    [SerializeField] private float sprintMultiplier = 2f;
    [SerializeField] private float maxCameraAngle = 85f;

    private Rigidbody rBody;
    private GameObject camera;
    private InputHandler inputHandler;
    private PhysicsInteractor physicsInteractor;

    private float yRotation = 0f;
    private float cameraXRotation = 0f;

    private bool debugDeteced = false;
    [Header("Oxygen Parameters")]
    [SerializeField, Min(0)] private float oxygenRegenRate = 10f;
    [SerializeField, Min(1)] private float maxTotalDistance = 900f;
    private Transform currentOxygenStation;
    private Vector3 previousPosition;
    private float totalDistanceTraveled;
    private bool wasInsideCircle = true;
    private GameManager game;
    private ResourceManager resources;

    public bool IsOnFoot => isGravityOn;
    public Transform CurrentOxygenStation => currentOxygenStation;
    // Keep the distance units from Drew's system, using Danny's resource as the source of truth.
    private float SecondsPerUnit => resources != null ? resources.OxygenCapacity / maxTotalDistance : 1f;
    public float OxygenRemaining => resources != null
        ? Mathf.Clamp01(resources.OxygenSeconds / Mathf.Max(.001f, resources.OxygenCapacity)) * maxTotalDistance
        : Mathf.Max(0, maxTotalDistance - totalDistanceTraveled);
    public float SafeRadius => OxygenRemaining;
    public bool CanUseSuitOxygen => isGravityOn && !SilentSuns.Menu.TerminalPause.BlocksGameplayInput &&
        (game == null || game.Phase == GameManager.GamePhase.PlanetaryExploration ||
         game.Phase == GameManager.GamePhase.SystemExploration || game.Phase == GameManager.GamePhase.SystemComplete);


    private void Awake()
    {
        rBody = GetComponent<Rigidbody>();
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void Start()
    {
        camera = transform.Find("Player Camera").gameObject;
        inputHandler = InputHandler.instance;
        physicsInteractor = GetComponent<PhysicsInteractor>();
        game = SilentSuns.Menu.TerminalGameSession.Manager;
        resources = game != null ? game.Resources : null;
        if (game != null) game.RunStarted += ResetOxygenForNewRun;
        previousPosition = rBody.position;

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
        {
            previousPosition = rBody.position;
            return;
        }

        if (inputHandler.debugPressed)
        {
            debugDeteced = true;
        }

        // While turning a held object, look input belongs to the hand instead of the camera.
        if (physicsInteractor != null && physicsInteractor.IsRotatingObject) return;

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
        TrackDistance();
        CheckSafeCircle();
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
        yRotation += inputHandler.lookInput.x * mouseSens * inputHandler.lookSensitivity;
        cameraXRotation -= inputHandler.lookInput.y * mouseSens * inputHandler.lookSensitivity;
        cameraXRotation = Mathf.Clamp(cameraXRotation, -maxCameraAngle, maxCameraAngle);

        transform.rotation = Quaternion.Euler(0, yRotation, 0);
        camera.transform.localRotation = Quaternion.Euler(cameraXRotation, 0, 0);
    }

    private void HandleMovementGravity()
    {
        Vector3 inputDirection = new Vector3(inputHandler.moveInput.x, 0, inputHandler.moveInput.z);
        inputDirection.Normalize();

        if (inputHandler.sprintInput)
        {
            rBody.AddRelativeForce(inputDirection * groundedMoveForce * sprintMultiplier, ForceMode.Force);
        }
        else
        {
            rBody.AddRelativeForce(inputDirection * groundedMoveForce, ForceMode.Force);
        }
    }

    private void HandleRotationNoGravity()
    {
        if (inputHandler.reorientInput)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(transform.forward), Time.deltaTime * reorientSpeed);
        }

        transform.rotation = Quaternion.AngleAxis(-inputHandler.lookInput.y * mouseSens * inputHandler.lookSensitivity, transform.right) * transform.rotation;
        transform.rotation = Quaternion.AngleAxis(inputHandler.lookInput.x * mouseSens * inputHandler.lookSensitivity, transform.up) * transform.rotation;
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
        if (isGravityOn != incomingState)
        {
            isGravityOn = incomingState;
            previousPosition = rBody.position;
            if (!isGravityOn) currentOxygenStation = null;
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

    private void TrackDistance()
    {
        float distanceMoved = Vector3.Distance(previousPosition, rBody.position);
        previousPosition = rBody.position;
        if (!CanUseSuitOxygen) return;
        if (resources != null)
            resources.DrainOxygen(distanceMoved * SecondsPerUnit);
        else
        {
            totalDistanceTraveled = Mathf.Clamp(totalDistanceTraveled + distanceMoved, 0, maxTotalDistance);
            if (OxygenRemaining <= 0) SilentSuns.Menu.TerminalFailure.Begin("GameOverScene");
        }
    }

    // The station calls this once per physics step while the player is inside its trigger.
    public void ResetDistance()
    {
        if (!CanUseSuitOxygen || OxygenRemaining <= 0) return;
        float amount = oxygenRegenRate * Time.fixedDeltaTime;
        if (resources != null)
            resources.AddOxygen(Mathf.Min(amount * SecondsPerUnit, Mathf.Max(0, resources.OxygenCapacity - resources.OxygenSeconds)));
        else
            totalDistanceTraveled = Mathf.Max(0, totalDistanceTraveled - amount);
    }

    public void SetOxygenStation(Transform station)
    {
        if (!CanUseSuitOxygen || currentOxygenStation == station) return;
        currentOxygenStation = station;
        wasInsideCircle = true;
    }

    private void CheckSafeCircle()
    {
        if (!CanUseSuitOxygen || currentOxygenStation == null) return;
        Vector3 offset = rBody.position - currentOxygenStation.position;
        bool inside = new Vector2(offset.x, offset.z).magnitude <= SafeRadius;
        if (!inside && wasInsideCircle) Debug.LogWarning("Outside oxygen return range.");
        wasInsideCircle = inside;
    }

    private void ResetOxygenForNewRun()
    {
        totalDistanceTraveled = 0;
        currentOxygenStation = null;
        wasInsideCircle = true;
        previousPosition = rBody.position;
    }

    private void OnDestroy()
    {
        if (game != null) game.RunStarted -= ResetOxygenForNewRun;
    }
}
