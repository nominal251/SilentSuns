using Unity.VisualScripting;
using UnityEngine;

public class GeyserDamageZone : MonoBehaviour
{
    private PlayerHealth playerHealth;

    [SerializeField] private float numDamageOverTime = 5f;
    [SerializeField] private float timer = 0f;
    [SerializeField] private float blowTimer = 0f;
    [SerializeField] private float dotTimer = 0f;

    [SerializeField] private int tmp;

    [SerializeField] private bool isBlowing;
    [SerializeField] private bool inPool;

    void Awake()
    {
        playerHealth = GameObject.FindWithTag("Player").GetComponent<PlayerHealth>(); //find player health
    }
    private void Update()
    {
        Logic();
    }

    private void Logic()
    {
        if (!isBlowing) //if geyser is not blowing, run random checks to see if it will blow
        {
            dotTimer = 0f;
            timer += Time.deltaTime; //timer only runs while geyser is not blowing
            if (timer >= 1f)
            {
                timer = 0f;

                tmp = Random.Range(0, 12);

                if (tmp == 3) //if the random number is hit, the timer stops and the geyser blows
                {
                    isBlowing = true;
                }
            }
        }

        else
        {
            blowTimer += Time.deltaTime;
            if (inPool)
            {
                dotTimer += Time.deltaTime;
                if (dotTimer >= 0.75f) //if you are in pool and dotTimer ticks you take damage, dottimer only ticks while you are in pool so you must be in pool
                                       //for this much time to take damage
                {
                    playerHealth.TakeDamage(numDamageOverTime); //take damage
                    dotTimer = 0f;
                }
            }

            if (blowTimer >= 4f) //if geyser has blown for 4s, reset and start random check again
            {
                isBlowing = false;
                blowTimer = 0f;
            }
        }
    }

    void OnTriggerEnter(Collider other) //checks if you are in geyser
    {
        if (other.name == "Player Body")
        {
            inPool = true;
        }
    }

    void OnTriggerExit(Collider other) //checks if you leave geyser
    {
        if (other.name == "Player Body")
        {
            inPool = false;
        }
    }
}
