using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Windows.Speech;

public class WideFieldScanner : MonoBehaviour
{
    public TMP_Text wfsText;

    public Button confirmButton;

    public GameObject shipMarker;

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

    void Start()
    {
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

    void Calculate()
    {
        float closestDistance = Mathf.Infinity;
        currentPlanet = null;

        // find the planet closest to the ship marker on the X axis
        // ignore planets that are below the ship
        foreach (GameObject planet in planets)
        {
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

        // all planets have been found
        if (planets.Count == 0)
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