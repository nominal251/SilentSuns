using UnityEngine;
using UnityEngine.AI;

public class ChaseEnemy : EnemyBase
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private NavMeshAgent agent;

    private void Update()
    {
        agent.speed = moveSpeedHolder;
        agent.SetDestination(player.position);
    }
}
