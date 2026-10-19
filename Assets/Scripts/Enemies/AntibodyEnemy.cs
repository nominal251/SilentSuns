using UnityEngine;
using UnityEngine.Animations;

public class AntibodyEnemy : EnemyBase
{
    [SerializeField] private float killTimer = 8f;
    [SerializeField] private float rotationSpeed = 2f;
    private float timer = 0f;

    private Transform upperBone;
    private Transform lowerBone;

    private Quaternion upperClosedRotation;
    private Quaternion upperOpenRotation;
    private Quaternion lowerClosedRotation;
    private Quaternion lowerOpenRotation;
    bool enemySight;
    bool enemyOpen = false;


    protected override void Awake()
    {
        base.Awake();

        upperBone = transform.Find("Armature/topbone");
        lowerBone = transform.Find("Armature/bottombone");

        if (upperBone == null) Debug.Log("Upper Null");
        if (lowerBone == null) Debug.Log("Lower Null");
    }

    protected override void Start()
    {
        base.Start();

        upperClosedRotation = upperBone.rotation;
        upperOpenRotation = Quaternion.Euler(0f, 0f, 0f);

        lowerClosedRotation = lowerBone.rotation;
        lowerOpenRotation = Quaternion.Euler(0f, 90f, 0f);
    }

    void FixedUpdate()
    {
        EnemyLogic();
    }
    void EnemyLogic()
    {
        timer = Mathf.Clamp(timer, 0f, killTimer);
        //Debug.Log(timer);
        //Debug.Log(enemyOpen);
        EnemySight();
        if (timer >= killTimer / 2)
        {
            EnemyOpen();
        }
        else if (timer <= killTimer / 2)
        {
            EnemyClose();
        }
        while (enemySight)
        {
            timer += Time.fixedDeltaTime;
            return;
        }

        timer -= Time.fixedDeltaTime;
    }

    void EnemyOpen()
    {
        if (!enemyOpen)
        {
            upperBone.localRotation = Quaternion.RotateTowards(upperBone.localRotation, upperOpenRotation, rotationSpeed * 90f * Time.deltaTime);
            lowerBone.localRotation = Quaternion.RotateTowards(lowerBone.localRotation, lowerOpenRotation, rotationSpeed * 90f * Time.deltaTime);

            if (Quaternion.Angle(upperBone.localRotation, upperOpenRotation) < 0.1f)
            {
                upperBone.localRotation = upperOpenRotation;
                enemyOpen = true;
            }
        }
    }
    void EnemyClose()
    {
        if (enemyOpen)
        {
            //Debug.Log(closedRotation.eulerAngles);
            upperBone.localRotation = Quaternion.RotateTowards(upperBone.localRotation, upperClosedRotation * Quaternion.Euler(90f, 0f, 0f), rotationSpeed * 90f * Time.deltaTime);
            lowerBone.localRotation = Quaternion.RotateTowards(lowerBone.localRotation, lowerClosedRotation, rotationSpeed * 90f * Time.deltaTime);

            if (Quaternion.Angle(upperBone.localRotation, upperClosedRotation * Quaternion.Euler(90f, 0f, 0f)) < 0.1f)
            {
                upperBone.localRotation = upperClosedRotation * Quaternion.Euler(90f, 0f, 0f);
                enemyOpen = false;
            }
        }
    }
    void EnemySight()
    {
        transform.LookAt(playerTransform);
        enemySight = EnemyCanSeePlayer();
    }


}
