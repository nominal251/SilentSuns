
using System.Collections;
using UnityEngine;

public class SpookCubeEventSequence : MonoBehaviour, IGameEventSequence
{
    [Header("Cube Event")]
    [SerializeField, Min(0.1f)] private float lifetime = 5f;
    [SerializeField, Min(0.1f)] private float distanceInFront = 3f;
    [SerializeField] private float verticalOffset = 0f;

    private bool hasStarted;

    //Begins the cube event and starts its lifetime
    public void Begin(GameEventContext context)
    {
        if (hasStarted)
        {
            Debug.LogWarning("[SpookCubeEventSequence] Sequence was already started");
            return;
        }

        hasStarted = true;

        Debug.Log("[SpookCubeEventSequence] Sequence started");

        Transform player = null;

        if (context.Source != null)
        player = context.Source;

        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

            if (playerObject != null)
                player = playerObject.transform;
        }

        if (player == null && Camera.main != null)
            player = Camera.main.transform;

        if (player == null)
        {
            Debug.LogError("[SpookCubeEventSequence] EVENT EFFECT FAILED | No player or main camera found");
            Destroy(gameObject);
            return;
        }

        Transform view = Camera.main != null && player.CompareTag("Player")
            ? Camera.main.transform
            : player;

        Vector3 spawnPosition =
            view.position +
            view.forward * distanceInFront +
            view.up * verticalOffset;

        transform.position = spawnPosition;

        Debug.Log($"[SpookCubeEventSequence] Cube positioned | Position: {transform.position} | Distance: {distanceInFront:F2}");

        StartCoroutine(DestroyAfterLifetime());
    }

    //Waits for the configured lifetime and then removes the event object
    private IEnumerator DestroyAfterLifetime()
    {
        Debug.Log($"[SpookCubeEventSequence] Cube lifetime started | Duration: {lifetime:F2}s");

        yield return new WaitForSeconds(lifetime);

        Debug.Log("[SpookCubeEventSequence] Cube lifetime ended | Destroying event object");

        Destroy(gameObject);
    }
}
