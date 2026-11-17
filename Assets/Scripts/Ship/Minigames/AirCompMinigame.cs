using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class AirCompMinigame : Objective, IMinigame
{
    [SerializeField] private GameObject camera;
    [SerializeField] private GameObject cameraToMinigame;
    [SerializeField] private GameObject cameraToPlayer;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private Transform buttonParent;
    [SerializeField] private GameObject bar;
    [SerializeField] private List<LayoutElement> sections = new List<LayoutElement>();
    [SerializeField] private GameObject playerObjectiveHUD; //Temp

    private bool hudWasActive;

    private List<int> cuts = new List<int>();

    [SerializeField] private int numCorrect;
    private MinigameManager minigameManager;
    private List<int> percentForBar = new List<int>();
    public List<int> correctOrder = new List<int>();

    public List<Color> normalColors = new List<Color>();

    public List<UnityEngine.UI.Button> buttons = new List<UnityEngine.UI.Button>();

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
            cameraToMinigame = GameObject.Find("AirComp Minigame Starter/AirComp Cam Spot");
        }

        if (playerMovement == null)
        {
            GameObject tmp = GameObject.Find("Player");
            playerMovement = tmp.GetComponent<PlayerMovement>();
        }
    }

    public override string GetProgressText() =>
        IsComplete ? "Air composition analysis complete" : "Analyze the air composition";

    public void StartMinigame()
    {
        HideHUD();
        playerMovement.MinigameOnOff(); //turns camera rotation off

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        camera.transform.position = cameraToMinigame.transform.position;
        camera.transform.rotation = cameraToMinigame.transform.rotation;

        bar.SetActive(true);

        numCorrect = 0;
        percentForBar.Clear();
        correctOrder.Clear();

        ResetColors();

        CreateBarValues();

        BarGenerator();
        CreateSortedList();
    }

    void CreateBarValues()
    {
        // Keep trying until an attempt produces no duplicate cuts
        while (!TryCreateBarValues()) { }
    }

    bool TryCreateBarValues()
    {
        cuts.Clear();
        percentForBar.Clear();

        int remaining = 100;
        int currentValue = 0;

        percentForBar.Add(currentValue);

        for (int i = 0; i < 7; i++)
        {
            int cutsLeft = 8 - i;

            int min = 5;
            int max = Mathf.Min(22, remaining - (cutsLeft - 1) * 3);

            int cut = Random.Range(min, max + 1);

            if (cuts.Contains(cut))
            {
                return false; // duplicate, start over
            }

            cuts.Add(cut);

            currentValue += cut;
            remaining -= cut;

            percentForBar.Add(currentValue);
        }

        // The last section is whatever is left, so it needs the same check
        if (cuts.Contains(remaining))
        {
            return false;
        }

        cuts.Add(remaining);
        percentForBar.Add(100);

        return true;
    }

    public void EndMinigame()
    {
        RestoreHUD();
        playerMovement.MinigameOnOff(); //turns camera rotation back on

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        camera.transform.position = cameraToPlayer.transform.position;
        camera.transform.rotation = cameraToPlayer.transform.rotation;

        bar.SetActive(false);
    }

    void CreateSortedList()
    {
        for (int i = 0; i < percentForBar.Count - 1; i++)
        {
            correctOrder.Add(i);
        }

        for (int i = 0; i < correctOrder.Count; i++)
        {
            for (int j = 0; j < correctOrder.Count - 1; j++)
            {
                int diffA = percentForBar[correctOrder[j] + 1] - percentForBar[correctOrder[j]];
                int diffB = percentForBar[correctOrder[j + 1] + 1] - percentForBar[correctOrder[j + 1]];

                if (diffA > diffB)
                {
                    int temp = correctOrder[j];
                    correctOrder[j] = correctOrder[j + 1];
                    correctOrder[j + 1] = temp;
                }
            }
        }

        correctOrder.Reverse();
    }

    void BarGenerator()
    {
        for (int i = 0; i < sections.Count; i++)
        {
            int sectionWidth = percentForBar[i + 1] - percentForBar[i];
            sections[i].flexibleWidth = sectionWidth;
        }
    }

    public void CheckIfCorrect()
    {
        GameObject myButtonGameObject = EventSystem.current.currentSelectedGameObject;
        if (myButtonGameObject == null)
        {
            Debug.Log("Unable to find GameObject.");
            return;
        }

        if (myButtonGameObject.name == correctOrder[numCorrect].ToString())
        {
            Debug.Log("Correct Element");
            buttons[correctOrder[numCorrect]].image.color = Color.green;
            numCorrect++;
        }
        else
        {
            Debug.Log("Wrong Element");
            ResetColors();
            numCorrect = 0;
        }

        if (numCorrect == 8) // WIN
        {
            Debug.Log("Correct Air Composition");
            minigameManager.EndCurrentMinigame();
            Complete();
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

    void ResetColors()
    {
        for (int i = 0; i < buttons.Count; i++)
        {
            buttons[i].image.color = normalColors[i];
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