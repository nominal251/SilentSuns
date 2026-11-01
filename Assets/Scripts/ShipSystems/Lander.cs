using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class Lander : MonoBehaviour
{
    public Button landerButton;
    public PlanetManager planetManager;
    public TextMeshPro statusText;
    // Update is called once per frame

    private bool alreadyLanded = false;

    void Update()
    {
        if (landerButton.activated && !alreadyLanded)
        {
            Planet planet = planetManager.CurrentPlanet;

            if (planet != null && planet.isLandable && planet.landingReady)
            {
                alreadyLanded = true;
                SceneSwap.GoToSurface(planet.surfaceScene);
            }
        }

        RefreshText();
    }

    void RefreshText()
    {
        if (planetManager.CurrentPlanet == null)
        {
            statusText.text = "SHIP NOT IN ORBIT";
        } 
        else if (planetManager.CurrentPlanet.landingReady == false)
        {
            statusText.text = "CANDIDATE STATUS UNKNOWN:\nPERFORM ALL SCANS";
        }
        else if (planetManager.CurrentPlanet.isLandable == false)
        {
            statusText.text = "CURRENT PLANET: NOT CANDIDATE\nPLANETARY LANDER LOCKED";
        }
        else if (planetManager.CurrentPlanet.isLandable == true)
        {
            statusText.text = "CURRENT PLANET: POSSIBLE CANDIDATE\nPLANETARY LANDER NOW AVAILABLE";
        }
        else
        {
            statusText.text = "YOU ARE NOT SUPPOSED TO SEE THIS TEXT";
        }
    }
}
