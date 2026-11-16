using UnityEditor.Rendering;
using UnityEngine;

public class StorageMinigame : MonoBehaviour, IMinigame
{
    [SerializeField] private GameObject camera;
    [SerializeField] private GameObject cameraToMinigame;
    [SerializeField] private GameObject cameraToPlayer;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private MinigameManager minigameManager;
    void Awake()
    {
        if (minigameManager == null)
        {
            GameObject tmp = GameObject.Find("Minigame Manager");
            minigameManager = tmp.GetComponent<MinigameManager>();
        }

        if (camera == null)
        {
            GameObject tmp = GameObject.Find("Astronaut Player/Camera Holder/Player Camera");
            camera = tmp;
        }

        if (cameraToPlayer == null)
        {
            GameObject tmp = GameObject.Find("Astronaut Player/Camera Holder");
            cameraToPlayer = tmp;
        }

        if (cameraToMinigame == null)
        {
            GameObject tmp = GameObject.Find("Storage Minigame/Storage Cam Spot");
            cameraToMinigame = tmp;
        }

        if (playerMovement == null)
        {
            GameObject tmp = GameObject.Find("Astronaut Player");
            playerMovement = tmp.GetComponent<PlayerMovement>();
        }
    }

    public void StartMinigame()
    {
        playerMovement.MinigameOnOff(); //turns camera rotation off

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        camera.transform.position = cameraToMinigame.transform.position;
        camera.transform.rotation = cameraToMinigame.transform.rotation;
    }

    public void EndMinigame()
    {
        playerMovement.MinigameOnOff(); //turns camera rotation off

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        camera.transform.position = cameraToPlayer.transform.position;
        camera.transform.rotation = cameraToPlayer.transform.rotation;
    }
    void OnTriggerEnter(Collider collider)
    {
        if (collider.name == "Player Body")
        {
            minigameManager.StartMinigame(this);
        }
    }

    void OnTriggerExit(Collider collider)
    {
        if (collider.name == "Player Body")
        {
            minigameManager.EndCurrentMinigame();
        }
    }
}
