using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.Rendering.DebugUI;

public class Spectrogram : MonoBehaviour
{
    public PlanetManager planetManager;

    public Image spectrogramImage;
    public Button confirmButton;

    [InspectorName("ScreenZoneWaveform")]
    public SineUI sineUI;

    private float fillSpeed = 0.1f;

    private bool activate;

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (confirmButton.activated)
            activate = true;

        if (sineUI.isMatched == false || planetManager.CurrentPlanet == null)
            activate = false;

        if (sineUI.isMatched == true && activate == true)
        {
            spectrogramImage.fillAmount += fillSpeed * Time.deltaTime;
            Mathf.Clamp(spectrogramImage.fillAmount, 0f, 1f);
        }
        else
            spectrogramImage.fillAmount = 0;
    }
}
