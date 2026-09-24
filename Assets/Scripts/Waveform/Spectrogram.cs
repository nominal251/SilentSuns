using UnityEngine;
using UnityEngine.UI;
using TMPro;
using static UnityEngine.Rendering.DebugUI;

public class Spectrogram : MonoBehaviour
{
    public PlanetManager planetManager;

    public AudioSource spectrogramScreen;

    public TextMeshPro spectrogramBackgroundText;

    public Image spectrogramImage;
    public Button confirmButton;

    [InspectorName("ScreenZoneWaveform")]
    public SineUI sineUI;

    public float volume = 1f;

    private float fillSpeed = 0.1f;

    private bool activate;

    private bool audioPlayed = false;

    void Update()
    {
        if (confirmButton.activated)
            activate = true;

        if (sineUI.isMatched == false || planetManager.CurrentPlanet == null)
            activate = false;

        if (sineUI.isMatched == true && activate == true)
        {
            if (!audioPlayed)
            {
                SequenceStart();
                audioPlayed = true;

                planetManager.CurrentPlanet.waveformComplete = true;
            }

            spectrogramImage.fillAmount += fillSpeed * Time.deltaTime;
            Mathf.Clamp(spectrogramImage.fillAmount, 0f, 1f);
        }
        else
            Reset();
    }

    void Reset()
    {
        spectrogramImage.fillAmount = 0;
        audioPlayed = false;
        spectrogramBackgroundText.text = "SIGNAL NOT ISOLATED";
    }

    void SequenceStart()
    {
        spectrogramImage.sprite = planetManager.CurrentPlanet.spectrogramImage;
        spectrogramScreen.PlayOneShot(planetManager.CurrentPlanet.spectrogramAudio);
        spectrogramBackgroundText.text = "";
    }
}
