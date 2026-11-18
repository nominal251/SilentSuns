using UnityEngine;

public class BodyTemp : MonoBehaviour
{
    [Header("Parameters")]
    [SerializeField] private float slowPerStack;
    [SerializeField] private int regenThreshold = 2;
    [SerializeField] private float regenPerSecond = 5;
    [SerializeField] private int damageThreshold = 6;
    [SerializeField] private float coldDamagePerSecond = 5;
    [SerializeField] private int deathThreshold = 20;

    private PlayerHealth healthScript;
    private PlayerMovementOnPlanet movementScript;
    private int coldStacks;
    private float coldDamageTaken;

    private void Awake()
    {
        healthScript = gameObject.GetComponent<PlayerHealth>();
        movementScript = gameObject.GetComponent<PlayerMovementOnPlanet>();
    }

    private void Update()
    {
        if (coldStacks >= deathThreshold)
        {
            //die
        }
        else if (coldStacks >= damageThreshold)
        {
            healthScript.TakeDamage(coldDamagePerSecond * Time.deltaTime);
            coldDamageTaken += (coldDamagePerSecond * Time.deltaTime);
        }
        else if (coldStacks <= regenThreshold)
        {
            if(coldDamageTaken != 0)
            {
                if(regenPerSecond * Time.deltaTime > coldDamageTaken)
                {
                    healthScript.HealDamage(coldDamageTaken);
                    coldDamageTaken = 0;
                }
                else
                {
                    healthScript.HealDamage(regenPerSecond * Time.deltaTime);
                    coldDamageTaken -= regenPerSecond * Time.deltaTime;
                }
            }
        }
    }

    public void changeColdStacks(int change)
    {
        //cold sources like wind will add stacks, heat sources will reduce stacks
        coldStacks += change;
        movementScript.updateSpeedDebuff(coldStacks * slowPerStack);
    }
}
