using System.Collections;
using UnityEngine;
using UnityEngine.AI;
public class EnemyBase : MonoBehaviour
{
    [Header("Layer Mask")]
    [Tooltip("These are objects that call for a hit on the raycast, setting our environment and player in this layerMask is how it functions")]
    [SerializeField] protected LayerMask layerMask;

    [Header("Enemy Stats")]
    //[SerializeField] protected float health = 10f; probably wont use this? we will see
    [SerializeField] protected float moveSpeed = 3f;

    //so you can set the movespeed in the inspector and it wont bug out swapping speeds
    protected float moveSpeedHolder;

    protected bool stunned = false;
    protected Transform playerTransform;
    protected GameObject player;
    protected NavMeshAgent agent;

    protected virtual void Awake()
    {
        Init();
    }
    protected virtual void Start()
    {
        moveSpeedHolder = moveSpeed;
    }

    protected virtual bool EnemyCanSeePlayer()
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (agent == null || !FindPlayer()) return false;

        Vector3 origin = agent.transform.position + Vector3.up;
        Vector3 target = playerTransform.position + Vector3.up * 0.9f;
        Vector3 direction = target - origin;

        if (Physics.Raycast(origin, direction.normalized, out RaycastHit hit, direction.magnitude + 0.5f,
                layerMask, QueryTriggerInteraction.Ignore))
            return hit.transform.IsChildOf(playerTransform);

        return false;
    }

    protected virtual void Stun()
    {
        if (!stunned)
        {
            moveSpeedHolder = moveSpeed;
            return;
        }
        else { moveSpeedHolder = 0; }
    }

    void Init()
    {
        player = GameObject.FindWithTag("Player");  //finds the player gameobject for tranfrom

        if (player != null)
        {
            playerTransform = player.transform;
        }

        agent = GetComponent<NavMeshAgent>(); //finds the navmeshagent
    }

    protected bool FindPlayer()
    {
        if (playerTransform != null) return true;

        // Prefer the player that actually has PlayerMovement, in case another object shares the tag.
        var movement = FindFirstObjectByType<PlayerMovement>();
        player = movement != null ? movement.gameObject : GameObject.FindWithTag("Player");
        if (player == null) return false;

        playerTransform = player.transform;
        return true;
    }
}
