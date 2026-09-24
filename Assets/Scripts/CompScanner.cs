using UnityEngine;
using TMPro;

public class CompScanner : MonoBehaviour
{
    public PlanetManager planetManager;

    public TextMeshPro compText;

    public Button startButton;

    private string stats;

    private bool scanFinished = false;
    private bool startScan = false;

    private float progressBarPercent = 0f;

    private string MakeProgressBar(float progress, int length = 20)
    {
        progress = Mathf.Clamp01(progress);

        int filled = Mathf.RoundToInt(progress * length);
        int empty = length - filled;

        return "[" + new string('▒', filled) + new string(' ', empty) + "]";
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        RefreshText();
    }

    // Update is called once per frame
    void Update()
    {
        if (startButton.activated == true)
            startScan = true;

        RefreshText();

        if (planetManager.CurrentPlanet == null)
            Reset();
    }

    void RefreshText()
    {
        if (planetManager.CurrentPlanet != null && scanFinished == true)
        {
            stats = (
                "ATMO:\n- " + planetManager.CurrentPlanet.atmoText1 + "\n- " + planetManager.CurrentPlanet.atmoText2 +
                "\nPHYS:\n- " + planetManager.CurrentPlanet.compText1 + "\n- " + planetManager.CurrentPlanet.compText2 + "\n"
            );

            compText.text = "Composition Scan Results:\n" + stats;
        } else if (planetManager.CurrentPlanet != null && scanFinished == false && startScan == true)
        {
            progressBarPercent += 0.15f * Time.deltaTime;

            compText.text = "SCANNING: \n" + MakeProgressBar(progressBarPercent, 20);

            if (progressBarPercent >= 1f)
            {
                scanFinished = true;
                startScan = false;
            }
        } else if (planetManager.CurrentPlanet != null)
        {
            compText.text = "READY TO SCAN\n" + MakeProgressBar(0f, 20);
        }
        else
            compText.text = "No Planet In Range";
    }

    void Reset()
    {
        progressBarPercent = 0f;
        startScan = false;
        scanFinished = false;
    }
}
