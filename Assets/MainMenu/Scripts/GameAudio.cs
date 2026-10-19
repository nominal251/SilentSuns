using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

namespace SilentSuns.Menu
{
    public class GameAudio : MonoBehaviour
    {
        private static AudioMixer mixer;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            mixer = Resources.Load<AudioMixer>("SilentSunsAudio");
            GameObject root = new GameObject("Game Audio", typeof(GameAudio));
            DontDestroyOnLoad(root);
        }

        private void Awake() { SceneManager.sceneLoaded += OnSceneLoaded; }
        private void Start() { ApplyVolumes(); }
        private void OnDestroy() { SceneManager.sceneLoaded -= OnSceneLoaded; }

        // Existing survey sounds are effects. Explicit Music assignments are left alone.
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mixer == null)
                return;
            AudioMixerGroup[] groups = mixer.FindMatchingGroups("FX");
            foreach (GameObject root in scene.GetRootGameObjects())
                foreach (AudioSource source in root.GetComponentsInChildren<AudioSource>(true))
                    if (source.outputAudioMixerGroup == null && groups.Length > 0)
                        source.outputAudioMixerGroup = groups[0];
            ApplyVolumes();
        }

        public static void ApplyVolumes()
        {
            if (mixer == null)
                mixer = Resources.Load<AudioMixer>("SilentSunsAudio");
            if (mixer == null)
                return;
            mixer.SetFloat("FXVolume", Decibels(GamePreferences.FX));
            mixer.SetFloat("MusicVolume", Decibels(GamePreferences.Music));
        }

        // A zero slider value is silence; the remaining range follows perceived loudness.
        public static float Decibels(float value) { return value <= 0f ? -80f : 20f * Mathf.Log10(value); }
    }
}
