using UnityEngine;

public class PlayerVision : MonoBehaviour
{
    [Header("Variables")]
    [SerializeField] private float visionAngle = 90f;
    [SerializeField] private LayerMask visionBlockerMask;
    private float visionDistance = Mathf.Infinity;
    private Transform player;


    void Start()
    {
        player = transform;
    }
    public bool CanSeeTarget(Transform target)
    {
        //reference points for raycast
        Vector3 origin = player.position;
        Vector3 targetPosition = target.position + Vector3.up;
        Vector3 direction = targetPosition - origin;

        //if you are too far away, you cannot see..probably need visionDistance to be very high, or to just shoot a ray infinitely
        if (direction.magnitude > visionDistance) { return false; }

        //determining your view angle
        float angle = Vector3.Angle(player.forward, direction.normalized);

        //player is outside of cone of vision
        if (angle > visionAngle / 2f) { return false; }

        if (Physics.Raycast(origin, direction.normalized, direction.magnitude, visionBlockerMask)) { return false; }

        return true;
    }
}
