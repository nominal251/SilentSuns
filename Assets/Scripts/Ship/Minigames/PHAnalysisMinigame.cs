using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PHAnalysisMinigame : Objective, IMinigame
{
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private GameObject phBar;
    [SerializeField] private Image colorImage;
    [SerializeField] private GameObject camera;
    [SerializeField] private GameObject cameraToMinigame;
    [SerializeField] private GameObject cameraToPlayer;
    private MinigameManager minigameManager;

    private int color = 0;

    void Awake()
    {
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
            cameraToMinigame = GameObject.Find("pH Analysis Minigame Starter/pH Cam Spot");
        }

        if (playerMovement == null)
        {
            GameObject tmp = GameObject.Find("Player");
            playerMovement = tmp.GetComponent<PlayerMovement>();
        }
    }

    public override string GetProgressText() =>
        IsComplete ? "pH analysis complete" : "Analyze the pH sample";

    public void StartMinigame()
    {
        playerMovement.MinigameOnOff(); //turns camera rotation off
        camera.transform.position = cameraToMinigame.transform.position;
        camera.transform.rotation = cameraToMinigame.transform.rotation;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        phBar.SetActive(true);
        color = Random.Range(0, 14);
        DisplayColor();
    }

    public void EndMinigame()
    {
        playerMovement.MinigameOnOff(); //turns camera rotation back on
        camera.transform.position = cameraToPlayer.transform.position;
        camera.transform.rotation = cameraToPlayer.transform.rotation;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        phBar.SetActive(false);
    }

    public void CheckIfCorrect()
    {
        GameObject myButtonGameObject = EventSystem.current.currentSelectedGameObject;
        if (myButtonGameObject == null)
        {
            Debug.Log("Unable to find GameObject.");
            return;
        }

        if (myButtonGameObject.name == color.ToString()) // WIN
        {
            Debug.Log("Winner");
            phBar.SetActive(false);
            Complete();
        }
        else
        {
            Debug.Log("Loser");
        }
    }

    void DisplayColor()
    {
        switch (color)
        {
            case 0:  SetColor("#C21006"); break;
            case 1:  SetColor("#EE2C23"); break;
            case 2:  SetColor("#F54D40"); break;
            case 3:  SetColor("#FC6023"); break;
            case 4:  SetColor("#FFD10F"); break;
            case 5:  SetColor("#FAE704"); break;
            case 6:  SetColor("#74CC22"); break;
            case 7:  SetColor("#41A044"); break;
            case 8:  SetColor("#11E47B"); break;
            case 9:  SetColor("#48C1F8"); break;
            case 10: SetColor("#0285D7"); break;
            case 11: SetColor("#0854A8"); break;
            case 12: SetColor("#4904CF"); break;
            case 13: SetColor("#790AA7"); break;
            case 14: SetColor("#400579"); break;
        }
    }

    private void SetColor(string hexCode)
    {
        if (ColorUtility.TryParseHtmlString(hexCode, out Color newColor))
        {
            colorImage.color = newColor;
        }
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
}