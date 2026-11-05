using Unity.VisualScripting;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public float health { get; private set; } = 100f;
    //need reference for vignettes

    public void TakeDamage(float damageTaken)
    {
        health -= damageTaken;
        UpdateVignette();
    }

    public void HealDamage(float damageHealed)
    {
        health += damageHealed;
        if(health > 100)
        {
            health = 100;
        }
        UpdateVignette();
    }

    private void UpdateVignette()
    {

    }
}
