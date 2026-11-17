using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PhysicsInteractable : MonoBehaviour, Interactable
{
    [SerializeField] private float followStrength = 15f;
    [SerializeField] private float rotateStrength = 15f;
    [SerializeField] private float maxSpeed = 15f;
    [SerializeField] private float breakDistance = 1.5f;

    public event Action<PhysicsInteractable> Released;

    private Rigidbody rBody;
    private Transform hand;
    private bool gravityBefore;
    private CollisionDetectionMode modeBefore;
    private RigidbodyInterpolation interpBefore;

    private void Awake()
    {
        rBody = GetComponent<Rigidbody>();
    }

    public void Interact(GameObject interactionSource)
    {
        hand = interactionSource.transform;

        gravityBefore = rBody.useGravity;
        modeBefore = rBody.collisionDetectionMode;
        interpBefore = rBody.interpolation;

        rBody.isKinematic = false;
        rBody.detectCollisions = true;
        rBody.useGravity = false;
        rBody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rBody.interpolation = RigidbodyInterpolation.Interpolate;
    }

    public void StopInteract()
    {
        if (hand == null) return;
        hand = null;

        rBody.useGravity = gravityBefore;
        rBody.collisionDetectionMode = modeBefore;
        rBody.interpolation = interpBefore;
        rBody.angularVelocity = Vector3.zero;

        Released?.Invoke(this);
    }

    private void FixedUpdate()
    {
        if (hand == null) return;

        Vector3 toTarget = hand.position - rBody.position;
        if (toTarget.magnitude > breakDistance)
        {
            StopInteract(); //stuck behind a wall, let go
            return;
        }

        rBody.linearVelocity = Vector3.ClampMagnitude(toTarget * followStrength, maxSpeed);

        Quaternion delta = hand.rotation * Quaternion.Inverse(rBody.rotation);
        delta.ToAngleAxis(out float angle, out Vector3 axis);
        if (angle > 180f) angle -= 360f;

        if (Mathf.Abs(angle) > 0.01f && !float.IsInfinity(axis.x))
            rBody.angularVelocity = axis * (angle * Mathf.Deg2Rad * rotateStrength);
        else
            rBody.angularVelocity = Vector3.zero;
    }
}