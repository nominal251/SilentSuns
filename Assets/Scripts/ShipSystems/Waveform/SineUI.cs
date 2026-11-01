using UnityEngine;
using TMPro;

public class SineUI : MonoBehaviour
{
    public PlanetManager planetManager;

    public SineWave playerSine;
    public SineWave targetSine;

    public TMP_Text UITextValues;
    public TMP_Text UITextMatch;

    [HideInInspector]
    public bool isMatched = false;

    void Start()
    {
        UpdateTargetSineState();
        UpdateText();
    }

    void Update()
    {
        CheckMatch();
        UpdateText();
    }

    public void UpdateTargetSineState()
    {
        if (planetManager.CurrentPlanet != null)
            targetSine.gameObject.SetActive(true);
        else
            targetSine.gameObject.SetActive(false);
    }

    void UpdateText()
    {
        UITextValues.text = (
            "P Mag " + playerSine.magnitude.ToString("F2") + " - P Freq " + playerSine.frequency.ToString("F2") +
            "\nT Mag " + targetSine.magnitude.ToString("F2") + " - T Freq " + targetSine.frequency.ToString("F2")
        );

        if (isMatched)
            UITextMatch.text = "! SIGNAL ISOLATED !";
        else if (planetManager.CurrentPlanet != null)
            UITextMatch.text = "SIGNAL NOT ISOLATED";
        else
            UITextMatch.text = "NO SIGNAL DETECTED";
    }

    void CheckMatch()
    {
        bool magnitudeMatch = Mathf.Abs(
            playerSine.magnitude - targetSine.magnitude
        ) <= 0.15f;

        bool frequencyMatch = Mathf.Abs(
            playerSine.frequency - targetSine.frequency
        ) <= 0.15f;

        bool offsetMatch = OffsetMatches();

        if (planetManager.CurrentPlanet != null)
            isMatched = magnitudeMatch && frequencyMatch && offsetMatch;
        else
            isMatched = false;
    }

    bool OffsetMatches()
    {
        float period = 2f * Mathf.PI;

        float offsetDifference = Mathf.Repeat(
            playerSine.offset - targetSine.offset,
            period
        );

        offsetDifference = Mathf.Min(
            offsetDifference,
            period - offsetDifference
        );

        return offsetDifference <= 0.2f;
    }
}