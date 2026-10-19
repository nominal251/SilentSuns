using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SilentSuns.Menu.Editor
{
    public static class TerminalShipUpgrade
    {
        public static void Apply()
        {
            var scene = EditorSceneManager.OpenScene("Assets/MainMenu/Scenes/MainMenu.unity");
            ConfigureDisplay(Object.FindFirstObjectByType<TerminalDisplay>());
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            const string path = "Assets/MainMenu/Resources/SilentSunsPause.prefab";
            var pause = PrefabUtility.LoadPrefabContents(path);
            ConfigurePause(pause);
            PrefabUtility.SaveAsPrefabAsset(pause, path);
            PrefabUtility.UnloadPrefabContents(pause);
            AssetDatabase.SaveAssets();
        }

        public static void ConfigurePause(GameObject root)
        {
            var display = root.GetComponent<TerminalPauseDisplay>();
            if (display == null) display = root.AddComponent<TerminalPauseDisplay>();
            var fields = new SerializedObject(display);
            fields.FindProperty("screenMaterial").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Material>("Assets/MainMenu/Shaders/TerminalCRT.mat");
            fields.FindProperty("settings").objectReferenceValue = AssetDatabase.LoadAssetAtPath<TerminalCRTSettings>("Assets/MainMenu/Shaders/DefaultCRT.asset");
            fields.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void ConfigureDisplay(TerminalDisplay display)
        {
            var fields = new SerializedObject(display);
            fields.FindProperty("shipSpillShader").objectReferenceValue = Shader.Find("Silent Suns/Ship Screen Spill");
            fields.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
