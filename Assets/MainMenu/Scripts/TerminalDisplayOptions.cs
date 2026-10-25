using UnityEngine;

namespace SilentSuns.Menu
{
    // Display modes preview immediately. Keep accepts the preview; Apply Settings saves it.
    public static class TerminalDisplayOptions
    {
        public static bool Pending
        {
            get; private set;
        }
        private static bool editing;
        private static Vector2Int originalSize, previewSize;
        private static FullScreenMode originalMode, previewMode;
        public static bool HasChanges => editing && (previewSize != originalSize || previewMode != originalMode);
        public static void BeginEdit()
        {
            if (editing) return;
            editing = true;
            originalSize = previewSize = new Vector2Int(Screen.width, Screen.height);
            originalMode = previewMode = Screen.fullScreenMode;
        }
        public static void ApplyEdits()
        {
            if (!HasChanges) return;
            Save(previewSize.x, previewSize.y, previewMode);
            originalSize = previewSize;
            originalMode = previewMode;
        }
        public static void DiscardEdits()
        {
            if (HasChanges) Screen.SetResolution(originalSize.x, originalSize.y, originalMode);
            previewSize = originalSize;
            previewMode = originalMode;
        }
        public static void EndEdit() { DiscardEdits(); editing = false; }
        private static void Save(int width, int height, FullScreenMode mode)
        {
            PlayerPrefs.SetInt("SilentSuns.Display.Width", width);
            PlayerPrefs.SetInt("SilentSuns.Display.Height", height);
            PlayerPrefs.SetInt("SilentSuns.Display.Mode", (int)mode);
            PlayerPrefs.Save();
        }
        public static void ResetMode()
        {
            // The project default is borderless at the display's native resolution.
            Resolution native = Screen.currentResolution;
            Apply(native.width, native.height, FullScreenMode.FullScreenWindow);
        }
        public static void Apply(int width, int height, FullScreenMode mode, System.Action<bool> finished = null)
        {
            if (Pending)
                return;
            int oldWidth = Screen.width, oldHeight = Screen.height;
            FullScreenMode oldMode = Screen.fullScreenMode;
            Pending = true;
            Screen.SetResolution(width, height, mode);
            float deadline = Time.unscaledTime + 15;
            bool accepted = false;
            var modal = TerminalModal.Open("DISPLAY MODE", "");
            modal.ClearDisplay = true;
            TerminalUI.Button(modal.Body, "KEEP THIS MODE", 0, .65f, .48f, .15f, () =>
            {
                accepted = true;
                if (editing) { previewSize = new Vector2Int(width, height); previewMode = mode; }
                else Save(width, height, mode);
                modal.Close();
            });
            TerminalUI.Button(modal.Body, "REVERT", .52f, .65f, .48f, .15f, modal.Close);
            modal.tick = () =>
            {
                string name = mode == FullScreenMode.Windowed ? "WINDOWED" : mode == FullScreenMode.FullScreenWindow ? "BORDERLESS" : "FULLSCREEN";
                modal.Message.text = name + " / " + width + " x " + height +
                    "\n\nKEEP THIS DISPLAY MODE?\nREVERTING IN " + Mathf.CeilToInt(deadline - Time.unscaledTime) + " SECONDS" +
                    (editing ? "\n\nThis is a preview. APPLY SETTINGS saves it." : "");
                if (Time.unscaledTime >= deadline)
                    modal.Close();
            };
            modal.closed = () =>
            {
                if (!accepted)
                    Screen.SetResolution(oldWidth, oldHeight, oldMode);
                Pending = false;
                finished?.Invoke(accepted);
            };
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Load()
        {
            Pending = false;
            editing = false;
            if (!Application.isEditor && PlayerPrefs.HasKey("SilentSuns.Display.Width"))
                Screen.SetResolution(PlayerPrefs.GetInt("SilentSuns.Display.Width"), PlayerPrefs.GetInt("SilentSuns.Display.Height"),
                    (FullScreenMode)PlayerPrefs.GetInt("SilentSuns.Display.Mode"));
            ApplyFrameRate();
        }
        public static void ApplyFrameRate()
        {
            QualitySettings.vSyncCount = (int)GamePreferences.Read("VSync", 1, 0, 1);
            Application.targetFrameRate = QualitySettings.vSyncCount > 0 ? -1 : (int)GamePreferences.Read("FrameCap", 120, 30, 240);
        }
    }
}
