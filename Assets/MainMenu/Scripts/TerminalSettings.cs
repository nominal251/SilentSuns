using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace SilentSuns.Menu
{
    public class TerminalSettings : MonoBehaviour
    {
        // Retained for the scene's existing UnityEvent connections.
        [SerializeField] private Slider fxSlider, musicSlider, sensitivitySlider;
        [SerializeField] private TMP_Text fxValue, musicValue, sensitivityValue;
        [SerializeField] private Toggle motionToggle, bootToggle;
        [SerializeField] private TerminalMenu menu;
        private RectTransform content;
        private RectTransform settingsFace;
        private RectTransform scrollArea, bindingTabs;
        private TerminalButton keyboardTab, controllerTab;
        private bool controllerBindings;
        private TerminalSlider frameSlider;
        private TMP_Text frameValue;
        private CanvasGroup frameRow;
        private TerminalButton modeButton, resolutionButton;
        private TMP_Text description, heading;
        private TMP_Text resetLabel;
        private TMP_Text changesLabel;
        private TerminalButton applyButton;
        private string conflict = "";
        private readonly List<Action> refreshBindingLabels = new List<Action>();
        private float nextStatusCheck;
        public bool HasChanges => GamePreferences.HasChanges || TerminalBindings.HasChanges || TerminalDisplayOptions.HasChanges;
        public bool CanApply => !TerminalBindings.Listening && !TerminalDisplayOptions.Pending && TerminalBindings.ConflictWarning().Length == 0;
        private TMP_InputField search;
        private GameObject first;
        private string tab = "GENERAL";
        private readonly List<Row> rows = new List<Row>();
        private readonly Dictionary<string, GameObject> tabsByName = new Dictionary<string, GameObject>();
        private bool built;
        private int resolutionIndex;
        private FullScreenMode displayMode;
        private readonly List<Vector2Int> resolutions = new List<Vector2Int>();
        private struct Row { public string name, tab, help; public Action<RectTransform> draw; }
        public GameObject FirstControl { get { Build(); return tabsByName.TryGetValue(tab, out var button) ? button : first; } }
        public string CurrentTab => tab;
        private static TerminalSettings openSettings;
        // The display scripts read this without changing the player's saved effects settings.
        public static bool EffectsSuspended => openSettings != null ||
            (TerminalModal.Current != null && TerminalModal.Current.ClearDisplay);

        private void OnEnable()
        {
            openSettings = this;
            GamePreferences.BeginEdit();
            TerminalBindings.BeginEdit();
            TerminalDisplayOptions.BeginEdit();
            Build();
            ReadDisplayMode();
            ShowTab(tab);
            GamePreferences.Changed += UpdateFrameLimit;
            Application.wantsToQuit += ConfirmApplicationExit;
        }
        private void OnDisable()
        {
            GamePreferences.Changed -= UpdateFrameLimit;
            Application.wantsToQuit -= ConfirmApplicationExit;
            if (TerminalDropdown.Current != null) TerminalDropdown.Current.Close();
            TerminalBindings.Cancel();
            if (openSettings == this)
            {
                openSettings = null;
                GamePreferences.EndEdit();
                TerminalBindings.EndEdit();
                TerminalDisplayOptions.EndEdit();
            }
        }
        private void Update()
        {
            if (!built) return;
            if (Time.unscaledTime >= nextStatusCheck)
            {
                nextStatusCheck = Time.unscaledTime + .15f;
                conflict = TerminalBindings.ConflictWarning();
                foreach (var refresh in refreshBindingLabels) refresh();
                applyButton.interactable = HasChanges && CanApply;
                changesLabel.text = conflict.Length > 0 ? conflict : HasChanges ?
                    "UNAPPLIED CHANGES\nPreviewing your changes.\nAPPLY SETTINGS to save." : "SETTINGS UP TO DATE";
                // Give conflicts the whole help panel instead of repeating help underneath them.
                description.gameObject.SetActive(conflict.Length == 0);
                changesLabel.rectTransform.anchorMax = new Vector2(.96f, conflict.Length > 0 ? .64f : .38f);
            }
            changesLabel.color = conflict.Length > 0 ? TerminalUI.Conflict : TerminalUI.Ink;
        }
        public bool ApplyChanges()
        {
            if (!CanApply) return false;
            TerminalBindings.ApplyEdits();
            TerminalDisplayOptions.ApplyEdits();
            GamePreferences.ApplyEdits();
            nextStatusCheck = 0;
            return true;
        }
        public void DiscardChanges()
        {
            TerminalBindings.DiscardEdits();
            TerminalDisplayOptions.DiscardEdits();
            GamePreferences.DiscardEdits();
            nextStatusCheck = 0;
        }
        public void RequestLeave(Action leave)
        {
            if (!HasChanges) { leave(); return; }
            var modal = TerminalModal.Open("UNAPPLIED SETTINGS", "APPLY SETTINGS OR DISCARD CHANGES?\nESC / X RETURNS TO SETTINGS.");
            modal.ClearDisplay = true;
            var apply = TerminalUI.Button(modal.Body, "APPLY SETTINGS", 0, .65f, .48f, .15f, () =>
            {
                if (!ApplyChanges()) return;
                modal.Close();
                leave();
            });
            TerminalUI.Button(modal.Body, "DISCARD CHANGES", .52f, .65f, .48f, .15f, () =>
            {
                DiscardChanges();
                modal.Close();
                leave();
            });
            string warning = TerminalBindings.ConflictWarning();
            apply.interactable = CanApply;
            if (warning.Length > 0)
            {
                modal.Message.text = warning;
                modal.Message.color = TerminalUI.Conflict;
            }
        }
        private bool ConfirmApplicationExit()
        {
            if (!HasChanges) return true;
            RequestLeave(() => Application.Quit());
            return false;
        }
        public void SetFX(float value) { GamePreferences.SetFX(value); }
        public void SetMusic(float value) { GamePreferences.SetMusic(value); }
        public void SetSensitivity(float value) { GamePreferences.SetMouseSensitivity(value); }
        public void SetBoot(bool value) { GamePreferences.SetPlayBoot(value); }
        public void SetMotion(bool value)
        {
            GamePreferences.SetReducedMotion(value);
            if (menu != null) menu.SetReducedMotion(value);
        }

        private void Build()
        {
            if (built || TerminalUIAssets.Current == null) return;
            built = true;
            foreach (Transform child in transform) child.gameObject.SetActive(false);
            TerminalUI.Panel("Settings border", transform, .023f, .106f, .954f, .798f, TerminalUI.Ink);
            var face = TerminalUI.Panel("Tabbed settings", transform, .025f, .11f, .95f, .79f, TerminalUI.Blue);
            settingsFace = face.rectTransform;
            heading = TerminalUI.Text("Title", face.transform, "TERMINAL SETTINGS", .04f, .015f, .8f, .08f, 30);
            search = TerminalUI.Search(face.transform, _ => Refresh());
            string[] tabs = { "GENERAL", "VIDEO", "AUDIO", "KEYBINDS" };
            for (int i = 0; i < tabs.Length; i++)
            {
                string item = tabs[i];
                var button = TerminalUI.Button(face.transform, item, .04f + i * .23f, .24f, .215f, .08f, () => ShowTab(item));
                tabsByName[item] = button.gameObject;
                if (i == 0) first = button.gameObject;
            }
            scrollArea = (RectTransform)TerminalUI.Scroll(face.transform, .04f, .35f, .62f, .53f, out content).transform;
            bindingTabs = TerminalUI.Rect("Binding devices", face.transform, .04f, .35f, .62f, .08f);
            keyboardTab = TerminalUI.Button(bindingTabs, "KEYBOARD / MOUSE", 0, 0, .49f, 1, () => ShowBindingDevice(false));
            controllerTab = TerminalUI.Button(bindingTabs, "CONTROLLER", .51f, 0, .49f, 1, () => ShowBindingDevice(true));
            foreach (var button in new[] { keyboardTab, controllerTab })
                TerminalUI.Panel("Selected device", button.transform, 0, .94f, 1, .06f, TerminalUI.Ink).raycastTarget = false;
            description = TerminalUI.Text("Setting description", face.transform, "Choose a setting. APPLY SETTINGS saves your changes.", .7f, .36f, .26f, .24f, 23);
            description.alignment = TextAlignmentOptions.TopLeft;
            changesLabel = TerminalUI.Text("Changes status", face.transform, "", .7f, .62f, .26f, .27f, 19);
            changesLabel.alignment = TextAlignmentOptions.TopLeft;
            changesLabel.enableAutoSizing = true;
            changesLabel.fontSizeMin = 14;
            changesLabel.fontSizeMax = 19;
            TerminalUI.Button(face.transform, "BACK [ESC]", .04f, .91f, .25f, .075f, Back);
            applyButton = TerminalUI.Button(face.transform, "APPLY SETTINGS", .32f, .91f, .34f, .075f, () => ApplyChanges());
            resetLabel = TerminalUI.Button(face.transform, "RESET TAB", .7f, .91f, .26f, .075f, AskReset).transform.Find("Label").GetComponent<TMP_Text>();
            DefineRows();
        }

        private void Back()
        {
            if (menu != null) menu.BackToMenu();
            else GetComponentInParent<TerminalPause>().CloseSettings();
        }

        public void ShowTab(string name)
        {
            tab = name;
            if (!built) return;
            search.SetTextWithoutNotify("");
            Refresh();
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(FirstControl);
        }

        public void ShowBindingDevice(bool controller)
        {
            controllerBindings = controller;
            Refresh();
            EventSystem.current?.SetSelectedGameObject(controller ? controllerTab.gameObject : keyboardTab.gameObject);
        }

        private void Add(string area, string name, string help, Action<RectTransform> draw)
        {
            rows.Add(new Row { tab = area, name = name, help = help, draw = draw });
        }
        private void Option(string area, string name, string help, string key, float fallback, float min, float max, string suffix = "")
        {
            Add(area, name, help, row => MakeSlider(row, name, GamePreferences.Read(key, fallback, min, max), min, max,
                value => GamePreferences.SetOption(key, value), suffix));
        }
        private void Flag(string area, string name, string help, Func<bool> read, Action<bool> save)
        {
            Add(area, name, help, row =>
            {
                TerminalButton button = null;
                button = TerminalUI.Button(row, name + "   " + (read() ? "[ON]" : "[OFF]"), 0, .08f, 1, .84f, () =>
                {
                    save(!read());
                    button.transform.Find("Label").GetComponent<TMP_Text>().text = name + "   " + (read() ? "[ON]" : "[OFF]");
                });
            });
        }
        private void ActionRow(string area, string name, string help, UnityEngine.Events.UnityAction action)
        {
            Add(area, name, help, row => TerminalUI.Button(row, name + " >", 0, .08f, 1, .84f, action));
        }

        private void DefineRows()
        {
            Flag("GENERAL", "PLAY DIAGNOSTICS ON START", "Show the ship diagnostic log on the next startup. SPACE skips the current log.", () => GamePreferences.PlayBoot, SetBoot);
            Add("GENERAL", "MOUSE SENSITIVITY", "Changes mouse look speed during gameplay.", row => MakeSlider(row, "MOUSE SENSITIVITY", GamePreferences.MouseSensitivity, .1f, 3, SetSensitivity, "x"));
            Option("GENERAL", "CONTROLLER SENSITIVITY", "Changes right-stick look speed during gameplay.", "ControllerSensitivity", 1, .1f, 3, "x");
            Flag("GENERAL", "INVERT LOOK", "Reverse vertical look for mouse and controller.", () => GamePreferences.InvertLook, v => GamePreferences.SetOption("InvertLook", v ? 1 : 0));
            Option("GENERAL", "CONTROLLER DEAD ZONE", "Ignore small stick movements near the center. Increase this if your controller drifts.", "DeadZone", .15f, .05f, .45f);
            ActionRow("GENERAL", "CONTROLLER CALIBRATION", "Watch both sticks and adjust the dead zone. Stick centers should remain still when untouched.", () => OpenCalibration(TerminalCalibration.Controller));
            Option("GENERAL", "CURSOR SIZE", "Size of the pointer while using menus. Does not change the gameplay crosshair.", "CursorSize", 1, .75f, 2, "x");
            Flag("GENERAL", "DARK CURSOR", "Switch between a light and dark menu pointer.", () => GamePreferences.DarkCursor, v => GamePreferences.SetOption("DarkCursor", v ? 1 : 0));
            Flag("GENERAL", "CURSOR OUTLINE", "Add a contrasting edge to the menu pointer.", () => GamePreferences.CursorOutline, v => GamePreferences.SetOption("CursorOutline", v ? 1 : 0));

            Option("VIDEO", "BRIGHTNESS", "Adjust overall display exposure. Use calibration to keep dark details visible.", "Brightness", 0, -.6f, .6f);
            Option("VIDEO", "CONTRAST", "Adjust the difference between dark and light areas.", "Contrast", 1, .65f, 1.5f, "x");
            Option("VIDEO", "TEXT SIZE", "Enlarge menu text. Long settings lists scroll to keep controls accessible.", "TextScale", 1, 1, 1.25f, "x");
            Option("VIDEO", "CRT INTENSITY", "Adjust scanlines, phosphor texture, glow, and interference.", "CRTIntensity", 1, 0, 1.4f, "x");
            Flag("VIDEO", "REDUCED EFFECTS", "Use smooth fades and suppress animated CRT faults and light flickers.", () => GamePreferences.ReducedMotion, SetMotion);
            ActionRow("VIDEO", "COMFORT PRESET", "Larger text, gentler CRT texture, and reduced effects. You can still adjust each setting.", () => { GamePreferences.ComfortPreset(true); Refresh(); });
            ActionRow("VIDEO", "ORIGINAL PRESET", "Restore the standard text size and full CRT presentation.", () => { GamePreferences.ComfortPreset(false); Refresh(); });
            ActionRow("VIDEO", "DISPLAY CALIBRATION", "Adjust brightness and contrast against black shapes, white text, and a blue panel.", () => OpenCalibration(TerminalCalibration.Display));
            ActionRow("VIDEO", "SCREEN BOUNDARIES", "Move interface content away from the edges. Keep all four corner markers visible.", () => OpenCalibration(TerminalCalibration.Boundaries));
            Flag("VIDEO", "VSYNC", "Match screen refresh to reduce tearing. When enabled, the display controls the frame rate.", () => QualitySettings.vSyncCount > 0, v => { GamePreferences.SetOption("VSync", v ? 1 : 0); TerminalDisplayOptions.ApplyFrameRate(); });
            Add("VIDEO", "FRAME LIMIT", "Maximum frame rate when VSync is off. VSync uses the monitor's refresh rate instead.", row =>
            {
                frameSlider = MakeSlider(row, "FRAME LIMIT", GamePreferences.Read("FrameCap", 120, 30, 240), 30, 240,
                    v => { GamePreferences.SetOption("FrameCap", Mathf.RoundToInt(v)); TerminalDisplayOptions.ApplyFrameRate(); }, " FPS");
                frameSlider.wholeNumbers = true;
                frameValue = row.Find("Value").GetComponent<TMP_Text>();
                frameRow = row.gameObject.AddComponent<CanvasGroup>();
                UpdateFrameLimit();
            });
            displayMode = Screen.fullScreenMode;
            foreach (var res in Screen.resolutions)
            {
                var size = new Vector2Int(res.width, res.height);
                if (!resolutions.Contains(size) && size.x >= 800 && size.y >= 600) resolutions.Add(size);
            }
            var current = new Vector2Int(Screen.width, Screen.height);
            if (!resolutions.Contains(current)) resolutions.Add(current);
            resolutionIndex = resolutions.IndexOf(current);
            Add("VIDEO", "WINDOW MODE", "Choosing a mode applies it immediately. Confirm within 15 seconds or it reverts.", row =>
            {
                modeButton = TerminalUI.Button(row, ModeName() + " [v]", 0, .08f, 1, .84f, () =>
                {
                    FullScreenMode[] modes = { FullScreenMode.Windowed, FullScreenMode.FullScreenWindow, FullScreenMode.ExclusiveFullScreen };
                    TerminalDropdown.Open(modeButton, settingsFace, new[] { "WINDOWED", "BORDERLESS", "FULLSCREEN" },
                        Array.IndexOf(modes, displayMode), i => ApplyDisplay(resolutionIndex, modes[i]));
                });
            });
            Add("VIDEO", "RESOLUTION", "Choose from your display's supported resolutions. Applies immediately with a 15-second confirmation.", row =>
            {
                resolutionButton = TerminalUI.Button(row, ResolutionName() + " [v]", 0, .08f, 1, .84f, () =>
                {
                    var labels = resolutions.ConvertAll(size => size.x + " x " + size.y);
                    TerminalDropdown.Open(resolutionButton, settingsFace, labels, resolutionIndex,
                        i => ApplyDisplay(i, displayMode));
                });
            });
            Add("AUDIO", "GAMEPLAY SOUND EFFECTS", "Gameplay effects and instrument sounds. Menu sounds have their own slider.", row => MakeSlider(row, "GAMEPLAY SFX", GamePreferences.FX, 0, 1, SetFX, "%"));
            Add("AUDIO", "MUSIC", "Music volume, including the main-menu track.", row => MakeSlider(row, "MUSIC", GamePreferences.Music, 0, 1, SetMusic, "%"));
            Option("AUDIO", "INTERFACE SOUNDS", "Menu clicks, typing, terminal hum, and controller notices.", "Interface", .65f, 0, 1, "%");
            Flag("AUDIO", "MUTE WHEN UNFOCUSED", "Silence the game while you use another window.", () => GamePreferences.MuteUnfocused, v => GamePreferences.SetOption("MuteUnfocused", v ? 1 : 0));
        }
        private string ModeName() => "WINDOW MODE: " + (displayMode == FullScreenMode.Windowed ? "WINDOWED" : displayMode == FullScreenMode.FullScreenWindow ? "BORDERLESS" : "FULLSCREEN");
        private string ResolutionName() => "RESOLUTION: " + resolutions[resolutionIndex].x + " x " + resolutions[resolutionIndex].y;
        private void ReadDisplayMode()
        {
            if (!built) return;
            displayMode = Screen.fullScreenMode;
            var current = new Vector2Int(Screen.width, Screen.height);
            if (!resolutions.Contains(current)) resolutions.Add(current);
            resolutionIndex = resolutions.IndexOf(current);
        }
        private void OpenCalibration(Action open)
        {
            open();
            TerminalModal.Current.closed += () =>
            {
                if (this == null || !isActiveAndEnabled) return;
                // Calibration edits the same draft. Update the sliders behind it without jumping the list.
                Vector2 position = content.anchoredPosition;
                Refresh();
                content.anchoredPosition = position;
            };
        }
        private void ApplyDisplay(int index, FullScreenMode mode)
        {
            int previousIndex = resolutionIndex;
            FullScreenMode previousMode = displayMode;
            resolutionIndex = index;
            displayMode = mode;
            UpdateDisplayLabels();
            TerminalDisplayOptions.Apply(resolutions[index].x, resolutions[index].y, mode, accepted =>
            {
                if (!accepted) { resolutionIndex = previousIndex; displayMode = previousMode; }
                if (this != null) UpdateDisplayLabels();
            });
        }
        private void UpdateDisplayLabels()
        {
            if (modeButton != null) modeButton.transform.Find("Label").GetComponent<TMP_Text>().text = ModeName() + " [v]";
            if (resolutionButton != null) resolutionButton.transform.Find("Label").GetComponent<TMP_Text>().text = ResolutionName() + " [v]";
        }
        private void UpdateFrameLimit()
        {
            if (frameSlider == null) return;
            bool vsync = GamePreferences.Read("VSync", 1, 0, 1) > 0;
            frameSlider.interactable = !vsync;
            frameRow.alpha = vsync ? .45f : 1;
            frameValue.text = vsync ? "VSYNC" : Mathf.RoundToInt(frameSlider.value) + " FPS";
        }

        private void Refresh()
        {
            if (!built) return;
            var selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            if (selected != null && selected.transform.IsChildOf(content)) EventSystem.current.SetSelectedGameObject(first);
            foreach (Transform child in content) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
            refreshBindingLabels.Clear();
            var visible = new List<Row>();
            int sectionCount = 0;
            string query = search.text.Trim();
            bool keybinds = tab == "KEYBINDS";
            bindingTabs.gameObject.SetActive(keybinds);
            keyboardTab.transform.Find("Selected device").gameObject.SetActive(!controllerBindings);
            controllerTab.transform.Find("Selected device").gameObject.SetActive(controllerBindings);
            scrollArea.anchorMax = new Vector2(.66f, keybinds ? .54f : .65f);
            foreach (var row in rows)
                if (query.Length > 0 ? (row.name + " " + row.help + " " + row.tab).IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 : row.tab == tab) visible.Add(row);
            if (tab == "KEYBINDS" || query.Length > 0)
            {
                foreach (string section in new[] { "ON FOOT", "IN SHIP", "ON FOOT + IN SHIP" })
                {
                    var sectionRows = new List<Row>();
                    foreach (var pair in TerminalBindings.Editable())
                    {
                        var action = pair.action; int index = pair.index;
                        string context = TerminalBindings.MovementContext(action, index);
                        // Controls without a movement restriction, like Interact, also work in both locations.
                        if (context.Length == 0) context = "ON FOOT + IN SHIP";
                        if (context != section) continue;
                        bool pad = TerminalBindings.IsController(action, index);
                        if (keybinds && pad != controllerBindings) continue;
                        string name = (pad ? "PAD / " : "KEY / ") + TerminalBindings.Label(action, index);
                        if (query.Length > 0 && name.IndexOf(query, StringComparison.OrdinalIgnoreCase) < 0) continue;
                        sectionRows.Add(new Row
                        {
                            tab = "KEYBINDS",
                            name = name,
                            help = context == "ON FOOT + IN SHIP" ?
                                TerminalBindings.Description(action) :
                                "Select to rebind. ESC cancels. Resolve conflicting actions before applying.",
                            draw = row =>
                            {
                                var button = TerminalUI.Button(row, name + " : " + action.GetBindingDisplayString(index), 0, .08f, 1, .84f, () =>
                                {
                                    description.text = "PRESS A CONTROL...\nESC CANCELS / 10 SECOND TIMEOUT";
                                    TerminalBindings.Rebind(action, index, message => { Refresh(); description.text = message; });
                                });
                                var label = button.transform.Find("Label").GetComponent<TMP_Text>();
                                Action updateLabel = () =>
                                {
                                    string key = action.GetBindingDisplayString(index);
                                    if (TerminalBindings.Conflicts(action, index, action.bindings[index].effectivePath))
                                        key = "<color=#" + ColorUtility.ToHtmlStringRGB(TerminalUI.Conflict) + ">" + key + "</color>";
                                    // Only the key is red. Rich text keeps that color during hover and selection.
                                    label.text = name + " : " + key;
                                };
                                refreshBindingLabels.Add(updateLabel);
                                updateLabel();
                            }
                        });
                    }
                    if (sectionRows.Count == 0) continue;
                    sectionCount++;
                    visible.Add(new Row { name = section, tab = "KEYBINDS", draw = row =>
                        TerminalUI.Text("Section heading", row, section, .02f, .15f, .96f, .7f, 26) });
                    visible.AddRange(sectionRows);
                }
            }
            float height = 82 * GamePreferences.TextScale;
            content.sizeDelta = new Vector2(0, Mathf.Max(1, visible.Count * height));
            for (int i = 0; i < visible.Count; i++)
            {
                var row = visible[i];
                var rect = TerminalUI.Rect(row.name, content, 0, 0, 1, 1);
                rect.anchorMin = new Vector2(0, 1); rect.anchorMax = Vector2.one; rect.pivot = new Vector2(.5f, 1);
                rect.sizeDelta = new Vector2(0, height); rect.anchoredPosition = Vector2.down * i * height;
                row.draw(rect);
                foreach (var selectable in rect.GetComponentsInChildren<Selectable>())
                    selectable.gameObject.AddComponent<TerminalSettingHelp>().show = () => description.text = row.tab + " / " + row.name + "\n\n" + row.help;
            }
            content.GetComponentInParent<TerminalScroll>().ResetPosition();
            heading.text = query.Length > 0 ? "SEARCH RESULTS / " + (visible.Count - sectionCount) : "TERMINAL SETTINGS / " + tab;
            resetLabel.text = keybinds ? "RESET " + (controllerBindings ? "CONTROLLER" : "KEYBOARD") : "RESET " + tab;
            description.text = visible.Count == 0 ? "NO MATCHES\nTry a shorter word." :
                keybinds ? "Controls are grouped by location.\nShared bindings appear once under ON FOOT + IN SHIP.\nReset affects only this device." :
                "Choose a setting. Changes preview immediately.\nAPPLY SETTINGS saves all tabs.";
        }

        private TerminalSlider MakeSlider(RectTransform row, string name, float value, float min, float max, Action<float> save, string suffix)
        {
            var label = TerminalUI.Text("Label", row, name, 0, 0, .75f, .45f, 23);
            var number = TerminalUI.Text("Value", row, "", .76f, 0, .24f, .45f, 23);
            var track = TerminalUI.Panel("Track", row, .02f, .6f, .94f, .23f, TerminalUI.Dark);
            var slider = track.gameObject.AddComponent<TerminalSlider>();
            slider.SetDisplay(menu == null ? null : menu.GetComponentInChildren<TerminalDisplay>(true));
            var handleArea = TerminalUI.Rect("Handle area", track.transform, .02f, -.5f, .96f, 2);
            var handle = TerminalUI.Panel("Handle", handleArea, 0, 0, .03f, 1, TerminalUI.Ink);
            handle.rectTransform.sizeDelta = new Vector2(16, 0);
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.minValue = min; slider.maxValue = max;
            slider.SetValueWithoutNotify(value);
            Action<float> show = v => number.text = suffix == "%" ? Mathf.RoundToInt(v * 100) + "%" :
                suffix == " FPS" ? Mathf.RoundToInt(v) + suffix : v.ToString("0.00") + suffix;
            show(value);
            slider.onValueChanged.AddListener(v => { save(v); show(v); });
            return slider;
        }

        private void AskReset()
        {
            string selectedTab = tab;
            bool selectedController = controllerBindings;
            string title = tab == "KEYBINDS" ? (selectedController ? "CONTROLLER" : "KEYBOARD") : tab;
            var modal = TerminalModal.Open("RESET " + title, "RESTORE DEFAULTS FOR THIS TAB?\nOTHER TABS WILL KEEP THEIR SETTINGS.");
            TerminalUI.Button(modal.Body, "RESET " + title, 0, .68f, .48f, .16f, () =>
            {
                if (selectedTab == "KEYBINDS") TerminalBindings.ResetDevice(selectedController);
                else ResetTab(selectedTab);
                modal.Close();
                Refresh();
                if (selectedTab == "VIDEO" && TerminalModal.Current != null)
                    TerminalModal.Current.closed += () => StartCoroutine(ReadModeAfterPreview());
            });
            TerminalUI.Button(modal.Body, "CANCEL", .52f, .68f, .48f, .16f, modal.Close);
        }
        private System.Collections.IEnumerator ReadModeAfterPreview()
        {
            yield return null; // Screen.SetResolution takes effect at the end of the frame.
            ReadDisplayMode();
            UpdateDisplayLabels();
        }
        public static void ResetTab(string area)
        {
            if (area == "KEYBINDS") { TerminalBindings.Reset(); return; }
            string[] keys = area == "GENERAL" ? new[] { "ControllerSensitivity", "DeadZone", "InvertLook", "CursorSize", "DarkCursor", "CursorOutline" } :
                area == "VIDEO" ? new[] { "Brightness", "Contrast", "TextScale", "CRTIntensity", "SafeArea", "VSync", "FrameCap" } : new[] { "Interface", "MuteUnfocused" };
            foreach (string key in keys) GamePreferences.SetOption(key, GamePreferences.DefaultOption(key));
            if (area == "GENERAL") { GamePreferences.SetMouseSensitivity(1); GamePreferences.SetPlayBoot(true); }
            if (area == "AUDIO") { GamePreferences.SetFX(.65f); GamePreferences.SetMusic(.65f); }
            if (area == "VIDEO")
            {
                GamePreferences.SetReducedMotion(false);
                TerminalDisplayOptions.ApplyFrameRate();
                // Close the reset question before opening the display preview.
                if (TerminalModal.Current != null) TerminalModal.Current.Close();
                TerminalDisplayOptions.ResetMode();
            }
        }
    }
}
