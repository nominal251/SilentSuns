using UnityEngine;
using UnityEngine.AI;

public class ChaseEnemy : EnemyBase
{
    private Transform playerTransform;
    private GameObject player;
    private NavMeshAgent agent;

    void Awake()
    {
        player = GameObject.Find("Astronaut Player");
        playerTransform = player.transform;
        agent = GetComponent<NavMeshAgent>();
    }
    private void Update()
    {
        agent.speed = moveSpeedHolder;
        agent.SetDestination(playerTransform.position);
    }
}
