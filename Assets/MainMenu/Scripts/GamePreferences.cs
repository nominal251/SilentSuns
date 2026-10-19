using UnityEngine;

namespace SilentSuns.Menu
{
    public static class GamePreferences
    {
        public static float FX => Mathf.Clamp01(PlayerPrefs.GetFloat("SilentSuns.Audio.FX", PlayerPrefs.GetFloat("SilentSuns.Menu.Volume", 0.65f)));
        public static float Music => Mathf.Clamp01(PlayerPrefs.GetFloat("SilentSuns.Audio.Music", 0.65f));
        public static float MouseSensitivity => Mathf.Clamp(PlayerPrefs.GetFloat("SilentSuns.Input.MouseSensitivity", 1f), 0.1f, 3f);
        public static bool ReducedMotion => PlayerPrefs.GetInt("SilentSuns.Menu.ReduceMotion", 0) == 1;
        public static bool PlayBoot => PlayerPrefs.GetInt("SilentSuns.Menu.PlayBoot", 1) == 1;

        // Save on each change so closing the game from either menu keeps the choice.
        public static void SetFX(float value)
        {
            SetFloat("SilentSuns.Audio.FX", Mathf.Clamp01(value));
            GameAudio.ApplyVolumes();
        }

        public static void SetMusic(float value)
        {
            SetFloat("SilentSuns.Audio.Music", Mathf.Clamp01(value));
            GameAudio.ApplyVolumes();
        }

        public static void SetMouseSensitivity(float value)
        {
            // PlayerMovement multiplies its normal sensitivity by this value.
            SetFloat("SilentSuns.Input.MouseSensitivity", Mathf.Clamp(value, 0.1f, 3f));
        }

        public static void SetReducedMotion(bool value)
        {
            SetBool("SilentSuns.Menu.ReduceMotion", value);
        }

        public static void SetPlayBoot(bool value)
        {
            SetBool("SilentSuns.Menu.PlayBoot", value);
        }

        private static void SetFloat(string key, float value)
        {
            PlayerPrefs.SetFloat(key, value);
            PlayerPrefs.Save();
        }

        private static void SetBool(string key, bool value)
        {
            PlayerPrefs.SetInt(key, value ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
}
