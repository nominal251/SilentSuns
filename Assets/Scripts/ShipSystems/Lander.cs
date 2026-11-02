using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using TMPro;

public class Lander : MonoBehaviour
{
    public Button landerButton;
    public PlanetManager planetManager;
    public TextMeshPro statusText;
    public LanderSurface surfaceLander;

    [Header("Launch")]
    [SerializeField] private bool requireObjectivesToReturn = true;
    [SerializeField, Min(0f)] private float launchDelay = 3f;

    [Header("Events")]
    public UnityEvent onLanderLand;
    public UnityEvent onObjectivesComplete;
    public UnityEvent onLaunchStarted;
    public UnityEvent onLanderReturn;

    private ObjectiveSystem surfaceObjectives;
    private bool objectivesComplete;
    private bool launching;

    private void Awake()
    {
        SceneSwap.SurfaceEntered += HandleSurfaceEntered;
        SceneSwap.ReturnedToShip += HandleReturnedToShip;

        if (landerButton != null)
            landerButton.onInteracted.AddListener(HandleButtonPress);
    }

    private void OnDestroy()
    {
        SceneSwap.SurfaceEntered -= HandleSurfaceEntered;
        SceneSwap.ReturnedToShip -= HandleReturnedToShip;
        if (landerButton != null)
            landerButton.onInteracted.RemoveListener(HandleButtonPress);
        UnhookObjectives();
    }

    void Update()
    {
        RefreshText();
    }

    private void HandleButtonPress()
    {
        Debug.Log($"Lander: button pressed (idle={SceneSwap.IsIdle}, onSurface={SceneSwap.OnSurface}, " +
                  $"objectivesComplete={objectivesComplete}, launching={launching})", this);

        if (SceneSwap.IsIdle)
            TryLand();
        else if (SceneSwap.OnSurface)
            TryLaunch();
    }

    private void TryLand()
    {
        Planet planet = planetManager.CurrentPlanet;
        if (planet == null || !planet.isLandable || !planet.landingReady)
            return;

        objectivesComplete = false;
        SceneSwap.GoToSurface(planet.surfaceScene);
    }

    private void TryLaunch()
    {
        if (launching) return;

        if (requireObjectivesToReturn && !objectivesComplete)
        {
            Debug.Log("Lander: launch blocked, the surface objectives are not complete yet.", this);
            return;
        }

        surfaceLander = FindSurfaceLander();
        StartCoroutine(LaunchRoutine());
    }

    private IEnumerator LaunchRoutine()
    {
        launching = true;

        if (surfaceLander != null) surfaceLander.isLaunching = true;
        else Debug.LogWarning("Lander: no LanderSurface found in the surface scene; launching without it.", this);

        onLaunchStarted?.Invoke();

        if (launchDelay > 0f)
            yield return new WaitForSeconds(launchDelay);

        //Unloads the planet scene; SceneSwap's ReturnedToShip event updates the GameManager phase.
        SceneSwap.ReturnToShip();
    }

    private void HandleSurfaceEntered()
    {
        ReleaseButton();
        onLanderLand?.Invoke();

        //Look only inside the surface scene
        surfaceObjectives = FindSurfaceObjectives();
        if (surfaceObjectives == null)
        {
            Debug.LogWarning("Lander: no ObjectiveSystem in the surface scene, so launching is allowed straight away.", this);
            objectivesComplete = true;
            return;
        }

        surfaceObjectives.AllObjectivesCompleted += HandleObjectivesComplete;
    }

    private void HandleObjectivesComplete()
    {
        Debug.Log("Lander: surface objectives complete, launch unlocked.", this);
        objectivesComplete = true;
        onObjectivesComplete?.Invoke();
    }

    private void HandleReturnedToShip()
    {
        ReleaseButton();
        UnhookObjectives();
        surfaceLander = null;
        launching = false;
        objectivesComplete = false;
        onLanderReturn?.Invoke();
    }

    //The ship Player is switched off while you're on the surface
    private void ReleaseButton()
    {
        if (landerButton != null)
            landerButton.ResetButton();
    }

    private void UnhookObjectives()
    {
        if (surfaceObjectives != null)
            surfaceObjectives.AllObjectivesCompleted -= HandleObjectivesComplete;
        surfaceObjectives = null;
    }

    private static ObjectiveSystem FindSurfaceObjectives()
    {
        Scene surface = SceneSwap.SurfaceScene;
        if (!surface.IsValid()) return null;

        foreach (GameObject root in surface.GetRootGameObjects())
        {
            ObjectiveSystem system = root.GetComponentInChildren<ObjectiveSystem>(true);
            if (system != null) return system;
        }
        return null;
    }

    //Searches only the surface scene and includes inactive objects
    private static LanderSurface FindSurfaceLander()
    {
        Scene surface = SceneSwap.SurfaceScene;
        if (!surface.IsValid()) return null;

        foreach (GameObject root in surface.GetRootGameObjects())
        {
            LanderSurface lander = root.GetComponentInChildren<LanderSurface>(true);
            if (lander != null) return lander;
        }
        return null;
    }

    void RefreshText()
    {
        if (launching)
        {
            statusText.text = "LAUNCH SEQUENCE INITIATED";
        }
        else if (SceneSwap.OnSurface)
        {
            statusText.text = objectivesComplete
                ? "SURVEY COMPLETE\nPRESS TO LAUNCH"
                : "LANDER DEPLOYED\nCOMPLETE SURFACE OBJECTIVES";
        }
        else if (SceneSwap.IsBusy)
        {
            statusText.text = "TRANSIT IN PROGRESS";
        }
        else if (planetManager.CurrentPlanet == null)
        {
            statusText.text = "SHIP NOT IN ORBIT";
        }
        else if (planetManager.CurrentPlanet.landingReady == false)
        {
            statusText.text = "CANDIDATE STATUS UNKNOWN:\nPERFORM ALL SCANS";
        }
        else if (planetManager.CurrentPlanet.isLandable == false)
        {
            statusText.text = "CURRENT PLANET: NOT CANDIDATE\nPLANETARY LANDER LOCKED";
        }
        else
        {
            statusText.text = "CURRENT PLANET: POSSIBLE CANDIDATE\nPLANETARY LANDER NOW AVAILABLE";
        }
    }
}