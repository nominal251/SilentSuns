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

    public List<Planet> planets = new List<Planet>();

    public Planet CurrentPlanet { get; private set; }

    void Awake()
    {
        planets.Clear();

        // autofill planets list
        foreach (Planet planet in FindObjectsByType<Planet>(FindObjectsInactive.Include))
        {
            if (planet.CompareTag("Planet"))
                planets.Add(planet);
        }

        // sort by system number first, then by object name within each system
        planets.Sort((a, b) =>
        {
            int bySystem = a.system.CompareTo(b.system);
            return bySystem != 0
                ? bySystem
                : string.Compare(a.gameObject.name, b.gameObject.name);
        });

        Debug.Log($"PlanetManager: registered {planets.Count} planets.", this);
    }

    void Start()
    {
        debugText.GetComponent<TMP_Text>().text = "Planet Name = N/A";
        objText.GetComponent<TMP_Text>().text = "ORBT: N/A";
        debugText.SetActive(debugMode);
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

    void Update()
    {
        var keyboard = UnityEngine.InputSystem.Keyboard.current;
        if (keyboard != null && keyboard.f10Key.wasPressedThisFrame)
            DebugCompleteSystem();
    }

    public void DebugCompleteSystem()
    {
        foreach (Planet planet in planets)
        {
            if (planet.system != currentSystem)
                continue;

            planet.gameObject.SetActive(true);   // discover planet
            planet.waveformComplete = true;
            planet.compComplete = true;
        }

        currentSystemLanderComplete = true;

        Debug.Log($"PlanetManager: DEBUG marked system {currentSystem} complete.", this);
    }
}