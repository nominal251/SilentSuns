using UnityEngine;
using System.Collections;
public class EnemyBase : MonoBehaviour
{
    [Header("Enemy Stats")]
    //[SerializeField] protected float health = 10f; probably wont use this? we will see
    [SerializeField] protected float moveSpeed = 3f;

    protected float moveSpeedHolder;

    protected bool stunned = false;

    void Start()
    {
        moveSpeedHolder = moveSpeed;
    }

    public virtual void Stun()
    {
        if (!stunned)
        {
            moveSpeedHolder = moveSpeed;
            return;
        }
        else { moveSpeedHolder = 0; }
    }
    void OnCollision(Collider other)
    {
        if (other.name == "Player")
        {

        }
    }
}
