using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneSwap
{
    private static Scene spaceScene;
    private static readonly List<GameObject> hiddenRoots = new List<GameObject>();

    public static void GoToSurface(string surfaceScene)
    {
        spaceScene = SceneManager.GetActiveScene();

        var op = SceneManager.LoadSceneAsync(surfaceScene, LoadSceneMode.Additive);
        op.completed += _ =>
        {
            SceneManager.SetActiveScene(SceneManager.GetSceneByName(surfaceScene));

            hiddenRoots.Clear();
            foreach (GameObject go in spaceScene.GetRootGameObjects())
            {
                if (go.activeSelf)
                {
                    hiddenRoots.Add(go);
                    go.SetActive(false);
                }
            }
        };
    }

    public static void ReturnToSpace(string surfaceScene)
    {
        foreach (GameObject go in hiddenRoots)
            if (go != null) go.SetActive(true);
        hiddenRoots.Clear();

        SceneManager.SetActiveScene(spaceScene);
        SceneManager.UnloadSceneAsync(surfaceScene);
    }
}