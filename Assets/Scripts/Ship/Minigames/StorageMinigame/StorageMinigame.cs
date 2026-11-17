using UnityEngine;
using UnityEngine.SceneManagement;

public class StorageMinigame : Objective, IMinigame
{
    [SerializeField] private GameObject camera;
    [SerializeField] private GameObject cameraToMinigame;
    [SerializeField] private GameObject cameraToPlayer;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private MinigameManager minigameManager;
    [SerializeField] private GameObject playerObjectiveHUD; //Temp
    [SerializeField] private GameObject plane;

    private bool hudWasActive;

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

    //Searches every loaded scene
    private static GameObject FindInLoadedScenes(string objectName)
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (!scene.isLoaded) continue;

            foreach (GameObject root in scene.GetRootGameObjects())
                foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                    if (t.name == objectName) return t.gameObject;
        }
        return null;
    }

    private void HideHUD()
    {
        if (playerObjectiveHUD == null)
            playerObjectiveHUD = FindInLoadedScenes("SurveyMinimap(Clone)");

        if (playerObjectiveHUD == null) return;

        hudWasActive = playerObjectiveHUD.activeSelf;
        playerObjectiveHUD.SetActive(false);
    }

    private void RestoreHUD()
    {
        if (playerObjectiveHUD != null && hudWasActive)
            playerObjectiveHUD.SetActive(true);
        hudWasActive = false;
    }

    public override string GetProgressText() =>
        IsComplete ? "Storage organized" : "Organize the storage";

    public void StartMinigame()
    {
        plane.SetActive(true);
        HideHUD();

        playerMovement.MinigameOnOff(); //turns camera rotation off

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        camera.transform.position = cameraToMinigame.transform.position;
        camera.transform.rotation = cameraToMinigame.transform.rotation;
    }

    public void EndMinigame()
    {
        plane.SetActive(false);
        RestoreHUD();

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