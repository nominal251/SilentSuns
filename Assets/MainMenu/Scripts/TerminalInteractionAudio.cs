using UnityEngine;

namespace SilentSuns.Menu
{
    [RequireComponent(typeof(AudioSource))]
    public class TerminalInteractionAudio : MonoBehaviour
    {
        [SerializeField] private AudioClip keyboardPress;
        [SerializeField] private AudioClip mouseClick;
        [SerializeField, Range(0f, 1f)] private float level = 0.14f;
        private AudioSource source;
        private int lastFrame = -1;
        public static TerminalInteractionAudio Instance { get; private set; }
        public AudioClip LastClip { get; private set; }
        public int AcceptedSounds { get; private set; }

        private void Awake()
        {
            Instance = this;
            source = GetComponent<AudioSource>();
            source.playOnAwake = false;
            source.ignoreListenerPause = true;
            source.outputAudioMixerGroup = Resources.Load<UnityEngine.Audio.AudioMixer>("SilentSunsAudio").FindMatchingGroups("FX")[0];
        }

        public void Mouse() { Play(mouseClick); }
        public void Keyboard() { Play(keyboardPress); }
        public void TakeFocus() { Instance = this; }

        // Stop the previous click so held keys do not pile up sounds.
        private void Play(AudioClip clip)
        {
            if (clip == null || Time.frameCount == lastFrame)
                return;
            lastFrame = Time.frameCount;
            source.Stop();
            source.clip = clip;
            source.volume = level;
            source.Play();
            LastClip = clip;
            AcceptedSounds++;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }
}
