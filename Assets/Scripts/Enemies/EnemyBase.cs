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
        //finding the reference points for raycast
        Vector3 origin = agent.transform.position + Vector3.up;
        Vector3 target = playerTransform.position + Vector3.up;
        Vector3 direction = target - origin;
        float distance = direction.magnitude;

        //Debug.DrawRay(origin, direction, Color.red);

        if (Physics.Raycast(origin, direction.normalized, out RaycastHit hit, Mathf.Infinity, layerMask, QueryTriggerInteraction.Ignore))
        {

            if (hit.transform.IsChildOf(playerTransform))
            {
                //Debug.Log("enemy sees");
                return true;
            }

            // A wall or another object blocks the view.
            return false;
        }
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
        player = GameObject.Find("Astronaut Player");

        if (player != null)
        {
            playerTransform = player.transform;
        }

        agent = GetComponent<NavMeshAgent>();
    }
}
