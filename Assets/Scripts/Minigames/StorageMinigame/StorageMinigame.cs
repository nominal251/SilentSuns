using UnityEditor.Rendering;
using UnityEngine;

public class StorageMinigame : Objective, IMinigame
{
    [SerializeField] private SurfaceObjectiveTracker surfaceObjectiveTracker; // SURFACE OBJECTIVE TRACKER CODE
    [SerializeField] private GameObject camera;
    [SerializeField] private GameObject cameraToMinigame;
    [SerializeField] private GameObject cameraToPlayer;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private MinigameManager minigameManager;

    [SerializeField] private GameObject plane;

    private bool success = false;
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
            GameObject tmp = GameObject.Find("Astronaut Player/Player Camera");
            camera = tmp;
        }

        if (cameraToPlayer == null)
        {
            GameObject tmp = GameObject.Find("Astronaut Player");
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

        if (surfaceObjectiveTracker == null) // SURFACE OBJECTIVE TRACKER CODE
        {
            GameObject tmp = GameObject.Find("SurfaceObjectiveTracker"); // SURFACE OBJECTIVE TRACKER CODE 
            surfaceObjectiveTracker = tmp.GetComponent<SurfaceObjectiveTracker>(); // SURFACE OBJECTIVE TRACKER CODE
        }
    }

    public override string GetProgressText()
    {
        return success ? "pH analysis complete" : "Analyze the pH sample";
    }
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

    public void CompletePuzzle()
    {
        base.Complete();
        surfaceObjectiveTracker.CompleteObj(); // SURFACE OBJECTIVE TRACKER CODE
    }
}
