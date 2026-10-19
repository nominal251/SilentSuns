using System.Collections;
using UnityEngine;

namespace SilentSuns.Menu
{
    [RequireComponent(typeof(AudioSource))]
    public class TerminalMenuMusic : MonoBehaviour
    {
        [Header("Playback")]
        [SerializeField, Range(0f, 1f)] private float musicLevel = 0.55f;
        [SerializeField, Min(0.1f)] private float fadeInDuration = 0.35f;
        [SerializeField, Min(0.1f)] private float fadeOutDuration = 0.9f;
        private AudioSource source;
        private Coroutine fadeRoutine;
        private float envelope;

        public float FadeOutDuration => fadeOutDuration;

        private void Update()
        {
            source.volume = envelope;
        }

        private void Awake()
        {
            source = GetComponent<AudioSource>();
            source.volume = 0f;
        }

        // Playback begins after electrical warm-up, with the saved mixer level already applied.
        public void Play()
        {
            Stop();
            GameAudio.ApplyVolumes();
            source.Play();
            fadeRoutine = StartCoroutine(Fade(musicLevel, fadeInDuration));
        }

        public void Stop()
        {
            if (fadeRoutine != null) StopCoroutine(fadeRoutine);
            fadeRoutine = null;
            envelope = 0f;
            source.volume = 0f;
            source.Stop();
        }

        public void FadeOut()
        {
            if (fadeRoutine != null)
                StopCoroutine(fadeRoutine);
            fadeRoutine = StartCoroutine(Fade(0f, fadeOutDuration));
        }

        // Fade the source; the Music mixer slider remains independent of this envelope.
        private IEnumerator Fade(float target, float duration)
        {
            float initial = envelope;
            for (float elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
            {
                envelope = Mathf.SmoothStep(initial, target, elapsed / duration);
                yield return null;
            }
            envelope = target;
            source.volume = envelope;
            if (target == 0f)
                source.Stop();
            fadeRoutine = null;
        }
    }
}
