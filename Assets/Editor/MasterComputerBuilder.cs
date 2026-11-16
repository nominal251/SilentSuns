using System.IO;
using SilentSuns.Menu;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class MasterComputerBuilder
{
    public const string PrefabPath = "Assets/MasterComputer/MasterComputer.prefab";
    public const string ReviewPath = "Assets/MasterComputer/MasterComputerReview.unity";

    [MenuItem("Silent Suns/Master Computer/Rebuild prefab and review scene")]
    public static void Prepare()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        Directory.CreateDirectory("Assets/MasterComputer");
        AssetDatabase.Refresh();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var root = new GameObject("Master Computer");
        // Keep the FBX transforms: the monitor and console were modelled as matching parts.
        AddModel("Assets/Meshes/Ship Interior/MasterComputer_Monitor.fbx", root.transform);
        AddModel("Assets/Meshes/Ship Interior/MasterComputer_Console.fbx", root.transform);
        var glass = root.transform.Find("MasterComputer_Monitor/Screen1").GetComponent<Renderer>();
        var face = GameObject.CreatePrimitive(PrimitiveType.Quad);
        face.name = "Master screen";
        face.transform.SetParent(root.transform, false);
        face.transform.position = new Vector3(glass.bounds.center.x, glass.bounds.center.y, glass.bounds.max.z + .002f);
        face.transform.rotation = Quaternion.Euler(0, 180, 0);
        face.transform.localScale = new Vector3(glass.bounds.size.x * .985f, glass.bounds.size.y * .985f, 1);
        face.tag = "Interactable";
        var light = new GameObject("Alert light", typeof(Light)).GetComponent<Light>();
        light.transform.SetParent(root.transform, false);
        light.transform.localPosition = new Vector3(0, -.32f, .48f);
        // Keep console lighting together for the upcoming console model replacement.
        light.transform.SetParent(root.transform.Find("MasterComputer_Console"), true);
        light.type = LightType.Point;
        light.color = Color.red;
        light.range = .55f;
        light.enabled = false;
        var display = root.AddComponent<MasterComputerDisplay>();
        display.Configure(face.GetComponent<Renderer>());
        display.ConfigureStartup(AssetDatabase.LoadAssetAtPath<Texture>("Assets/MasterComputer/Media/ScreenIntrusion.png"));
        root.AddComponent<MasterComputerInterference>().Configure(display,
            AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/MasterComputer/Media/ScreenIntrusion.mp3"));
        var station = face.AddComponent<MasterComputerStation>();
        station.Configure(display, light);
        var selector = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Meshes/Buttons/RotarySwitch_4_Notches.fbx"));
        selector.name = "Page selector";
        selector.transform.SetParent(root.transform, false);
        // The FBX origin is above its base; lift the base clear of the sloped console face.
        selector.transform.localPosition = new Vector3(-.08f, -.546f, .939f);
        selector.transform.localRotation = Quaternion.Euler(50.79f, 0, 0) * Quaternion.Euler(0, 180, 0);
        selector.transform.localScale = Vector3.one * 1.6f;
        selector.tag = "Interactable";
        var hitbox = selector.AddComponent<BoxCollider>();
        hitbox.center = new Vector3(0, -.005f, 0);
        hitbox.size = new Vector3(.19f, .12f, .19f);
        var dial = selector.transform.Find("Dial");
        // The controller is on the dial child so the hitbox has only one Interactable.
        var rotary = dial.gameObject.AddComponent<RotarySwitch>();
        rotary.dial = dial;
        rotary.stateRotations = new Vector3[4];
        var labels = new[] { "Zero", "One", "Two", "Three" };
        for (int i = 0; i < labels.Length; i++)
        {
            // Use the printed notch positions after Unity imports the model from Maya.
            var label = selector.transform.Find(labels[i]).GetComponent<Renderer>();
            var direction = selector.transform.InverseTransformPoint(label.bounds.center) - dial.localPosition;
            rotary.stateRotations[i] = new Vector3(0, Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg, 0);
        }
        dial.localEulerAngles = rotary.stateRotations[0];
        rotary.useUnscaledTime = true;
        rotary.switchSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/Click2.mp3");
        selector.AddComponent<MasterComputerSelector>().Configure(station, rotary);
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        var review = new GameObject("Master Computer Review").AddComponent<MasterComputerReview>();
        var serialized = new SerializedObject(review);
        serialized.FindProperty("stationPrefab").objectReferenceValue = prefab;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.SaveScene(scene, ReviewPath);
        AssetDatabase.SaveAssets();
    }

    private static void AddModel(string path, Transform parent)
    {
        var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));
        model.transform.SetParent(parent, true);
    }

    [MenuItem("Silent Suns/Master Computer/Open review scene")]
    public static void OpenReview()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        EditorSceneManager.OpenScene(ReviewPath);
    }

    public static void BuildReview()
    {
        var scenes = new System.Collections.Generic.List<string> { ReviewPath };
        foreach (var scene in EditorBuildSettings.scenes) if (scene.enabled) scenes.Add(scene.path);
        string directory = System.Environment.GetEnvironmentVariable("SILENT_SUNS_MASTER_OUTPUT");
        if (string.IsNullOrEmpty(directory)) throw new System.InvalidOperationException("Set SILENT_SUNS_MASTER_OUTPUT for the review build.");
        Directory.CreateDirectory(directory);
        var report = BuildPipeline.BuildPlayer(scenes.ToArray(), Path.Combine(directory, "SilentSuns-MasterComputer.exe"), BuildTarget.StandaloneWindows64, BuildOptions.None);
        if (report.summary.result != BuildResult.Succeeded) throw new System.Exception("Master computer review build failed.");
    }
}
