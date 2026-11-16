using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneSwap
{
    //Variables for loading the scene in
    public static Vector3 SurfaceOffset = Vector3.zero;
    public static bool OnSurface { get; private set; }
    public static bool IsBusy { get; private set; }
    public static bool IsIdle => !OnSurface && !IsBusy;
    public static Scene SurfaceScene => surfaceScene;

    public static event System.Action SurfaceEntered;
    public static event System.Action ReturnedToShip;

    private static Scene shipScene;
    private static Scene surfaceScene;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetState()
    {
        OnSurface = false;
        IsBusy = false;
        SceneManager.sceneUnloaded -= SceneUnloaded;
        SceneManager.sceneUnloaded += SceneUnloaded;
    }

    private static void SceneUnloaded(Scene scene)
    {
        // Restart or main menu can unload the planet without using the launch button.
        if (scene != surfaceScene) return;
        OnSurface = false;
        IsBusy = false;
        surfaceScene = default;
    }

    public static void GoToSurface(string sceneName)
    {
        if (!IsIdle)
            return;

        if (string.IsNullOrEmpty(sceneName) || !Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError($"SceneSwap: '{sceneName}' is empty or not in Build Settings.");
            return;
        }

        IsBusy = true;
        shipScene = SceneManager.GetActiveScene();

        AsyncOperation load = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        load.completed += _ =>
        {
            //The newly loaded additive scene is always the last one.
            surfaceScene = SceneManager.GetSceneAt(SceneManager.sceneCount - 1);
            ApplySurfaceOffset();
            SceneManager.SetActiveScene(surfaceScene);

            OnSurface = true;
            IsBusy = false;
            SurfaceEntered?.Invoke();
        };
    }

    private static void ApplySurfaceOffset()
    {
        if (SurfaceOffset == Vector3.zero)
            return;

        foreach (GameObject root in surfaceScene.GetRootGameObjects())
            root.transform.position += SurfaceOffset;
        Physics.SyncTransforms();
    }

    //Same as ReturnToShip; the string keeps ship scene
    public static void ReturnToSpace() => ReturnToShip();
    public static void ReturnToSpace(string surfaceSceneName) => ReturnToShip();

    //Unloads the surface scene and hands control back to the ship.
    public static void ReturnToShip()
    {
        if (IsBusy || !OnSurface)
            return;

        IsBusy = true;

        //active at the same time as the ship's player.
        foreach (GameObject root in surfaceScene.GetRootGameObjects())
            root.SetActive(false);

        SceneManager.SetActiveScene(shipScene);
        ReturnedToShip?.Invoke();

        AsyncOperation unload = SceneManager.UnloadSceneAsync(surfaceScene);
        unload.completed += _ =>
        {
            OnSurface = false;
            IsBusy = false;
        };
    }
}