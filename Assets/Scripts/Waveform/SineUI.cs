using UnityEngine;
using TMPro;

public class SineUI : MonoBehaviour
{
    public SineWave playerSine;
    public SineWave targetSine;

    public TMP_Text UIText;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        UpdateText();
    }

    // Update is called once per frame
    void Update()
    {
        UpdateText();
    }

    void UpdateText()
    {
        UIText.text = (
            "P Mag " + playerSine.magnitude + " - P Freq " + playerSine.freqButtonSens +
            "\nT Mag " + targetSine.magnitude + " - T Freq " + targetSine.freqButtonSens
        );
        
    }
}
