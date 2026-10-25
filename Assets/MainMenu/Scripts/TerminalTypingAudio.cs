using UnityEngine;

namespace SilentSuns.Menu
{
    [RequireComponent(typeof(AudioSource))]
    public class TerminalTypingAudio : MonoBehaviour
    {
        [SerializeField] private AudioClip[] keypresses;
        [SerializeField, Range(0f, 1f)] private float typingLevel = 0.08f;
        private AudioSource source;
        private float nextKey;
        private int lastClip = -1;

        private void Awake()
        {
            source = GetComponent<AudioSource>();
            source.outputAudioMixerGroup = GameAudio.InterfaceGroup;
        }

        public void Stop() { source.Stop(); nextKey = 0f; }

        // Short recorded keypresses follow visible characters, with a limit on overlap.
        public void Type()
        {
            if (keypresses.Length == 0 || Time.unscaledTime < nextKey)
                return;
            int index = (lastClip + Random.Range(1, keypresses.Length)) % keypresses.Length;
            source.PlayOneShot(keypresses[index], typingLevel);
            lastClip = index;
            nextKey = Time.unscaledTime + 0.045f;
        }
    }
}
