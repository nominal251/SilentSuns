using UnityEngine;

public class SineUpdater : MonoBehaviour
{
    public SineWave sineWave;

    public float magButtonSens = 0.1f;
    public float freqButtonSens = 0.1f;
    public float offsetButtonSens = 0.1f;

    public Button magUpButton;
    public Button magDownButton;
    public Button freqUpButton;
    public Button freqDownButton;
    public Button offsetUpButton;
    public Button offsetDownButton;

    void Update()
    {
        if (magUpButton.activated)
            sineWave.magnitude += (magButtonSens * Time.deltaTime);
        
        if (magDownButton.activated)
            sineWave.magnitude -= (magButtonSens * Time.deltaTime);

        if (freqUpButton.activated)
            sineWave.frequency += (freqButtonSens * Time.deltaTime);

        if (freqDownButton.activated)
            sineWave.frequency -= (freqButtonSens * Time.deltaTime);

        if (offsetUpButton.activated)
            sineWave.offset += (offsetButtonSens * Time.deltaTime);

        if (offsetDownButton.activated)
            sineWave.offset -= (offsetButtonSens * Time.deltaTime);
    }
}