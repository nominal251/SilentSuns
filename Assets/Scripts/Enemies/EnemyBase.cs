using UnityEngine;
using System.Collections;
public class EnemyBase : MonoBehaviour
{
    [Header("Enemy Stats")]
    //[SerializeField] protected float health = 10f; probably wont use this? we will see
    [SerializeField] protected float moveSpeed = 3f;

    //so you can set the movespeed in the inspector and it wont bug out swapping speeds
    protected float moveSpeedHolder;

    protected bool stunned = false;

    protected virtual void Start()
    {
        moveSpeedHolder = moveSpeed;
    }

    protected virtual void Stun()
    {
        if (!stunned)
        {
            moveSpeedHolder = moveSpeed;
            return;
        }
        else { moveSpeedHolder = 0; }
    }
}
