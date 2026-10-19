using UnityEngine;

namespace SilentSuns.Menu
{
    // Separate sources prevent UI clicks from cutting off the physical monitor sound.
    public class TerminalPowerAudio : MonoBehaviour
    {
        [SerializeField] private AudioSource startup;
        [SerializeField] private AudioSource hum;
        [SerializeField, Range(0f, 1f)] private float startupLevel = .55f;
        [SerializeField, Range(0f, 1f)] private float humLevel = .18f;
        private float envelope;
        private float target;
        private float speed;

        public bool Running => hum != null && hum.isPlaying;

        private void Awake() { Stop(); }

        public void PowerOn()
        {
            Stop();
            GameAudio.ApplyVolumes();
            if (startup != null && startup.clip != null)
            {
                startup.volume = startupLevel;
                startup.Play();
            }
            if (hum != null && hum.clip != null)
            {
                hum.volume = 0f;
                hum.Play();
            }
            target = 1f;
            speed = 1f / 2.4f;
        }

        public void FadeOut(float duration)
        {
            target = 0f;
            speed = 1f / Mathf.Max(.01f, duration);
        }

        private void Update()
        {
            envelope = Mathf.MoveTowards(envelope, target, Time.unscaledDeltaTime * speed);
            if (hum != null) hum.volume = humLevel * envelope;
            if (target == 0f && startup != null)
                startup.volume = Mathf.MoveTowards(startup.volume, 0f, Time.unscaledDeltaTime * speed * startupLevel);
            if (target == 0f && envelope <= 0f && (startup == null || startup.volume <= 0f)) Stop();
        }

        public void Stop()
        {
            target = envelope = 0f;
            if (startup != null) { startup.Stop(); startup.volume = 0f; }
            if (hum != null) { hum.Stop(); hum.volume = 0f; }
        }

        private void OnDisable() { Stop(); }
    }
}
