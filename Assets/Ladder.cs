using UnityEngine;

public class Ladder : MonoBehaviour, Interactable
{
    //Climb positions
    public Transform bottomPoint;
    public Transform topPoint;
    public Transform topExitPoint;
    //Where player is on the ladder
    public float standOffset = 0.5f;

    // Ladder's transform.forward must point toward the player.
    public Vector3 ClimbPosition => transform.position + transform.forward * standOffset;

    public void Interact(GameObject interactionSource)
    {
        var climber = interactionSource.GetComponentInParent<LadderClimber>();
        if (climber == null) return;

        if (!climber.IsClimbing) climber.Mount(this);
    }

    public void StopInteract() { }

    void OnDrawGizmosSelected()
    {
        if (bottomPoint == null || topPoint == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(bottomPoint.position, topPoint.position);
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(ClimbPosition, 0.1f);
        if (topExitPoint != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(topExitPoint.position, 0.15f);
        }
    }
}