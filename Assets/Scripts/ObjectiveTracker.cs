using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ObjectiveTracker : MonoBehaviour
{
    public Planet planet1;
    public Planet planet2;
    public Planet planet3;
    public Planet planet4;

    public TextMeshPro objText;

    [HideInInspector]
    public bool allObjectivesComplete = false;

    public Button cryoButton;
    public Button debugButton;

    private bool debugComplete = false;

    private string stats;

    private string p1String;
    private string p2String;
    private string p3String;
    private string p4String;

    public string victoryScene;

    public BlinkyLight blinkyLight;
    private bool var = true;

    void Start()
    {
        RefreshText();
    }

    void Update()
    {
        if (debugButton.activated)
            debugComplete = true;

        RefreshText();

        if (cryoButton.activated && allObjectivesComplete)
            SceneManager.LoadScene(victoryScene);

        if (allObjectivesComplete && var)
        {
            blinkyLight.blink = true;
            var = false;
        }
    }

    void RefreshText()
    {
        // Planet 1
        if (!planet1.gameObject.activeInHierarchy)
        {
            p1String = "PLANET 1 NOT LOCATED";
        }
        else
        {
            p1String =
                planet1.name +
                "- Signal Scan: [" +
                (planet1.waveformComplete ? "X" : " ") +
                "] - Comp Scan: [" +
                (planet1.compComplete ? "X" : " ") +
                "]";
        }

        // Planet 2
        if (!planet2.gameObject.activeInHierarchy)
        {
            p2String = "PLANET 2 NOT LOCATED";
        }
        else
        {
            p2String =
                planet2.name +
                "- Signal Scan: [" +
                (planet2.waveformComplete ? "X" : " ") +
                "] - Comp Scan: [" +
                (planet2.compComplete ? "X" : " ") +
                "]";
        }

        // Planet 3
        if (!planet3.gameObject.activeInHierarchy)
        {
            p3String = "PLANET 3 NOT LOCATED";
        }
        else
        {
            p3String =
                planet3.name +
                "- Signal Scan: [" +
                (planet3.waveformComplete ? "X" : " ") +
                "] - Comp Scan: [" +
                (planet3.compComplete ? "X" : " ") +
                "]";
        }

        // Planet 4
        if (!planet4.gameObject.activeInHierarchy)
        {
            p4String = "PLANET 4 NOT LOCATED";
        }
        else
        {
            p4String =
                planet4.name +
                "- Signal Scan: [" +
                (planet4.waveformComplete ? "X" : " ") +
                "] - Comp Scan: [" +
                (planet4.compComplete ? "X" : " ") +
                "]";
        }

        // Check whether every objective is complete
        allObjectivesComplete =
            debugComplete ||
            (
                planet1.waveformComplete && planet1.compComplete &&
                planet2.waveformComplete && planet2.compComplete &&
                planet3.waveformComplete && planet3.compComplete &&
                planet4.waveformComplete && planet4.compComplete
            );

        stats =
            p1String + "\n" +
            p2String + "\n" +
            p3String + "\n" +
            p4String;

        objText.text = "Bodies Surveyed:\n" + stats;
    }
}