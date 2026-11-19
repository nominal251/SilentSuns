using UnityEngine;

public class AreaHeater : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BodyTemp playerTempScript;

    [Header("Heater Parameters")]
    [SerializeField] private float cooldown = 5;
    [SerializeField] private int heaterIntensity = 2;

    private float currentCooldown;
    private bool playerInArea;

    private void Awake()
    {
        currentCooldown = cooldown;
    }

    void Start()
    {
        if (playerTempScript == null)
            playerTempScript = GameObject.FindGameObjectWithTag("Player").GetComponent<BodyTemp>();
    }

    void Update()
    {
        if (playerInArea)
        {
            currentCooldown -= Time.deltaTime;
            if (currentCooldown > 0)
            {
                playerTempScript.changeColdStacks(-heaterIntensity);
                currentCooldown += cooldown;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.tag == "Player")
        {
            playerInArea = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.tag == "Player")
        {
            playerInArea = false;
        }
        currentCooldown = cooldown
    }
}
