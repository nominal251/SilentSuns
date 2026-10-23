using System.Reflection.Metadata.Ecma335;
using Unity.VisualScripting;
using UnityEngine;

public class ObserverEnemy : EnemyBase
{
    [SerializeField] private float unitsToMove = 5f;
    private bool enemySight;
    private bool playerSight;
    private PlayerVision playerVision;
    private Rigidbody playerRb;

    private InputHandler inputHandler;

    private float stunTimer = 3f;
    private float timer = 0f;

    private EnemyState enemyState;
    private enum EnemyState
    {
        Stunned, //not moving
        Stalk, //slow moving
        Hunt, //normal move speed
        Grasp //stopped and holding you in the air
    }

    protected override void Awake()
    {
        base.Awake();

        if (player != null)
        {
            playerVision = player.GetComponent<PlayerVision>();
            playerRb = player.GetComponent<Rigidbody>();
        }

        if (inputHandler == null)
        {
            GameObject tmp = GameObject.Find("Input Handler");
            inputHandler = tmp.GetComponent<InputHandler>();
        }
    }

    void Update()
    {
        //Debug.Log(enemyState);

        while (enemyState == EnemyState.Stunned)
        {
            timer += Time.deltaTime;

            if (timer >= stunTimer)
            {
                Logic();
                Move();

                timer = 0f;
            }

            return;
        }
        Logic();
        Move();

    }

    bool PlayerCanSeeEnemy()
    {
        bool playerCanSee = playerVision.CanSeeTarget(transform);
        return playerCanSee;
    }

    void Logic()
    {
        enemySight = EnemyCanSeePlayer();
        playerSight = PlayerCanSeeEnemy();

        if (enemyState == EnemyState.Grasp && inputHandler.toolInput)
        {
            //Debug.Log("Hit");
            enemyState = EnemyState.Stunned;
        }

        else if (!enemySight) { enemyState = EnemyState.Hunt; }

        else if (enemySight && !playerSight)
        {
            if (DistanceToPlayer() <= 5)
            {
                enemyState = EnemyState.Grasp;
                //playerRb.position += Vector3.up * unitsToMove;
                return;
            }
            enemyState = EnemyState.Hunt;
        }

        else if (enemySight && playerSight)
        {
            if (DistanceToPlayer() <= 5)
            {
                enemyState = EnemyState.Grasp;
                //playerRb.position += Vector3.up * unitsToMove;
                return;
            }
            enemyState = EnemyState.Stalk;
        }
    }

    void Move()
    {
        switch (enemyState)
        {
            case EnemyState.Stalk:
                agent.isStopped = false;
                agent.speed = moveSpeedHolder / 2;
                agent.SetDestination(playerTransform.position);
                break;

            case EnemyState.Hunt:
                agent.isStopped = false;
                agent.speed = moveSpeedHolder;
                agent.SetDestination(playerTransform.position);
                break;

            case EnemyState.Grasp:
                agent.isStopped = true;
                GraspPlayer();
                break;

            case EnemyState.Stunned:
                agent.isStopped = true;
                UnGraspPlayer();
                break;
        }
    }
    private float DistanceToPlayer()
    {
        //my goat the distance formula
        float distance = Vector3.Distance(agent.transform.position, player.transform.position);
        return distance;
    }

    private void GraspPlayer()
    {
        playerRb.isKinematic = true;
    }

    private void UnGraspPlayer()
    {
        playerRb.isKinematic = false;
    }

    private bool StunTimer() //returns true if active, false if inactive
    {
        timer += Time.deltaTime;

        if (timer >= stunTimer) { return true; }
        else { return false; }
    }
}
