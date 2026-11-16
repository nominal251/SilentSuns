using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace SilentSuns.Menu
{
    public static class TerminalCalibration
    {
        public static Slider Slider(Transform parent, string title, float y, float value, float min, float max, System.Action<float> change)
        {
            var label = TerminalUI.Text(title, parent, title, 0, y, .65f, .09f);
            var number = TerminalUI.Text("Value", parent, value.ToString("0.00"), .75f, y, .25f, .09f);
            var track = TerminalUI.Panel("Track", parent, .02f, y + .10f, .94f, .025f, TerminalUI.Dark);
            var slider = track.gameObject.AddComponent<Slider>();
            var area = TerminalUI.Rect("Handle area", track.transform, .02f, -1, .96f, 3);
            var handle = TerminalUI.Panel("Handle", area, 0, 0, .02f, 1, TerminalUI.Ink);
            handle.rectTransform.sizeDelta = new Vector2(16, 0);
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.minValue = min;
            slider.maxValue = max;
            slider.SetValueWithoutNotify(value);
            slider.onValueChanged.AddListener(v => { number.text = v.ToString("0.00"); change(v); });
            return slider;
        }

        public static void Display()
        {
            var modal = TerminalModal.Open("DISPLAY CALIBRATION", "");
            modal.ClearDisplay = true;
            var black = TerminalUI.Panel("Black reference", modal.Body, 0, 0, .48f, .37f, Color.black);
            for (int i = 0; i < 4; i++)
            {
                float grey = .015f + i * .025f;
                TerminalUI.Panel("Shadow " + i, black.transform, .08f + i * .23f, .15f, .15f, .7f, new Color(grey, grey, grey));
            }
            var blue = TerminalUI.Panel("Blue reference", modal.Body, .52f, 0, .48f, .37f, new Color32(0, 0, 170, 255));
            TerminalUI.Text("White reference", blue.transform, "ARCA/OS\nWHITE TEXT / BLUE FIELD", .06f, .08f, .88f, .84f, 27);
            TerminalUI.Text("Instructions", modal.Body, "Make the darkest shapes barely visible. White letters should stay distinct.", 0, .39f, 1, .15f);
            var brightness = Slider(modal.Body, "BRIGHTNESS", .55f, GamePreferences.Brightness, -.6f, .6f, v => GamePreferences.SetOption("Brightness", v));
            var contrast = Slider(modal.Body, "CONTRAST", .72f, GamePreferences.Contrast, .65f, 1.5f, v => GamePreferences.SetOption("Contrast", v));
            TerminalUI.Button(modal.Body, "RESET TO DEFAULT", 0, .9f, .55f, .11f, () =>
            {
                brightness.value = 0;
                contrast.value = 1;
            });
            // TerminalPauseDisplay applies brightness and contrast to this whole panel.
        }

        public static void Boundaries()
        {
            var modal = TerminalModal.Open("SCREEN BOUNDARIES", "KEEP ALL FOUR CORNER BRACKETS VISIBLE\nClose this panel, then APPLY SETTINGS to save.");
            modal.ClearDisplay = true;
            // These guides belong to the screen, not the popup's camera or its safe-area scaling.
            var overlay = new GameObject("Screen boundary guides", typeof(Canvas), typeof(CanvasScaler));
            var canvas = overlay.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 860;
            var scaler = overlay.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600, 900);
            scaler.matchWidthOrHeight = .5f;
            var markers = TerminalUI.Rect("Boundary preview", overlay.transform, 0, 0, 1, 1);
            // Keep the complete stroke inside the viewport, even at zero margin.
            markers.offsetMin = new Vector2(2, 2);
            markers.offsetMax = new Vector2(-2, -2);
            foreach (Vector2 corner in new[] { Vector2.zero, Vector2.right, Vector2.up, Vector2.one })
            {
                var mark = TerminalUI.Rect("Corner", markers, 0, 0, 1, 1);
                mark.anchorMin = mark.anchorMax = corner;
                mark.pivot = corner;
                mark.sizeDelta = new Vector2(34, 34);
                mark.anchoredPosition = Vector2.zero;
                TerminalUI.Panel("Horizontal", mark, 0, (1 - corner.y) * .9f, 1, .1f, TerminalUI.Ink).raycastTarget = false;
                TerminalUI.Panel("Vertical", mark, corner.x * .9f, 0, .1f, 1, TerminalUI.Ink).raycastTarget = false;
            }
            var margin = Slider(modal.Body, "EDGE MARGIN", .55f, GamePreferences.SafeArea, 0, .12f, v => GamePreferences.SetOption("SafeArea", v));
            TerminalUI.Button(modal.Body, "RESET TO DEFAULT", 0, .8f, .55f, .13f, () => margin.value = .03f);
            modal.closed += () => { overlay.SetActive(false); Object.Destroy(overlay); };
            modal.tick = () =>
            {
                markers.anchorMin = Vector2.one * GamePreferences.SafeArea;
                markers.anchorMax = Vector2.one * (1 - GamePreferences.SafeArea);
            };
        }

        public static void Controller()
        {
            var modal = TerminalModal.Open("CONTROLLER CALIBRATION", "");
            modal.ClearDisplay = true;
            var status = TerminalUI.Text("Status", modal.Body, "", 0, 0, 1, .13f);
            RectTransform[] dots = new RectTransform[2];
            RectTransform[] zones = new RectTransform[2];
            for (int i = 0; i < 2; i++)
            {
                var box = TerminalUI.Panel(i == 0 ? "Left stick" : "Right stick", modal.Body, .04f + i * .51f, .18f, .40f, .38f, TerminalUI.Dark);
                TerminalUI.Panel("Horizontal axis", box.transform, 0, .497f, 1, .006f, new Color(.25f, .3f, .6f));
                TerminalUI.Panel("Vertical axis", box.transform, .497f, 0, .006f, 1, new Color(.25f, .3f, .6f));
                zones[i] = TerminalUI.Panel("Dead zone", box.transform, .4f, .4f, .2f, .2f, new Color(.15f, .25f, .55f, .5f)).rectTransform;
                dots[i] = TerminalUI.Panel("Stick position", box.transform, .48f, .48f, .04f, .04f, TerminalUI.Ink).rectTransform;
                TerminalUI.Text("Stick label", modal.Body, i == 0 ? "LEFT / MOVE" : "RIGHT / LOOK", .04f + i * .51f, .58f, .4f, .08f);
            }
            Slider(modal.Body, "DEAD ZONE", .75f, GamePreferences.DeadZone, .05f, .45f, v => GamePreferences.SetOption("DeadZone", v));
            modal.tick = () =>
            {
                var pad = Gamepad.current;
                status.text = pad == null ? "NO CONTROLLER CONNECTED" : "MOVE THE STICKS / RELEASE TO CHECK DRIFT";
                for (int i = 0; i < 2; i++)
                {
                    Vector2 input = pad == null ? Vector2.zero : i == 0 ? pad.leftStick.ReadUnprocessedValue() : pad.rightStick.ReadUnprocessedValue();
                    Vector2 center = (input + Vector2.one) * .5f;
                    dots[i].anchorMin = center - Vector2.one * .025f;
                    dots[i].anchorMax = center + Vector2.one * .025f;
                    zones[i].anchorMin = Vector2.one * (.5f - GamePreferences.DeadZone * .5f);
                    zones[i].anchorMax = Vector2.one * (.5f + GamePreferences.DeadZone * .5f);
                }
            };
        }

    }
}
