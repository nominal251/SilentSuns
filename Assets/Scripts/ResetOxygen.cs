using UnityEngine;

public class ResetOxygen : MonoBehaviour
{
    private PlayerMovement playerMovement;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        GameObject tmp = GameObject.Find("Player");
        playerMovement = tmp.GetComponent<PlayerMovement>();
    }

    void OnTriggerStay(Collider collision)
    {
        //Debug.Log(collision.gameObject.name);
        if (collision.gameObject.name == "Player Body")
        {
            playerMovement.SetOxygenStation(transform);
            playerMovement.ResetDistance();
        }
    }

}
