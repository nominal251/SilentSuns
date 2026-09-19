using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class WideFieldScanner : MonoBehaviour
{
    public TMP_Text wfsText;

    public GameObject shipMarker;

    public List<GameObject> planets = new List<GameObject>();

    public float confidenceRange = 0.3f;
    public float fullConfidenceMargin = 0.1f;

    private float luminosity = 0.92f;
    private float confidence = 0.1f;

    private string readyText;

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
        Calculate();

        if (confidence >= 1)
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

        // find the planet closest to the ship marker on the X axis
        foreach (GameObject planet in planets)
        {
            float distance = Mathf.Abs(
                shipMarker.transform.position.x - planet.transform.position.x
            );

            if (distance < closestDistance)
            {
                closestDistance = distance;
            }
        }

        if (planets.Count == 0)
        {
            confidence = 0f;
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