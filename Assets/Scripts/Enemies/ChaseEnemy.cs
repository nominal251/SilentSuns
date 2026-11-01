using UnityEngine;
using UnityEngine.AI;

public class ChaseEnemy : EnemyBase
{
    private void Update()
    {
        agent.speed = moveSpeedHolder;
        agent.SetDestination(playerTransform.position);
    }
}
