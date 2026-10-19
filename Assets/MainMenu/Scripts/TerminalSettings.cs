using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SilentSuns.Menu
{
    public class TerminalSettings : MonoBehaviour
    {
        [Header("Controls")]
        [SerializeField] private Slider fxSlider;
        [SerializeField] private Slider musicSlider;
        [SerializeField] private Slider sensitivitySlider;
        [SerializeField] private TMP_Text fxValue;
        [SerializeField] private TMP_Text musicValue;
        [SerializeField] private TMP_Text sensitivityValue;
        [SerializeField] private Toggle motionToggle;
        [SerializeField] private Toggle bootToggle;
        [SerializeField] private TerminalMenu menu;

        public GameObject FirstControl => fxSlider.gameObject;

        private void OnEnable()
        {
            // Fill the controls from the saved settings without firing their change events.
            fxSlider.SetValueWithoutNotify(GamePreferences.FX);
            musicSlider.SetValueWithoutNotify(GamePreferences.Music);
            sensitivitySlider.SetValueWithoutNotify(GamePreferences.MouseSensitivity);
            motionToggle.SetIsOnWithoutNotify(GamePreferences.ReducedMotion);
            bootToggle.SetIsOnWithoutNotify(GamePreferences.PlayBoot);
            RefreshLabels();
        }

        // The sliders call these methods. GamePreferences saves the value and updates game audio.
        public void SetFX(float value)
        {
            GamePreferences.SetFX(value);
            RefreshLabels();
        }

        public void SetMusic(float value)
        {
            GamePreferences.SetMusic(value);
            RefreshLabels();
        }

        public void SetSensitivity(float value)
        {
            GamePreferences.SetMouseSensitivity(value);
            RefreshLabels();
        }

        public void SetBoot(bool value)
        {
            GamePreferences.SetPlayBoot(value);
        }
        public void SetMotion(bool value)
        {
            GamePreferences.SetReducedMotion(value);
            // Main-menu effects need the update now. The pause display reads the saved value each frame.
            if (menu != null)
                menu.SetReducedMotion(value);
        }

        private void RefreshLabels()
        {
            fxValue.text = Mathf.RoundToInt(GamePreferences.FX * 100f).ToString("000") + "%";
            musicValue.text = Mathf.RoundToInt(GamePreferences.Music * 100f).ToString("000") + "%";
            sensitivityValue.text = GamePreferences.MouseSensitivity.ToString("0.00") + "x";
        }
    }
}
