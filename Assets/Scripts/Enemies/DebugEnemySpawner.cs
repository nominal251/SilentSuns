using UnityEngine;

public class DebugEnemySpawner : MonoBehaviour
{
    public Button spawnButton;
    public GameObject enemy;

    // Update is called once per frame
    void Update()
    {
        if (spawnButton.activated)
        {
            enemy.SetActive(true);
        }
    }
}
