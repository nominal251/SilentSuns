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
        bool complete = SystemComplete();

        // Only touch the light when the state changes
        if (complete != wasComplete)
        {
            blinkyLight.blink = complete;
            wasComplete = complete;
        }

        if (complete && cryoButton.activated)
        {
            if (planetManager.HasNextSystem)
                planetManager.AdvanceToNextSystem();
            else
                SceneManager.LoadScene(victoryScene);
        }
    }

    bool SystemComplete()
    {
        // A system with no planets must not count as complete
        if (planetManager.planets.Count == 0)
            return false;

        bool hasLandable = false;

        foreach (Planet planet in planetManager.planets)
        {
            if (planet.isLandable)
                hasLandable = true;

            if (!planet.gameObject.activeInHierarchy ||
                !planet.waveformComplete ||
                !planet.compComplete)
                return false;
        }

        // If the system has a landable planet, the player must have landed and launched
        if (hasLandable && !planetManager.currentSystemLanderComplete)
            return false;

        return true;
    }
}