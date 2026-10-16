using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;

public class WeepingEnemy : EnemyBase
{
    private PlayerVision playerVision;
    bool enemySight;
    bool playerSight;

    void Update()
    {
        MoveEnemy();
        //Debug.Log($"{enemySight} {playerSight}");
    }

    protected override void Awake()
    {
        base.Awake();

        if (player != null)
        {
            playerVision = player.GetComponent<PlayerVision>();
        }
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
}
