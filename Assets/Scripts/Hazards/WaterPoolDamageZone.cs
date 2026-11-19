using UnityEngine;

public class WaterPoolDamageZone : MonoBehaviour
{
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private float numDamageOverTime;
    [SerializeField] private bool inPool;
    [SerializeField] private float timer;

    void Awake()
    {
        playerHealth = GameObject.FindWithTag("Player").GetComponent<PlayerHealth>(); //find player health
    }

    void Update()
    {
        if (inPool)
        {
            timer += Time.deltaTime;
            if (timer >= 1f) //if you are in the pool, every second you take damage and the timer resets
            {
                playerHealth.TakeDamage(numDamageOverTime);
                timer = 0f;
            }
        }

        else //if you leave the pool timer set back to zero
        {
            if (timer == 0f) { return; }
            timer = 0f;
        }
    }
    void OnTriggerEnter(Collider other) //when you enter, it sets inPool to tru
    {
        if (other.name == "Player Body")
        {
            inPool = true;
        }
    }

    void OnTriggerExit(Collider other) //exit sets false
    {
        if (other.name == "Player Body")
        {
            inPool = false;
        }
    }
}
