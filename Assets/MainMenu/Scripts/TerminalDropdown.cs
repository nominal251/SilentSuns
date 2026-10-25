using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace SilentSuns.Menu
{
    // Keep the list on the same canvas so it also works on the ship's screen.
    [DefaultExecutionOrder(-150)]
    public class TerminalDropdown : MonoBehaviour
    {
        public static TerminalDropdown Current { get; private set; }
        private static int closedFrame = -1;
        public static bool BlocksInput => Current != null || Time.frameCount <= closedFrame;
        private GameObject owner;

        public static void Open(TerminalButton button, RectTransform settingsFace,
            IReadOnlyList<string> options, int selected, Action<int> choose)
        {
            if (Current != null) Current.Close();
            var root = TerminalUI.Rect("Dropdown", settingsFace, 0, 0, 1, 1);
            var dropdown = root.gameObject.AddComponent<TerminalDropdown>();
            Current = dropdown;
            dropdown.owner = button.gameObject;
            var blocker = TerminalUI.Panel("Dismiss dropdown", root, 0, 0, 1, 1, Color.clear);
            blocker.gameObject.AddComponent<Button>().onClick.AddListener(dropdown.Close);

            // Anchor near the field, but keep the full list within the settings border.
            var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(settingsFace, button.transform);
            float x = Mathf.Clamp01((bounds.min.x - settingsFace.rect.xMin) / settingsFace.rect.width);
            float y = Mathf.Clamp((settingsFace.rect.yMax - bounds.min.y) / settingsFace.rect.height, .12f, .48f);
            float width = Mathf.Min(.62f, .98f - x);
            var border = TerminalUI.Panel("Dropdown border", root, x, y, width, .46f, TerminalUI.Ink);
            var face = TerminalUI.Panel("Dropdown options", border.transform, .004f, .008f, .992f, .984f, TerminalUI.Blue);
            var scroll = (TerminalScroll)TerminalUI.Scroll(face.transform, .015f, .02f, .97f, .96f, out var content);
            const float rowHeight = 52;
            content.sizeDelta = new Vector2(0, options.Count * rowHeight);
            var buttons = new List<TerminalButton>();
            for (int i = 0; i < options.Count; i++)
            {
                int index = i;
                var row = TerminalUI.Rect(options[i], content, 0, 0, 1, 1);
                row.anchorMin = new Vector2(0, 1);
                row.anchorMax = Vector2.one;
                row.pivot = new Vector2(.5f, 1);
                row.sizeDelta = new Vector2(0, rowHeight);
                row.anchoredPosition = Vector2.down * i * rowHeight;
                var option = TerminalUI.Button(row, (i == selected ? "> " : "  ") + options[i],
                    0, .02f, 1, .96f, () => { dropdown.Close(); choose(index); });
                option.gameObject.AddComponent<TerminalSettingHelp>();
                buttons.Add(option);
            }
            for (int i = 0; i < buttons.Count; i++)
                buttons[i].navigation = new Navigation
                {
                    mode = Navigation.Mode.Explicit,
                    selectOnUp = buttons[(i + buttons.Count - 1) % buttons.Count],
                    selectOnDown = buttons[(i + 1) % buttons.Count]
                };
            Canvas.ForceUpdateCanvases();
            scroll.ResetPosition();
            EventSystem.current?.SetSelectedGameObject(buttons[Mathf.Clamp(selected, 0, buttons.Count - 1)].gameObject);
        }

        private void Update()
        {
            if (Keyboard.current?.escapeKey.wasPressedThisFrame == true ||
                Gamepad.current?.buttonEast.wasPressedThisFrame == true)
                Close();
        }

        public void Close()
        {
            if (Current != this) return;
            Current = null;
            closedFrame = Time.frameCount;
            if (owner != null && owner.activeInHierarchy)
                EventSystem.current?.SetSelectedGameObject(owner);
            gameObject.SetActive(false);
            Destroy(gameObject);
        }

        private void OnDisable()
        {
            if (Current == this) { Current = null; closedFrame = Time.frameCount; }
        }
    }
}
