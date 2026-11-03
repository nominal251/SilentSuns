using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WideFieldScanner : MonoBehaviour
{
    public PlanetManager planetManager;

    public TMP_Text wfsText;

    public Button confirmButton;

    public GameObject shipMarker;

    // Filled automatically with the current system's undiscovered planets
    public List<GameObject> planets = new List<GameObject>();

    public float confidenceRange = 0.3f;
    public float fullConfidenceMargin = 0.1f;

    private float luminosity = 1f;
    private float confidence = 0.1f;

    private string readyText;

    private GameObject currentPlanet;

    private bool scanComplete = false;

    private string MakeProgressBar(float progress, int length = 20)
    {
        progress = Mathf.Clamp01(progress);

        int filled = Mathf.RoundToInt(progress * length);
        int empty = length - filled;

        return "[" + new string('▒', filled) + new string(' ', empty) + "]";
    }

    void Awake()
    {
        if (planetManager == null)
        {
            PlanetManager[] managers = FindObjectsByType<PlanetManager>();
            if (managers.Length > 0)
                planetManager = managers[0];
        }
    }

    void Start()
    {
        if (planetManager == null)
        {
            Debug.LogError("WideFieldScanner: no PlanetManager found in the scene.", this);
            return;
        }

        planetManager.SystemChanged += RebuildPlanetList;
        RebuildPlanetList();
    }

    void OnDestroy()
    {
        if (planetManager != null)
            planetManager.SystemChanged -= RebuildPlanetList;
    }

    // Fill the scanner with the current system's undiscovered planets
    void RebuildPlanetList()
    {
        planets.Clear();

        foreach (Planet planet in planetManager.planets)
        {
            if (!planet.gameObject.activeInHierarchy)
                planets.Add(planet.gameObject);
        }

        scanComplete = false;
        currentPlanet = null;
        confidence = 0.1f;

        Calculate();
    }

    void Update()
    {
        // stop doing anything once the scan is complete
        if (scanComplete)
            return;

        if (confirmButton.activated && confidence == 1f)
        {
            if (currentPlanet != null)
            {
                // enable the planet
                currentPlanet.SetActive(true);

                // remove it from the scanner
                planets.Remove(currentPlanet);

                currentPlanet = null;
            }
        }

        Calculate();

        if (confidence >= 1f)
        {
            readyText = "! POS CONFIRM READY !";
        }
        else
        {
            readyText = "POS CONFIRM NOT READY";
        }
    }

    // True if the planet is in the current system and hasn't been located yet
    private bool NeedsLocating(GameObject planetObject)
    {
        if (planetManager == null || planetObject == null)
            return false;

        // Already located (normally or via the debug key), so the scanner no longer needs it
        if (planetObject.activeInHierarchy)
            return false;

        return planetObject.TryGetComponent(out Planet planet) &&
               planet.system == planetManager.currentSystem;
    }

    void Calculate()
    {
        float closestDistance = Mathf.Infinity;
        currentPlanet = null;
        int remaining = 0;

        // find the planet closest to the ship marker on the X axis
        // ignore located planets, planets in other systems, and planets below the ship
        foreach (GameObject planet in planets)
        {
            if (!NeedsLocating(planet))
                continue;

            remaining++;

            if (shipMarker.transform.position.y > planet.transform.position.y)
                continue;

            float distance = Mathf.Abs(
                shipMarker.transform.position.x - planet.transform.position.x
            );

            if (distance < closestDistance)
            {
                closestDistance = distance;
                currentPlanet = planet;
            }
        }

        // all planets in this system have been found
        if (remaining == 0)
        {
            confidence = 0f;
            scanComplete = true;

            wfsText.text = "ALL PLANETARY MASS\nOBJECTS LOCATED";

            return;
        }

        // within the margin = 100% confidence
        if (closestDistance <= fullConfidenceMargin)
        {
            confidence = 1f;
        }
        else
        {
            // decrease confidence as distance increases
            confidence = 1f - (
                (closestDistance - fullConfidenceMargin) /
                (confidenceRange - fullConfidenceMargin)
            );

            confidence = Mathf.Clamp01(confidence);
        }

        if (confidence >= 1f)
        {
            readyText = "! POS CONFIRM READY !";
        }
        else
        {
            readyText = "POS CONFIRM NOT READY";
        }

        luminosity = 1f - (confidence * 0.008f);

        RefreshText();
    }

    void RefreshText()
    {
        wfsText.text = (
            "PRIMARY STAR LUMINOSITY: " + (luminosity * 100f).ToString("F0") + "%\n" +
            MakeProgressBar(luminosity) +
            "\n\nPLANET POS CONFIDENCE: " + (confidence * 100f).ToString("F0") + "%\n" +
            MakeProgressBar(confidence) +
            "\n\n" + readyText
        );
    }
}