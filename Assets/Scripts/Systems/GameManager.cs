
using System;
using UnityEngine;

[RequireComponent(typeof(ResourceManager))]
public class GameManager : MonoBehaviour
{
    public enum GamePhase
    {
        Boot,                 //Main Menu
        SystemExploration,    //Player is exploring a system; progress is not complete.
        SystemComplete,       //System completed; cryosleep is next
        PlanetaryExploration, //Player is exploring a planet
        LoadingScreen,        //The screen of the loading variety
        Ending                //Whatever the ending is whenever its reached
    }

    [Header("Run Progress")]
    [SerializeField, Min(0)] private int currentSystemNumber = 0;
    [SerializeField, Range(0f, 100f)] private float startingWeirdness = 0f;
    [SerializeField, Min(0f)] private float weirdnessPerCompletedSystem = 5f;
    [SerializeField] private bool resetResourcesAtRunStart = true;
    [SerializeField] private ResourceManager resourceManager;

    public GamePhase Phase { get; private set; } = GamePhase.Boot;
    public int CurrentSystemNumber => currentSystemNumber;
    public float Weirdness { get; private set; }
    public ResourceManager Resources => resourceManager;
    public bool CatalogSubmitted { get; private set; }
    private bool cryosleepPending;

    public event Action RunStarted;
    public event Action<GamePhase> PhaseChanged;
    public event Action<int> SystemLoadRequested;
    public event Action<int> SystemStarted;
    public event Action<int> SystemSubmitted;
    public event Action<int> CryosleepEntered;
    public event Action<float> WeirdnessChanged;

    private void Awake()
    {
        if (resourceManager == null) resourceManager = GetComponent<ResourceManager>();
        Weirdness = Mathf.Clamp(startingWeirdness, 0f, 100f);
    }

    // Returning to the menu ends the run. Resources reset when the next run starts.
    public void ReturnToBoot()
    {
        CatalogSubmitted = false;
        cryosleepPending = false;
        SetPhase(GamePhase.Boot);
    }

    // Call once from a bootstrapper when a new session begins
    public void StartNewRun()
    {
        if (Phase != GamePhase.Boot)
        {
            Debug.LogWarning(
                "StartNewRun ignored because the run has already started.",
                this);
            return;
        }

        currentSystemNumber = 1;
        CatalogSubmitted = false;
        Weirdness = Mathf.Clamp(startingWeirdness, 0f, 100f);
        WeirdnessChanged?.Invoke(Weirdness);

        if (resetResourcesAtRunStart && resourceManager != null)
            resourceManager.ResetForNewRun();

        RunStarted?.Invoke();
        RequestSystemLoad();
    }

    private void RequestSystemLoad()
    {
        SetPhase(GamePhase.LoadingScreen);
        SystemLoadRequested?.Invoke(currentSystemNumber);
    }

    //Call after the system generator finishes loading the system
    public void NotifySystemLoaded()
    {
        if (Phase != GamePhase.LoadingScreen)
            return;

        SetPhase(GamePhase.SystemExploration);
        CatalogSubmitted = false;
        SystemStarted?.Invoke(currentSystemNumber);
    }

    //Call when the player has finished cataloguing the system
    public void MarkCatalogReady()
    {
        if (Phase != GamePhase.SystemExploration)
            return;

        SetPhase(GamePhase.SystemComplete);
    }

    //Call from the master computer's submit action
    public bool SubmitCatalog()
    {
        if (Phase != GamePhase.SystemComplete || CatalogSubmitted)
            return false;

        CatalogSubmitted = true;
        Weirdness = Mathf.Clamp(
            Weirdness + weirdnessPerCompletedSystem,
            0f,
            100f);

        WeirdnessChanged?.Invoke(Weirdness);
        SystemSubmitted?.Invoke(currentSystemNumber);

        return true;
    }

    //Call when the player uses the cryosleep pod
    //The nitrogen cost is paid here
    public bool EnterCryosleep()
    {
        if (Phase != GamePhase.SystemComplete || !CatalogSubmitted || resourceManager == null)
            return false;

        if (!resourceManager.TryConsumeNitrogen(1))
            return false;

        cryosleepPending = true;
        CryosleepEntered?.Invoke(currentSystemNumber);

        //Loading Screen after it's done
        SetPhase(GamePhase.LoadingScreen);

        return true;
    }

    //Call after the cryosleep and any dream sequences finish; Since cryosleep dreams are backlogged
    public void FinishCryosleepTransition()
    {
        if (Phase != GamePhase.LoadingScreen || !cryosleepPending)
            return;

        cryosleepPending = false;
        currentSystemNumber++;
        SystemLoadRequested?.Invoke(currentSystemNumber);
    }

    //Call when the player reaches an ending
    public void SetEnding()
    {
        SetPhase(GamePhase.Ending);
    }

    //Call when the player enters a planet
    public void EnterPlanetaryExploration()
    {
        if (Phase != GamePhase.SystemExploration)
            return;

        SetPhase(GamePhase.PlanetaryExploration);
    }

    //Call when the player returns from a planet to the system
    public void ReturnToSystemExploration()
    {
        if (Phase != GamePhase.PlanetaryExploration)
            return;

        SetPhase(GamePhase.SystemExploration);
    }

    private void SetPhase(GamePhase next)
    {
        if (Phase == next)
            return;

        Phase = next;
        PhaseChanged?.Invoke(Phase);
    }
}
