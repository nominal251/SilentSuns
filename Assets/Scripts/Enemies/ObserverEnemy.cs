using System.Reflection.Metadata.Ecma335;
using Unity.VisualScripting;
using UnityEngine;

public class ObserverEnemy : EnemyBase
{
    [Header("Grasp Settings")]
    [SerializeField] private float raiseDistance = 5f;
    [SerializeField] private float raiseTime = 2.0f;
    [Header("Stun Settings")]
    [SerializeField] private float stunTimer = 3f;

    private Vector3 startPos;
    private Vector3 targetPosition;
    private bool enemySight;
    private bool playerSight;
    private bool isMoving = false;
    private PlayerVision playerVision;
    private Rigidbody playerRb;

    private InputHandler inputHandler;


    private float timer = 0f; //timer for stun


    private float elapsedTimer = 0f; //timer for raise movement


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

            if (timer >= stunTimer) //if the creature has waited long enough after being stunned, it can start it's states again
            {
                Logic();
                Move();

                timer = 0f;
            }

            return; //if not, return
        }
        Logic();
        Move();

    }

    void FixedUpdate()
    {
        if (enemyState == EnemyState.Grasp && isMoving)
        {
            elapsedTimer += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTimer / raiseTime); //clamping so we dont fly into oblivion

            t = Mathf.SmoothStep(0f, 1f, t); //smoothstep starts and ends slower, so it smooths in and out and gives it a little more life

            Vector3 newPosition = Vector3.Lerp(startPos, targetPosition, t);
            playerRb.MovePosition(newPosition);

            if (t >= 1f)
            {
                isMoving = false;//stops once its done
            }
        }
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

        //Debug.Log($"{enemySight} + {playerSight}");

        if (enemyState == EnemyState.Grasp) //if you are in the grasp state, nothing else occurs unless you stun the enemy
        {
            if (inputHandler.toolInput) { enemyState = EnemyState.Stunned; }
        }

        else
        {
            if (enemySight && playerSight) //the enemy moves slower if you are both looking at each other
            {
                enemyState = EnemyState.Stalk;
            }

            else //otherwise, the enemy moves full speed
            {
                enemyState = EnemyState.Hunt;
            }

            if (DistanceToPlayer() <= 5) //if at any point you are close enough, you are grasped and we exit out of this block
            {
                enemyState = EnemyState.Grasp;
                ResetGrasp();

                //Debug.Log("Grasp");
                return;
            }
        }
    }

    void Move()
    {
        switch (enemyState)
        {
            case EnemyState.Stalk: //half reg speed
                agent.isStopped = false;
                agent.speed = moveSpeedHolder / 2;
                agent.SetDestination(playerTransform.position);
                break;

            case EnemyState.Hunt: //reg speed
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
        playerRb.isKinematic = true; //turns gravity off for player
        inputHandler.DisableMovement();
    }

    private void UnGraspPlayer()
    {
        playerRb.isKinematic = false; //turns gravity on for player
        inputHandler.EnableMovement();
    }
    private void ResetGrasp() //reset the transforms and timers for the grasp
    {
        startPos = playerRb.position;
        targetPosition = playerRb.position + new Vector3(0f, playerRb.position.y + raiseDistance, 0f);
        isMoving = true;
        elapsedTimer = 0f;
    }
}
