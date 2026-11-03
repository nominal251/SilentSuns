using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;

namespace SilentSuns.Menu.Editor
{
    public static class TerminalWarmupUpgrade
    {
        // Patch references in place so hand-edited layout and gameplay scenes stay intact.
        public static void Apply()
        {
            var scene = EditorSceneManager.OpenScene("Assets/MainMenu/Scenes/MainMenu.unity");
            var root = GameObject.Find("ARCA Terminal");
            Configure(root);
            ConfigureDisplay(root.GetComponentInChildren<TerminalDisplay>());
            root.transform.Find("Boot/POST progress").GetComponent<TMP_Text>().text = "[............]  SYSTEM CHECK   0%";
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
        }

        public static void Configure(GameObject root)
        {
            Transform existing = root.transform.Find("Monitor Audio");
            var audio = existing != null ? existing.gameObject : new GameObject("Monitor Audio");
            audio.transform.SetParent(root.transform, false);
            var controller = audio.GetComponent<TerminalPowerAudio>() ?? audio.AddComponent<TerminalPowerAudio>();
            var mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>("Assets/MainMenu/Resources/SilentSunsAudio.mixer");
            var fields = new SerializedObject(controller);
            string[] names = { "CRTPowerOn", "CRTHum" };
            string[] references = { "startup", "hum" };
            for (int i = 0; i < names.Length; i++)
            {
                string path = "Assets/MainMenu/Audio/Hardware/" + names[i] + ".wav";
                var importer = (AudioImporter)AssetImporter.GetAtPath(path);
                importer.forceToMono = true;
                var samples = importer.defaultSampleSettings;
                samples.loadType = AudioClipLoadType.DecompressOnLoad;
                samples.compressionFormat = AudioCompressionFormat.PCM;
                samples.preloadAudioData = true;
                importer.defaultSampleSettings = samples;
                importer.SaveAndReimport();
                Transform child = audio.transform.Find(names[i]);
                var source = child != null ? child.GetComponent<AudioSource>() : new GameObject(names[i], typeof(AudioSource)).GetComponent<AudioSource>();
                source.transform.SetParent(audio.transform, false);
                source.playOnAwake = false;
                source.loop = i == 1;
                source.spatialBlend = 0f;
                source.ignoreListenerPause = true;
                source.volume = 0f;
                source.outputAudioMixerGroup = mixer.FindMatchingGroups("FX")[0];
                source.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                fields.FindProperty(references[i]).objectReferenceValue = source;
            }
            fields.ApplyModifiedPropertiesWithoutUndo();
            var menu = new SerializedObject(root.GetComponent<TerminalMenu>());
            menu.FindProperty("powerAudio").objectReferenceValue = controller;
            menu.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void ConfigureDisplay(TerminalDisplay display)
        {
            var fields = new SerializedObject(display);
            fields.FindProperty("textLightShader").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Shader>("Assets/MainMenu/Shaders/TerminalTextLight.shader");
            fields.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
