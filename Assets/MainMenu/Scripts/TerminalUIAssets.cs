using TMPro;
using UnityEngine;

namespace SilentSuns.Menu
{
    // The editor puts the shared font, artwork, and sound here for builds.
    public class TerminalUIAssets : ScriptableObject
    {
        public TMP_FontAsset font;
        public Sprite shuttle;
        public AudioClip disconnected;
        public TextAsset credits;
        public Material crtMaterial;
        public TerminalCRTSettings crtSettings;
        public string version;
        public string build;
        public string updated;
        private static TerminalUIAssets cached;
        public static TerminalUIAssets Current => cached != null ? cached : cached = Resources.Load<TerminalUIAssets>("TerminalUIAssets");
    }
}
