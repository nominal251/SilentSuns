using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PlanetManager : MonoBehaviour
{
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
}