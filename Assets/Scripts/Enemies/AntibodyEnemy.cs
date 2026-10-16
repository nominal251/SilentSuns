using UnityEngine;
using UnityEngine.Animations;

public class AntibodyEnemy : EnemyBase
{
    [SerializeField] private float killTimer = 8f;
    private float timer = 0f;
    bool enemySight;

    void FixedUpdate()
    {
        EnemyLogic();
    }
    void EnemyLogic()
    {
        timer = Mathf.Clamp(timer, 0f, killTimer);
        Debug.Log(timer);
        EnemySight();
        while (enemySight)
        {
            timer += Time.fixedDeltaTime;
            return;
        }

        timer -= Time.fixedDeltaTime;
    }
    void EnemySight()
    {
        transform.LookAt(playerTransform);
        enemySight = EnemyCanSeePlayer();
    }
}
