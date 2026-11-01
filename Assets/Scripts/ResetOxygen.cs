using UnityEngine;

public class ResetOxygen : MonoBehaviour
{
    // Find the player from the collider, so names and other trigger visitors do not matter.
    private void OnTriggerStay(Collider collision)
    {
        var player = collision.GetComponentInParent<PlayerMovement>();
        if (player == null || !player.CanUseSuitOxygen) return;
        player.SetOxygenStation(transform);
        player.ResetDistance();
    }
}
