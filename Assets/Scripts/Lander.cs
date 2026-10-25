using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class Lander : MonoBehaviour
{
    public Button landerButton;
    public PlanetManager planetManager;
    public TextMeshPro statusText;
    // Update is called once per frame
    void Update()
    {
        if (landerButton.activated)
        {
            if (planetManager.CurrentPlanet != null)
            {
                if (planetManager.CurrentPlanet.isLandable && planetManager.CurrentPlanet.landingReady == true)
                {
                    SceneManager.LoadScene(planetManager.CurrentPlanet.surfaceScene);
                }
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
