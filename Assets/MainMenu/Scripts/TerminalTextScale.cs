using TMPro;
using UnityEngine;

namespace SilentSuns.Menu
{
    public class TerminalTextScale : MonoBehaviour
    {
        private TMP_Text label;
        private float original;
        private void Awake()
        {
            label = GetComponent<TMP_Text>();
            original = label.fontSize;
        }
        private void OnEnable()
        {
            GamePreferences.Changed += Apply;
            Apply();
        }
        private void OnDisable()
        {
            GamePreferences.Changed -= Apply;
        }
        private void Apply()
        {
            label.fontSize = original * GamePreferences.TextScale;
            label.enableAutoSizing = true;
            label.fontSizeMax = original * GamePreferences.TextScale;
            label.fontSizeMin = original * .9f;
        }
        public static void Install(Transform root)
        {
            foreach (var text in root.GetComponentsInChildren<TMP_Text>(true))
                if (text.GetComponent<TerminalTextScale>() == null)
                    text.gameObject.AddComponent<TerminalTextScale>();
        }
    }
}
