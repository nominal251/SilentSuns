using UnityEngine;
using UnityEngine.SceneManagement;

public class CryoStation : MonoBehaviour
{
    public PlanetManager planetManager;
    public Button cryoButton;
    public BlinkyLight blinkyLight;
    public string victoryScene;

    private bool wasComplete;

    void Update()
    {
        bool complete = AllPlanetsSurveyed();

        if (complete != wasComplete)
        {
            blinkyLight.blink = complete;
            wasComplete = complete;
        }

        if (complete && cryoButton.activated)
            SceneManager.LoadScene(victoryScene);
    }

    bool AllPlanetsSurveyed()
    {
        int count = 0;

        foreach (Planet planet in planetManager.planets)
        {
            if (planet.system != planetManager.currentSystem)
                continue;

            count++;

            if (!planet.gameObject.activeInHierarchy ||
                !planet.waveformComplete ||
                !planet.compComplete)
                return false;
        }

        return count > 0;
    }
}