using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PlanetManager : MonoBehaviour
{
    public bool debugMode = false;
    public GameObject debugText;

    [Header("NAV Station Assignments")]
    public GameObject objText;

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

        if (debugMode)
        {
            debugText.GetComponent<TMP_Text>().text = "Planet Name = N/A";
        }
    }
}