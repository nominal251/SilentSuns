using UnityEngine;

public class StorageMinigame : Objective, IMinigame
{
    [SerializeField] private GameObject camera;
    [SerializeField] private GameObject cameraToMinigame;
    [SerializeField] private GameObject cameraToPlayer;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private MinigameManager minigameManager;

    [SerializeField] private GameObject plane;

    void Awake()
    {
        plane.SetActive(false);

        if (minigameManager == null)
        {
            GameObject tmp = GameObject.Find("Minigame Manager");
            minigameManager = tmp.GetComponent<MinigameManager>();
        }

        if (camera == null)
        {
            camera = GameObject.Find("Player Camera");
        }

        if (cameraToPlayer == null)
        {
            cameraToPlayer = GameObject.Find("Player");
        }

        if (cameraToMinigame == null)
        {
            cameraToMinigame = GameObject.Find("Storage Minigame/Storage Cam Spot");
        }

        if (playerMovement == null)
        {
            GameObject tmp = GameObject.Find("Player");
            playerMovement = tmp.GetComponent<PlayerMovement>();
        }
    }

    public override string GetProgressText() =>
        IsComplete ? "Storage organized" : "Organize the storage";

    public void StartMinigame()
    {
        plane.SetActive(true);

        playerMovement.MinigameOnOff(); //turns camera rotation off

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        camera.transform.position = cameraToMinigame.transform.position;
        camera.transform.rotation = cameraToMinigame.transform.rotation;
    }

    public void EndMinigame()
    {
        plane.SetActive(false);

        playerMovement.MinigameOnOff(); //turns camera rotation back on

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        camera.transform.position = cameraToPlayer.transform.position;
        camera.transform.rotation = cameraToPlayer.transform.rotation;
    }

    void OnTriggerEnter(Collider collider)
    {
        if (collider.name != "Player Body") return;
        if (IsComplete || !IsRunning) return;
        minigameManager.StartMinigame(this);
    }

    void OnTriggerExit(Collider collider)
    {
        if (collider.name != "Player Body") return;
        minigameManager.EndCurrentMinigame();
    }

    public void CompletePuzzle()
    {
        Complete();
    }
}