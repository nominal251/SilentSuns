using UnityEngine;

public class MinigameManager : MonoBehaviour
{
    private IMinigame currentMinigame;

    public void StartMinigame(IMinigame minigame)
    {
        if (currentMinigame != null)
        {
            currentMinigame.EndMinigame();
        }

        currentMinigame = minigame;
        currentMinigame.StartMinigame();
    }

    public void EndCurrentMinigame()
    {
        if (currentMinigame == null) return;

        currentMinigame.EndMinigame();
        currentMinigame = null;
    }
}

