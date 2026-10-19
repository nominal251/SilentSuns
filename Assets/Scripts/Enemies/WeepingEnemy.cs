using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;

public class WeepingEnemy : EnemyBase
{
    private PlayerVision playerVision;
    bool enemySight;
    bool playerSight;

    private EnemyState enemyState;

    private enum EnemyState
    {
        Stalk,
        Hunt
    }

    void Update()
    {
        Logic();
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

    void Logic()
    {
        //setting bools
        enemySight = EnemyCanSeePlayer(); //eventSystem boolean auditorEncountered can go after this line
        //this is always running in update, so you can do an if(enemySight == true) check and when that line runs
        //you can set run the auditorEncountered method to change the boolean

        //if(enemySight) { AuditorEncountered();}

        playerSight = PlayerCanSeeEnemy();

        //if enemy sees you and you dont see enemy
        if (enemySight && !playerSight)
        {
            enemyState = EnemyState.Hunt;
        }
        //any other combination of sight will not allow it to move
        else { enemyState = EnemyState.Stalk; }
    }

    void MoveEnemy()
    {
        switch (enemyState)
        {
            case EnemyState.Hunt:
                agent.isStopped = false;
                agent.speed = moveSpeedHolder;
                //Debug.Log(agent.speed);
                agent.SetDestination(playerTransform.position);
                break;

            case EnemyState.Stalk:
                agent.isStopped = true;
                break;
        }
    }

    //void AuditorEncountered()
    //{
    //    if(auditorEncountered) { return; }

    //    eventMangaer.AuditorEncountered();
    //    auditorEncountered = true;         this can be a boolean in this script that will stop us from continuously calling the eventManager method
    //}
}
