using UnityEngine;
using UnityEngine.SceneManagement;

public class LanderSurface : MonoBehaviour
{
    public Button launchButton;

    public bool objectivesComplete = true;

    public string spaceScene = "SampleScene";

    private bool isLaunching = false;

    void Update()
    {
        if (launchButton.activated && objectivesComplete && !isLaunching)
        {
            isLaunching = true;

            SceneManager.LoadScene(spaceScene);
        }
    }
}