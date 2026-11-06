using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using SilentSuns.Menu;

// Keeps the ship screen and buttons connected while ObjectiveSystem tracks the survey.
public class ObjectiveTracker : MonoBehaviour
{
    public Planet planet1;
    public Planet planet2;
    public Planet planet3;
    public Planet planet4;
    public TextMeshPro objText;
    [HideInInspector] public bool allObjectivesComplete;
    public Button cryoButton;
    public Button debugButton;
    public string victoryScene;
    public BlinkyLight blinkyLight;
    [SerializeField] private ObjectiveSystem objectiveSystem;

    private readonly PlanetSurveyObjective[] surveys = new PlanetSurveyObjective[4];
    private GameManager game;
    private bool debugComplete;
    private bool leaving;

    private void Start()
    {
        objectiveSystem.SetSuspended(true);
        Planet[] planets = { planet1, planet2, planet3, planet4 };
        for (int i = 0; i < planets.Length; i++)
        {
            // Keep objectives on their planets so future map markers use the right positions.
            surveys[i] = planets[i].gameObject.AddComponent<PlanetSurveyObjective>();
            surveys[i].SetPlanet(planets[i], i + 1);
            objectiveSystem.Register(surveys[i]);
        }
        objectiveSystem.AllObjectivesCompleted += SurveyComplete;
        game = TerminalGameSession.Manager;
        if (game != null) game.RunStarted += ResetSurvey;
        RefreshText();
    }

    private void Update()
    {
        bool playing = game == null || game.Phase == GameManager.GamePhase.SystemExploration ||
            game.Phase == GameManager.GamePhase.SystemComplete || game.Phase == GameManager.GamePhase.PlanetaryExploration;
        objectiveSystem.SetSuspended(!playing || TerminalPause.BlocksGameplayInput);
        if (objectiveSystem.Suspended || leaving) return;

        if (debugButton != null && debugButton.activated) debugComplete = true;
        foreach (var survey in surveys)
            if (survey != null) survey.CheckScans();

        allObjectivesComplete = debugComplete || (objectiveSystem.Objectives.Count == 4 && objectiveSystem.AllComplete);
        RefreshText();
        if (!allObjectivesComplete) return;

        // The debug button can unlock cryosleep, but does not earn the completion flag.
        SurveyComplete();
        blinkyLight.blink = true;
        if (cryoButton == null || !cryoButton.activated) return;
        if (game != null)
        {
            game.SubmitCatalog();
            if (!game.EnterCryosleep()) return;
            game.SetEnding();
        }
        leaving = true;
        if (!debugComplete) GameProgress.MarkCompleted();
        SceneManager.LoadScene(victoryScene);
    }

    private void SurveyComplete()
    {
        // Danny's manager handles catalog submission and the nitrogen cost after this.
        if (objectiveSystem.Objectives.Count == 4 && (objectiveSystem.AllComplete || debugComplete))
            game?.MarkCatalogReady();
    }

    private void ResetSurvey()
    {
        objectiveSystem.SetSuspended(true);
        objectiveSystem.ResetObjectives();
        debugComplete = false;
        leaving = false;
        allObjectivesComplete = false;
        blinkyLight.blink = false;
        if (cryoButton != null) cryoButton.activated = false;
        if (debugButton != null) debugButton.activated = false;
        RefreshText();
    }

    private void RefreshText()
    {
        string[] lines = new string[4];
        for (int i = 0; i < surveys.Length; i++)
            lines[i] = surveys[i] != null ? surveys[i].GetProgressText() : "PLANET " + (i + 1) + " NOT LOCATED";
        objText.text = "Bodies Surveyed:\n" + string.Join("\n", lines);
    }

    private void OnDestroy()
    {
        if (game != null) game.RunStarted -= ResetSurvey;
        if (objectiveSystem != null) objectiveSystem.AllObjectivesCompleted -= SurveyComplete;
    }
}
