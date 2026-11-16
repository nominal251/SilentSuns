using System.Collections.Generic;
using UnityEngine;

namespace SilentSuns.Menu
{
    public static class GamePreferences
    {
        // Settings preview these values in memory. Only Apply writes them to disk.
        private static bool editing;
        private static readonly Dictionary<string, float> draft = new Dictionary<string, float>();
        public static bool HasChanges => draft.Count > 0;
        public static float FX => Mathf.Clamp01(Value("SilentSuns.Audio.FX", PlayerPrefs.GetFloat("SilentSuns.Menu.Volume", .65f)));
        public static float Music => Mathf.Clamp01(Value("SilentSuns.Audio.Music", .65f));
        public static float MouseSensitivity => Mathf.Clamp(Value("SilentSuns.Input.MouseSensitivity", 1), .1f, 3);
        public static bool ReducedMotion => Value("SilentSuns.Menu.ReduceMotion", 0, true) == 1;
        public static bool PlayBoot => Value("SilentSuns.Menu.PlayBoot", 1, true) == 1;
        public static float InterfaceVolume => Read("Interface", .65f, 0, 1);
        public static float Brightness => Read("Brightness", 0, -.6f, .6f);
        public static float Contrast => Read("Contrast", 1, .65f, 1.5f);
        public static float TextScale => Read("TextScale", 1, 1, 1.25f);
        public static float CRTIntensity => Read("CRTIntensity", 1, 0, 1.4f);
        public static float SafeArea => Read("SafeArea", .03f, 0, .12f);
        public static float ControllerSensitivity => Read("ControllerSensitivity", 1, .1f, 3);
        public static float DeadZone => Read("DeadZone", .15f, .05f, .45f);
        public static float CursorSize => Read("CursorSize", 1, .75f, 2);
        public static bool DarkCursor => Read("DarkCursor", 0, 0, 1) > 0;
        public static bool CursorOutline => Read("CursorOutline", 1, 0, 1) > 0;
        public static bool InvertLook => Read("InvertLook", 0, 0, 1) > 0;
        public static bool MuteUnfocused => Read("MuteUnfocused", 0, 0, 1) > 0;
        public static event System.Action Changed;

        public static float Read(string name, float fallback, float min, float max)
        {
            return Mathf.Clamp(Value("SilentSuns.Options." + name, fallback), min, max);
        }

        private static float Value(string key, float fallback, bool integer = false)
        {
            if (draft.TryGetValue(key, out float value)) return value;
            return integer ? PlayerPrefs.GetInt(key, (int)fallback) : PlayerPrefs.GetFloat(key, fallback);
        }

        public static float DefaultOption(string name)
        {
            switch (name)
            {
                case "Interface": return .65f;
                case "DeadZone": return .15f;
                case "SafeArea": return .03f;
                case "FrameCap": return 120;
                case "Contrast": case "TextScale": case "CRTIntensity": case "ControllerSensitivity":
                case "CursorSize": case "CursorOutline": case "VSync": return 1;
                default: return 0;
            }
        }

        public static void BeginEdit() { editing = true; }
        public static void ApplyEdits()
        {
            foreach (var entry in draft)
                if (entry.Key == "SilentSuns.Menu.ReduceMotion" || entry.Key == "SilentSuns.Menu.PlayBoot")
                    PlayerPrefs.SetInt(entry.Key, (int)entry.Value);
                else PlayerPrefs.SetFloat(entry.Key, entry.Value);
            draft.Clear();
            PlayerPrefs.Save();
            Changed?.Invoke();
        }
        public static void DiscardEdits()
        {
            draft.Clear();
            // Audio and display scripts also need to stop using the preview.
            GameAudio.ApplyVolumes();
            TerminalDisplayOptions.ApplyFrameRate();
            Changed?.Invoke();
        }
        public static void EndEdit() { editing = false; DiscardEdits(); }

        public static void SetOption(string name, float value)
        {
            SetFloat("SilentSuns.Options." + name, value);
            GameAudio.ApplyVolumes();
        }

        public static void ComfortPreset(bool comfortable)
        {
            SetReducedMotion(comfortable);
            SetOption("CRTIntensity", comfortable ? .35f : 1);
            SetOption("TextScale", comfortable ? 1.15f : 1);
        }

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
            if (editing)
            {
                float fallback = key.StartsWith("SilentSuns.Options.") ? DefaultOption(key.Substring("SilentSuns.Options.".Length)) :
                    key == "SilentSuns.Input.MouseSensitivity" ? 1 :
                    key == "SilentSuns.Audio.FX" ? PlayerPrefs.GetFloat("SilentSuns.Menu.Volume", .65f) : .65f;
                if (Mathf.Approximately(value, PlayerPrefs.GetFloat(key, fallback))) draft.Remove(key);
                else draft[key] = value;
                Changed?.Invoke();
                return;
            }
            PlayerPrefs.SetFloat(key, value);
            PlayerPrefs.Save();
            Changed?.Invoke();
        }

        private static void SetBool(string key, bool value)
        {
            if (editing)
            {
                int number = value ? 1 : 0;
                if (number == PlayerPrefs.GetInt(key, key == "SilentSuns.Menu.PlayBoot" ? 1 : 0)) draft.Remove(key);
                else draft[key] = number;
                Changed?.Invoke();
                return;
            }
            PlayerPrefs.SetInt(key, value ? 1 : 0);
            PlayerPrefs.Save();
            Changed?.Invoke();
        }
    }
}
