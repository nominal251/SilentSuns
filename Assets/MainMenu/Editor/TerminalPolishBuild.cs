using System;
using System.Globalization;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Audio;

namespace SilentSuns.Menu.Editor
{
    public class TerminalPolishBuild : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;
        public void OnPreprocessBuild(BuildReport report)
        {
            Prepare();
        }

        [MenuItem("Silent Suns/Main Menu/Update shared UI assets")]
        public static void Prepare()
        {
            const string path = "Assets/MainMenu/Resources/TerminalUIAssets.asset";
            var data = AssetDatabase.LoadAssetAtPath<TerminalUIAssets>(path);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<TerminalUIAssets>();
                AssetDatabase.CreateAsset(data, path);
            }
            var importer = (TextureImporter)AssetImporter.GetAtPath("Assets/MainMenu/Art/shuttle-space.png");
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Bilinear;
            importer.SaveAndReimport();
            data.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/MainMenu/Fonts/TerminalFont.asset");
            data.shuttle = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/MainMenu/Art/shuttle-space.png");
            data.disconnected = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/MainMenu/Audio/Interaction/ControllerDisconnected.ogg");
            data.credits = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/MainMenu/Resources/MenuCredits.txt");
            data.crtMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/MainMenu/Shaders/TerminalCRT.mat");
            data.crtSettings = AssetDatabase.LoadAssetAtPath<TerminalCRTSettings>("Assets/MainMenu/Shaders/DefaultCRT.asset");
            data.version = PlayerSettings.bundleVersion;
            data.build = DateTime.UtcNow.ToString("yyyyMMdd.HHmmss", CultureInfo.InvariantCulture);
            data.updated = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(-5)).ToString("ddd, MM/dd/yyyy @ hh:mm tt", CultureInfo.InvariantCulture) + " EST";
            EditorUtility.SetDirty(data);
            PrepareMixer();
            AssetDatabase.SaveAssets();
        }
        private static void PrepareMixer()
        {
            var mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>("Assets/MainMenu/Resources/SilentSunsAudio.mixer");
            if (mixer.FindMatchingGroups("Interface").Length > 0)
                return;
            // Unity's mixer authoring API is editor-only. This follows the existing builder.
            Type type = mixer.GetType();
            object master = type.GetProperty("masterGroup").GetValue(mixer);
            var group = (UnityEngine.Object)type.GetMethod("CreateNewGroup").Invoke(mixer, new object[] { "Interface", false });
            type.GetMethod("AddChildToParent").Invoke(mixer, new object[] { group, master });
            object guid = group.GetType().GetMethod("GetGUIDForVolume").Invoke(group, null);
            Type pathType = type.Assembly.GetType("UnityEditor.Audio.AudioGroupParameterPath");
            object parameterPath = Activator.CreateInstance(pathType, group, guid);
            type.GetMethod("AddExposedParameter").Invoke(mixer, new[] { parameterPath });
            var serialized = new SerializedObject(mixer);
            var exposed = serialized.FindProperty("m_ExposedParameters");
            for (int i = 0; i < exposed.arraySize; i++)
            {
                var name = exposed.GetArrayElementAtIndex(i).FindPropertyRelative("name");
                if (name.stringValue.StartsWith("MyExposedParam"))
                    name.stringValue = "InterfaceVolume";
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(group);
            EditorUtility.SetDirty(mixer);
        }
    }
}
