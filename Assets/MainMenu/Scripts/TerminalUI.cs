using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SilentSuns.Menu
{
    // Small shared helpers for the new panels. Coordinates use the parent's size.
    public static class TerminalUI
    {
        public static readonly Color Blue = new Color32(8, 13, 106, 255);
        public static readonly Color Dark = new Color32(4, 7, 32, 255);
        public static readonly Color Ink = new Color32(219, 229, 255, 255);
        public static readonly Color Conflict = new Color32(235, 65, 74, 255);
        public static RectTransform Rect(string name, Transform parent, float x, float y, float w, float h)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.gameObject.layer = parent.gameObject.layer;
            rect.anchorMin = new Vector2(x, 1 - y - h);
            rect.anchorMax = new Vector2(x + w, 1 - y);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return rect;
        }

        public static Image Panel(string name, Transform parent, float x, float y, float w, float h, Color color)
        {
            var image = Rect(name, parent, x, y, w, h).gameObject.AddComponent<Image>();
            image.color = color;
            return image;
        }

        public static TMP_Text Text(string name, Transform parent, string text, float x, float y, float w, float h, float size = 24)
        {
            var label = Rect(name, parent, x, y, w, h).gameObject.AddComponent<TextMeshProUGUI>();
            label.font = TerminalUIAssets.Current.font;
            label.text = text;
            label.fontSize = size;
            label.color = Ink;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.raycastTarget = false;
            label.gameObject.AddComponent<TerminalTextScale>();
            return label;
        }

        public static TerminalButton Button(Transform parent, string text, float x, float y, float w, float h, UnityAction action)
        {
            var face = Panel(text, parent, x, y, w, h, Dark);
            Text("Label", face.transform, text, .04f, .03f, .92f, .94f);
            var button = face.gameObject.AddComponent<TerminalButton>();
            face.color = Color.white;
            button.targetGraphic = face;
            var colors = button.colors;
            colors.normalColor = Dark;
            colors.highlightedColor = Ink;
            colors.selectedColor = colors.highlightedColor;
            colors.pressedColor = Color.white;
            button.colors = colors;
            face.CrossFadeColor(Dark, 0, true, true);
            button.onClick.AddListener(action);
            return button;
        }

        public static ScrollRect Scroll(Transform parent, float x, float y, float w, float h, out RectTransform content)
        {
            var root = Rect("Scroll list", parent, x, y, w, h);
            var scroll = root.gameObject.AddComponent<TerminalScroll>();
            var view = Panel("Viewport", root, 0, 0, .96f, 1, new Color(0, 0, 0, .12f));
            view.gameObject.AddComponent<RectMask2D>();
            content = Rect("Content", view.transform, 0, 0, 1, 1);
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = Vector2.one;
            content.pivot = new Vector2(.5f, 1);
            scroll.viewport = view.rectTransform;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 50;
            var track = Panel("Scrollbar", root, .975f, 0, .025f, 1, Dark);
            var bar = track.gameObject.AddComponent<TerminalScrollbar>();
            var handle = Panel("Handle", track.transform, 0, 0, 1, 1, Ink);
            bar.handleRect = handle.rectTransform;
            bar.targetGraphic = handle;
            bar.direction = Scrollbar.Direction.BottomToTop;
            bar.navigation = new Navigation { mode = Navigation.Mode.None };
            scroll.verticalScrollbar = bar;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
            return scroll;
        }

        public static TMP_InputField Search(Transform parent, UnityAction<string> changed)
        {
            var face = Panel("Settings search", parent, .04f, .14f, .65f, .07f, Dark);
            var input = face.gameObject.AddComponent<TMP_InputField>();
            var label = Text("Text", face.transform, "", .025f, 0, .95f, 1);
            input.textViewport = face.rectTransform;
            input.textComponent = (TextMeshProUGUI)label;
            input.placeholder = Text("Placeholder", face.transform, "> SEARCH SETTINGS", .025f, 0, .95f, 1);
            input.characterLimit = 48;
            input.onValueChanged.AddListener(changed);
            return input;
        }
    }
}
