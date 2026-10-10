using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;

public class WeepingEnemy : EnemyBase
{
    [SerializeField] private LayerMask playerLayer;
    private PlayerVision playerVision;
    private Transform playerTransform;
    private GameObject player;
    private NavMeshAgent agent;

    bool enemySight;
    bool playerSight;

    void Awake()
    {
        Init();
    }

    void Update()
    {

        MoveEnemy();
        //Debug.Log($"{enemySight} {playerSight}");
    }

    bool EnemyCanSeePlayer()
    {
        //finding the reference points for raycast
        Vector3 origin = agent.transform.position + Vector3.up;
        Vector3 target = playerTransform.position + Vector3.up;
        Vector3 direction = target - origin;
        float distance = direction.magnitude;

        //exclude the enemy's own layer. this took forever to find the raycast kept hitting the enemy's own collider
        int layerMask = ~(1 << gameObject.layer);

        //Debug.DrawRay(origin, direction, Color.red);

        if (Physics.Raycast(origin, direction.normalized, out RaycastHit hit, distance, layerMask, QueryTriggerInteraction.Ignore))
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

    bool PlayerCanSeeEnemy()
    {
        bool playerCanSee = playerVision.CanSeeTarget(transform);
        return playerCanSee;
    }

    void MoveEnemy()
    {
        //setting bools
        enemySight = EnemyCanSeePlayer();
        playerSight = PlayerCanSeeEnemy();

        //if enemy sees you and you dont see enemy
        if (enemySight && !playerSight)
        {
            agent.isStopped = false;
            agent.speed = moveSpeedHolder;
            //Debug.Log(agent.speed);
            agent.SetDestination(playerTransform.position);
        }
        //stops in his place
        else { agent.isStopped = true; }
    }

    void Init()
    {
        player = GameObject.Find("Astronaut Player");
        playerTransform = player.transform;
        playerVision = player.GetComponent<PlayerVision>();
        agent = GetComponent<NavMeshAgent>();
    }
}
