using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace SilentSuns.Menu
{
    // The startup screen signals this component. It borrows nearby lights only for the interruption.
    public class MasterComputerInterference : MonoBehaviour
    {
        [SerializeField] private MasterComputerDisplay display;
        [SerializeField] private AudioClip sound;
        private MasterComputerUI ui;
        private AudioSource source;
        private readonly List<Light> lights = new List<Light>();
        private readonly List<float> brightness = new List<float>();
        private bool active;
        private float started, nextFlicker;

        public void Configure(MasterComputerDisplay screen, AudioClip clip) { display = screen; sound = clip; }

        private void Update()
        {
            if (ui == null && display != null && display.UI != null)
            {
                ui = display.UI;
                ui.IntrusionChanged += SetActive;
            }
            if (!active) return;
            float elapsed = Time.unscaledTime - started;
            // Give the reveal its attack at the same volume cap, then soften the tail.
            if (source != null)
                source.volume = .18f * Mathf.Clamp01((MasterComputerUI.IntrusionDuration - elapsed) / .35f);
            if (GamePreferences.ReducedMotion) { RestoreLights(); return; }
            if (Time.unscaledTime < nextFlicker) return;
            nextFlicker = Time.unscaledTime + Random.Range(.19f, .36f);
            float amount = Random.Range(.03f, .18f);
            for (int i = 0; i < lights.Count; i++)
                if (lights[i] != null) lights[i].intensity = brightness[i] * amount;
        }

        private void SetActive(bool value)
        {
            active = value;
            if (!value)
            {
                if (source != null) source.Stop();
                RestoreLights();
                return;
            }
            started = Time.unscaledTime;
            nextFlicker = started + .18f;
            if (!GamePreferences.ReducedMotion)
            {
                foreach (var light in FindObjectsByType<Light>(FindObjectsSortMode.None))
                {
                    if (!light.isActiveAndEnabled || light.type == LightType.Directional ||
                        Vector3.Distance(light.transform.position, transform.position) > 12 || light.transform.IsChildOf(transform)) continue;
                    lights.Add(light); brightness.Add(light.intensity);
                    light.intensity *= .015f;
                }
            }
            if (source == null)
            {
                source = gameObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.loop = false;
                source.spatialBlend = 0;
                var mixer = Resources.Load<AudioMixer>("SilentSunsAudio");
                var groups = mixer == null ? null : mixer.FindMatchingGroups("FX");
                if (groups != null && groups.Length > 0) source.outputAudioMixerGroup = groups[0];
            }
            source.clip = sound;
            source.volume = .18f;
            if (sound != null) source.Play();
        }

        private void RestoreLights()
        {
            for (int i = 0; i < lights.Count; i++)
                if (lights[i] != null) lights[i].intensity = brightness[i];
            lights.Clear(); brightness.Clear();
        }

        private void OnDisable() => SetActive(false);
        private void OnDestroy()
        {
            SetActive(false);
            if (ui != null) ui.IntrusionChanged -= SetActive;
        }
    }
}
