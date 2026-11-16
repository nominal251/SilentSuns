using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PlanetManager : MonoBehaviour
{
    public int currentSystem = 1;

    public bool currentSystemLanderComplete = false;

    public bool debugMode = false;
    public GameObject debugText;

    [Header("NAV Station Assignments")]
    public GameObject objText;

    [Header("Waveform Station Assignments")]
    public SineWave targetSine;
    [InspectorName("ScreenZoneWaveform")]
    public SineUI sineUI;

    [Header("Other Assignments")]

    // Only the planets in the current system
    public List<Planet> planets = new List<Planet>();

    // Every planet in every system
    private readonly List<Planet> allPlanets = new List<Planet>();

    public Planet CurrentPlanet { get; private set; }

    // Fired after the system number changes and the planet list has been rebuilt
    public event System.Action SystemChanged;

    // True if any planet belongs to a later system
    public bool HasNextSystem
    {
        get
        {
            foreach (Planet planet in allPlanets)
                if (planet.system > currentSystem)
                    return true;
            return false;
        }
    }

    void Awake()
    {
        allPlanets.Clear();

        // include inactive planets, since undiscovered planets start disabled
        foreach (Planet planet in FindObjectsByType<Planet>(FindObjectsInactive.Include))
        {
            if (planet.CompareTag("Planet"))
                allPlanets.Add(planet);
        }

        // sort by system number first, then by object name within each system
        allPlanets.Sort((a, b) =>
        {
            int bySystem = a.system.CompareTo(b.system);
            return bySystem != 0
                ? bySystem
                : string.Compare(a.gameObject.name, b.gameObject.name);
        });

        RebuildSystemList();
    }

    void Start()
    {
        debugText.GetComponent<TMP_Text>().text = "Planet Name = N/A";
        objText.GetComponent<TMP_Text>().text = "ORBT: N/A";
        debugText.SetActive(debugMode);
    }

    void Update()
    {
        var keyboard = UnityEngine.InputSystem.Keyboard.current;
        if (keyboard != null && keyboard.f10Key.wasPressedThisFrame)
            DebugCompleteSystem();
    }

    // Refill the public list with only the current system's planets
    private void RebuildSystemList()
    {
        planets.Clear();

        foreach (Planet planet in allPlanets)
        {
            if (planet.system == currentSystem)
                planets.Add(planet);
        }

        Debug.Log($"PlanetManager: system {currentSystem} has {planets.Count} planets.", this);
    }

    public void AdvanceToNextSystem()
    {
        // The old current planet is about to be disabled, so drop the reference
        ClearCurrentPlanet();

        // Hide every planet and wipe its scan progress
        foreach (Planet planet in allPlanets)
        {
            planet.waveformComplete = false;
            planet.compComplete = false;
            planet.landingReady = false;

            if (planet.enemy != null)
                planet.enemy.SetActive(false);

            planet.gameObject.SetActive(false);
        }

        currentSystem++;
        currentSystemLanderComplete = false;

        RebuildSystemList();
        SystemChanged?.Invoke();
    }

    public void SetCurrentPlanet(Planet planet)
    {
        CurrentPlanet = planet;

        objText.GetComponent<TMP_Text>().text = "ORBT: " + planet.name;

        targetSine.magnitude = planet.sineMag;
        targetSine.frequency = planet.sineFreq;
        targetSine.offset = planet.sineOffset;

        sineUI.UpdateTargetSineState();

        if (debugMode)
        {
            debugText.GetComponent<TMP_Text>().text =
                "Planet Name = " + planet.name;
        }
    }

    public void ClearCurrentPlanet()
    {
        CurrentPlanet = null;

        objText.GetComponent<TMP_Text>().text = "ORBT: N/A";

        sineUI.UpdateTargetSineState();

        if (debugMode)
        {
            debugText.GetComponent<TMP_Text>().text = "Planet Name = N/A";
        }
    }

    public void DebugCompleteSystem()
    {
        foreach (Planet planet in planets)
        {
            planet.gameObject.SetActive(true);   // discover planet
            planet.waveformComplete = true;
            planet.compComplete = true;
        }

        currentSystemLanderComplete = true;

        Debug.Log($"PlanetManager: DEBUG marked system {currentSystem} complete.", this);
    }
}