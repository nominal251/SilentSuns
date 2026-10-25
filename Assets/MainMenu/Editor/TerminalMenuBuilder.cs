using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Audio;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UIButton = SilentSuns.Menu.TerminalButton;

namespace SilentSuns.Menu.Editor
{
    public static class TerminalMenuBuilder
    {
        private const string Folder = "Assets/MainMenu";
        private const string ScenePath = Folder + "/Scenes/MainMenu.unity";
        private static TMP_FontAsset font;
        private static readonly Color Ink = new Color32(0, 0, 150, 255);
        private static readonly Color Paper = new Color32(222, 222, 230, 255);
        private static readonly Color Dim = new Color32(177, 180, 203, 255);
        private static readonly Color Accent = new Color32(239, 240, 255, 255);
        private static readonly Color Rule = new Color32(115, 124, 185, 255);
        private static readonly Color Navy = new Color32(0, 0, 170, 255);

        // Rebuild only this feature's scene; ask before replacing an edited menu.
        [MenuItem("Silent Suns/Main Menu/Rebuild menu scene")]
        public static void RebuildMenu()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            if (!Application.isBatchMode && File.Exists(ScenePath) && !EditorUtility.DisplayDialog("Rebuild main menu?", "This replaces the MainMenu scene with the default terminal layout. Keep a copy of any layout changes first.", "Rebuild", "Cancel"))
                return;
            CreateScene();
        }

        // Batch entry point creates the initial scene without replacing an existing one.
        public static void CreateInitialScene()
        {
            if (!File.Exists(ScenePath))
                CreateScene();
        }

        // The scene stores real UI objects and references, editable in the Inspector.
        private static void CreateScene()
        {
            CreateAudioMixer();
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Folder + "/Fonts/TerminalFont.asset");
            if (font == null)
            {
                Font source = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/Px437_DOS-V_re_ANK16.ttf");
                font = TMP_FontAsset.CreateFontAsset(source, 64, 5, UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 1024, 1024);
                font.name = "TerminalFont";
                font.TryAddCharacters("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789 .,:;!?%[]()/+-_=><'\"\\|");
                AssetDatabase.CreateAsset(font, Folder + "/Fonts/TerminalFont.asset");
                AssetDatabase.AddObjectToAsset(font.material, font);
                foreach (Texture2D texture in font.atlasTextures)
                    AssetDatabase.AddObjectToAsset(texture, font);
            }

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Camera camera = new GameObject("Terminal Camera", typeof(Camera), typeof(AudioListener)).GetComponent<Camera>();
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.orthographic = true;
            camera.orthographicSize = 4.5f;
            camera.cullingMask = 1 << 31;
            camera.depth = -10;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 30f;

            GameObject events = new GameObject("Menu Input", typeof(EventSystem), typeof(InputSystemUIInputModule));
            events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            events.GetComponent<InputSystemUIInputModule>().deselectOnBackgroundClick = false;

            GameObject root = new GameObject("ARCA Terminal", typeof(RectTransform), typeof(Canvas), typeof(TerminalRaycaster), typeof(TerminalMenu), typeof(TerminalInputHints), typeof(AudioSource));
            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.sizeDelta = new Vector2(1600f, 900f);
            rootRect.localScale = Vector3.one * 0.01f;
            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = camera;
            TerminalMenu menu = root.GetComponent<TerminalMenu>();
            AudioSource audio = root.GetComponent<AudioSource>();
            audio.playOnAwake = false;
            audio.spatialBlend = 0f;
            ConfigureInteractionAudio(root);
            TerminalWarmupUpgrade.Configure(root);

            GameObject music = new GameObject("Menu Music", typeof(AudioSource), typeof(TerminalMenuMusic));
            music.transform.SetParent(root.transform, false);
            AudioSource musicSource = music.GetComponent<AudioSource>();
            musicSource.playOnAwake = false;
            musicSource.loop = true;
            musicSource.volume = 0f;
            musicSource.spatialBlend = 0f;
            string musicPath = Folder + "/Audio/Menu Music.mp3";
            AudioImporter musicImporter = (AudioImporter)AssetImporter.GetAtPath(musicPath);
            AudioImporterSampleSettings musicSettings = musicImporter.defaultSampleSettings;
            musicSettings.loadType = AudioClipLoadType.Streaming;
            musicImporter.defaultSampleSettings = musicSettings;
            musicImporter.SaveAndReimport();
            musicSource.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(musicPath);
            musicSource.outputAudioMixerGroup = AssetDatabase.LoadAssetAtPath<AudioMixer>(Folder + "/Resources/SilentSunsAudio.mixer").FindMatchingGroups("Music")[0];

            GameObject typing = new GameObject("Keyboard Audio", typeof(AudioSource), typeof(TerminalTypingAudio));
            typing.transform.SetParent(root.transform, false);
            typing.GetComponent<AudioSource>().playOnAwake = false;
            TerminalTypingAudio typingAudio = typing.GetComponent<TerminalTypingAudio>();
            SerializedObject typingFields = new SerializedObject(typingAudio);
            SerializedProperty clips = typingFields.FindProperty("keypresses");
            clips.arraySize = 4;
            for (int i = 0; i < clips.arraySize; i++)
                clips.GetArrayElementAtIndex(i).objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(Folder + "/Audio/keypress-00" + (i + 1) + ".wav");
            typingFields.ApplyModifiedPropertiesWithoutUndo();

            Box(root.transform, "Monitor face", 0, 0, 1600, 900, Ink);
            Box(root.transform, "Header band", 2, 2, 1596, 94, Navy);
            Box(root.transform, "Footer band", 2, 818, 1596, 80, Paper);
            Box(root.transform, "Top frame", 0, 0, 1600, 2, Rule);
            Box(root.transform, "Bottom frame", 0, 898, 1600, 2, Rule);
            Box(root.transform, "Left frame", 0, 0, 2, 900, Rule);
            Box(root.transform, "Right frame", 1598, 0, 2, 900, Rule);
            Label(root.transform, "System identity", "ARCA SYSTEM SETUP UTILITY", 80, 46, 760, 30, 24, Paper);
            Label(root.transform, "Vessel identity", "042  /  ARCADIA", 1170, 46, 350, 30, 24, Dim, TextAlignmentOptions.Right);
            Box(root.transform, "Header rule", 80, 96, 1440, 1, Rule);
            Box(root.transform, "Footer rule", 80, 818, 1440, 1, Rule);
            Label(root.transform, "Footer left", "ONBOARD SYSTEMS / OPERATOR ACCESS", 80, 840, 820, 28, 20, Ink);
            Label(root.transform, "Footer right", "LOCAL ACCESS ONLY", 1110, 840, 410, 28, 20, Ink, TextAlignmentOptions.Right);

            CanvasGroup standby = Panel(root.transform, "Standby");

            CanvasGroup boot = Panel(root.transform, "Boot");
            Box(boot.transform, "POST background", 2, 98, 1596, 720, Ink);
            Label(boot.transform, "Diagnostic heading", "INITIALIZING ONBOARD SYSTEMS", 100, 142, 1200, 40, 30, Paper);
            Label(boot.transform, "BIOS revision", "ARCADIA 042 / OPERATOR CHANNEL", 100, 188, 1200, 30, 22, Dim);
            TMP_Text bootText = Label(boot.transform, "Boot output", "", 100, 250, 1350, 420, 29, Paper);
            bootText.verticalAlignment = VerticalAlignmentOptions.Top;
            bootText.lineSpacing = 5f;
            UIButton skip = Button(boot.transform, "Skip Boot", "SKIP DIAGNOSTICS  [SPACE]", 100, 706, 600, 52);
            TMP_Text bootProgress = Label(boot.transform, "POST progress", "[............]  SYSTEM CHECK   0%", 890, 712, 620, 40, 22, Paper);
            UnityEventTools.AddPersistentListener(skip.onClick, menu.SkipBoot);

            CanvasGroup main = Panel(root.transform, "Main Menu");
            TMP_Text wordmark = Label(main.transform, "Wordmark", "SILENT.SUNS", 80, 142, 960, 88, 64, Paper);
            Image titleCursor = Box(wordmark.transform, "Block Cursor", 0, 0, 32, 86, Paper);
            TerminalTitle titleAnimation = wordmark.gameObject.AddComponent<TerminalTitle>();
            SerializedObject titleFields = new SerializedObject(titleAnimation);
            Set(titleFields, "cursor", titleCursor.rectTransform);
            Set(titleFields, "typingAudio", typingAudio);
            titleFields.ApplyModifiedPropertiesWithoutUndo();
            Label(main.transform, "Session label", "ARCADIA 042 / ONBOARD OPERATING SYSTEM", 88, 245, 860, 32, 22, Dim);
            Label(main.transform, "Functions heading", "OPERATOR FUNCTIONS", 88, 312, 860, 30, 20, Dim);
            UIButton start = Button(main.transform, "Begin Survey", "01    BEGIN SURVEY", 80, 368, 790, 64);
            UIButton settings = Button(main.transform, "Terminal Settings", "02    TERMINAL SETTINGS", 80, 454, 790, 64);
            UIButton guide = Button(main.transform, "Operator Guide", "03    OPERATOR GUIDE", 80, 540, 790, 64);
            UIButton quit = Button(main.transform, "Exit Program", "04    EXIT PROGRAM", 80, 626, 790, 64);
            UnityEventTools.AddPersistentListener(start.onClick, menu.BeginSurvey);
            UnityEventTools.AddPersistentListener(settings.onClick, menu.OpenSettings);
            UnityEventTools.AddPersistentListener(guide.onClick, menu.OpenGuide);
            UnityEventTools.AddPersistentListener(quit.onClick, menu.OpenQuit);
            TMP_Text commandLine = Label(main.transform, "Command line", "> RUN SURVEY", 88, 710, 900, 26, 22, Accent);
            AddCommand(start, commandLine, "RUN SURVEY");
            AddCommand(settings, commandLine, "OPEN SETTINGS");
            AddCommand(guide, commandLine, "READ OPERATOR_GUIDE");
            AddCommand(quit, commandLine, "TERMINATE LOCAL APPLICATION");
            TMP_Text navigationHint = Label(main.transform, "Menu hints", "ARROWS  SELECT     ENTER  CONFIRM     ESC  BACK", 88, 756, 900, 26, 19, Dim);
            CanvasGroup settingsPanel = Panel(root.transform, "Settings");
            Heading(settingsPanel.transform, "TERMINAL SETTINGS", "CHANGES SAVED AUTOMATICALLY");
            TerminalSettings settingsControls = CreateSettingsControls(settingsPanel.gameObject, menu);
            UIButton settingsBack = Button(settingsPanel.transform, "Settings Back", "BACK  [ESC]", 100, 710, 500, 54);
            UnityEventTools.AddPersistentListener(settingsBack.onClick, menu.BackToMenu);

            CanvasGroup guidePanel = Panel(root.transform, "Operator Guide");
            Heading(guidePanel.transform, "OPERATOR GUIDE", "CURRENT SURVEY PROTOTYPE");
            Label(guidePanel.transform, "Control labels", "W A S D\nMOUSE\nSPACE / CTRL\nQ / E\nR\nSHIFT\nESC", 100, 316, 440, 260, 32, Accent);
            Label(guidePanel.transform, "Control descriptions", "MOVE (ON FOOT + IN SHIP)\nLOOK (ON FOOT + IN SHIP)\nASCEND / DESCEND (IN SHIP)\nROLL (IN SHIP)\nREORIENT (IN SHIP)\nSPRINT (ON FOOT)\nPAUSE", 560, 316, 900, 260, 32, Paper);
            TMP_Text guideNote = Label(guidePanel.transform, "Guide note", "Inspect the ship's instruments to begin your survey.", 100, 624, 1360, 40, 24, Dim);
            UIButton guideBack = Button(guidePanel.transform, "Guide Back", "BACK  [ESC]", 100, 710, 500, 54);
            UnityEventTools.AddPersistentListener(guideBack.onClick, menu.BackToMenu);

            CanvasGroup quitPanel = Panel(root.transform, "Confirm Quit");
            Heading(quitPanel.transform, "EXIT PROGRAM?", "TERMINATE LOCAL APPLICATION");
            UIButton cancel = Button(quitPanel.transform, "Cancel Quit", "CANCEL", 100, 444, 660, 70);
            UIButton confirm = Button(quitPanel.transform, "Confirm Exit", "EXIT PROGRAM", 100, 550, 660, 70);
            UnityEventTools.AddPersistentListener(cancel.onClick, menu.BackToMenu);
            UnityEventTools.AddPersistentListener(confirm.onClick, menu.ConfirmQuit);

            CanvasGroup loading = Panel(root.transform, "Loading");
            Heading(loading.transform, "SESSION HANDOFF", "SURVEY OPERATIONS");
            TMP_Text loadingText = Label(loading.transform, "Loading status", "OPENING SURVEY SESSION...", 100, 414, 1380, 70, 34, Accent);

            CanvasGroup restorationPanel = Panel(root.transform, "SESSION_RESTORATION");
            Heading(restorationPanel.transform, "SILENT.SUNS", "SESSION RESTORATION UTILITY");
            TMP_Text savedInfo = Label(restorationPanel.transform, "Saved session", "", 100, 284, 1360, 28, 22, Dim);
            TMP_Text restoreStatus = Label(restorationPanel.transform, "Save status", "", 100, 322, 1360, 30, 25, Accent);
            Label(restorationPanel.transform, "Restore heading", "SESSION VALIDATION", 100, 376, 1360, 26, 18, Dim);
            TMP_Text diagnostics = Label(restorationPanel.transform, "Diagnostics", "", 100, 418, 1360, 270, 24, Paper);
            diagnostics.verticalAlignment = VerticalAlignmentOptions.Top;
            UIButton skipRestore = Button(restorationPanel.transform, "Skip Restore", "CONTINUE", 100, 720, 600, 52);
            TerminalSessionRestoration restoration = root.AddComponent<TerminalSessionRestoration>();
            UnityEventTools.AddPersistentListener(skipRestore.onClick, restoration.Skip);

            // The ship screen gets the menu texture when TerminalShipIntro loads SampleScene.
            TerminalDisplay display = Rect(root.transform, "CRT Display", 0, 0, 1600, 900).gameObject.AddComponent<TerminalDisplay>();
            // Show black while the ship loads. The player's camera takes over in SampleScene.
            Camera startupCamera = new GameObject("Startup Camera", typeof(Camera)).GetComponent<Camera>();
            startupCamera.tag = "MainCamera";
            startupCamera.clearFlags = CameraClearFlags.SolidColor;
            startupCamera.backgroundColor = Color.black;
            startupCamera.cullingMask = 0;
            Material crtMaterial = AssetDatabase.LoadAssetAtPath<Material>(Folder + "/Shaders/TerminalCRT.mat");
            if (crtMaterial == null)
            {
                crtMaterial = new Material(Shader.Find("Silent Suns/Terminal CRT"));
                AssetDatabase.CreateAsset(crtMaterial, Folder + "/Shaders/TerminalCRT.mat");
            }
            TerminalCRTSettings crtSettings = AssetDatabase.LoadAssetAtPath<TerminalCRTSettings>(Folder + "/Shaders/DefaultCRT.asset");
            if (crtSettings == null)
            {
                crtSettings = ScriptableObject.CreateInstance<TerminalCRTSettings>();
                AssetDatabase.CreateAsset(crtSettings, Folder + "/Shaders/DefaultCRT.asset");
            }
            SerializedObject displayFields = new SerializedObject(display);
            Set(displayFields, "settings", crtSettings);
            Set(displayFields, "sourceCamera", camera);
            Set(displayFields, "screenMaterial", crtMaterial);
            displayFields.ApplyModifiedPropertiesWithoutUndo();
            TerminalWarmupUpgrade.ConfigureDisplay(display);
            SerializedObject restoreFields = new SerializedObject(restoration);
            Set(restoreFields, "menu", menu); Set(restoreFields, "display", display);
            Set(restoreFields, "screen", restorationPanel);
            Set(restoreFields, "skipButton", skipRestore);
            Set(restoreFields, "savedInfo", savedInfo); Set(restoreFields, "status", restoreStatus);
            Set(restoreFields, "diagnostics", diagnostics);
            restoreFields.ApplyModifiedPropertiesWithoutUndo();
            SerializedObject raycaster = new SerializedObject(root.GetComponent<TerminalRaycaster>());
            Set(raycaster, "display", display);
            raycaster.ApplyModifiedPropertiesWithoutUndo();
            foreach (TerminalSlider slider in root.GetComponentsInChildren<TerminalSlider>(true))
            {
                SerializedObject sliderFields = new SerializedObject(slider);
                Set(sliderFields, "display", display);
                sliderFields.ApplyModifiedPropertiesWithoutUndo();
            }
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                child.gameObject.layer = 31;
            SerializedObject fields = new SerializedObject(menu);
            Set(fields, "sourceCanvas", canvas);
            Set(fields, "typingAudio", typingAudio);
            Set(fields, "inputModule", events.GetComponent<InputSystemUIInputModule>());
            Set(fields, "standbyScreen", standby); Set(fields, "bootScreen", boot); Set(fields, "menuScreen", main);
            Set(fields, "settingsScreen", settingsPanel); Set(fields, "guideScreen", guidePanel); Set(fields, "quitScreen", quitPanel); Set(fields, "loadingScreen", loading);
            Set(fields, "startButton", start.gameObject);
            Set(fields, "guideBackButton", guideBack.gameObject); Set(fields, "cancelQuitButton", cancel.gameObject);
            Set(fields, "bootText", bootText); Set(fields, "bootProgress", bootProgress); Set(fields, "loadingText", loadingText);
            Set(fields, "settings", settingsControls);
            Set(fields, "restoration", restoration);
            Set(fields, "menuMusic", music.GetComponent<TerminalMenuMusic>());
            Set(fields, "titleAnimation", titleAnimation);
            Set(fields, "display", display);
            fields.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject hints = new SerializedObject(root.GetComponent<TerminalInputHints>());
            Set(hints, "inputModule", events.GetComponent<InputSystemUIInputModule>());
            Set(hints, "navigationHint", navigationHint);
            Set(hints, "skipLabel", skip.transform.Find("Label").GetComponent<TMP_Text>());
            Set(hints, "restoreSkipLabel", skipRestore.transform.Find("Label").GetComponent<TMP_Text>());
            Set(hints, "guideNote", guideNote);
            SerializedProperty backLabels = hints.FindProperty("backLabels");
            backLabels.arraySize = 2;
            backLabels.GetArrayElementAtIndex(0).objectReferenceValue = settingsBack.transform.Find("Label").GetComponent<TMP_Text>();
            backLabels.GetArrayElementAtIndex(1).objectReferenceValue = guideBack.transform.Find("Label").GetComponent<TMP_Text>();
            hints.ApplyModifiedPropertiesWithoutUndo();

            foreach (CanvasGroup panel in new[] { boot, main, settingsPanel, guidePanel, quitPanel, loading, restorationPanel })
                panel.gameObject.SetActive(false);
            TerminalCommandBootUpgrade.Configure(root);
            TerminalShipUpgrade.ConfigureDisplay(Object.FindFirstObjectByType<TerminalDisplay>());
            EditorSceneManager.SaveScene(scene, ScenePath);
            CreatePausePrefab();
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) }
                .Concat(EditorBuildSettings.scenes.Where(s => s.path != ScenePath)).ToArray();
            AssetDatabase.SaveAssets();
            Debug.Log("Main menu scene ready: " + ScenePath);
        }

        // Keep the pause screen in a prefab so the survey scene needs no layout changes.
        private static void CreatePausePrefab()
        {
            GameObject root = new GameObject("Survey Pause", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup), typeof(TerminalPause));
            ConfigureInteractionAudio(root);
            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600, 900);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            TerminalPause pause = root.GetComponent<TerminalPause>();
            RectTransform layout = Rect(root.transform, "Layout", 0, 0, 1600, 900);
            layout.anchorMin = layout.anchorMax = layout.pivot = Vector2.one * 0.5f;

            RectTransform panel = Rect(layout, "Pause", 0, 0, 1600, 900);
            Box(panel, "Dim", -5000, -5000, 10000, 10000, new Color(0, 0, 0, 0.78f)).raycastTarget = true;
            Box(panel, "Border", 336, 156, 928, 588, Paper);
            Box(panel, "Face", 340, 160, 920, 580, Ink);
            Box(panel, "Title bar", 340, 160, 920, 60, Navy);
            Label(panel, "Title", "SURVEY SUSPENDED", 380, 170, 840, 40, 32, Paper);
            Label(panel, "Status", "SESSION HELD IN MEMORY", 380, 240, 840, 30, 22, Dim);
            UIButton resume = Button(panel, "Resume", "RESUME SURVEY", 380, 300, 840, 64);
            UIButton settingsButton = Button(panel, "Settings", "SETTINGS", 380, 390, 840, 64);
            UnityEventTools.AddPersistentListener(settingsButton.onClick, pause.OpenSettings);
            UIButton menu = Button(panel, "Main Menu", "RETURN TO MAIN MENU", 380, 480, 840, 64);
            UIButton quit = Button(panel, "Quit", "EXIT GAME", 380, 570, 840, 64);
            TMP_Text hint = Label(panel, "Input hint", "", 380, 684, 840, 30, 20, Paper);
            UnityEventTools.AddPersistentListener(resume.onClick, pause.Resume);
            UnityEventTools.AddPersistentListener(menu.onClick, pause.AskReturnToMenu);
            UnityEventTools.AddPersistentListener(quit.onClick, pause.AskQuit);

            RectTransform confirmation = Rect(layout, "Confirmation", 0, 0, 1600, 900);
            Box(confirmation, "Dim", -5000, -5000, 10000, 10000, new Color(0, 0, 0, 0.78f)).raycastTarget = true;
            Box(confirmation, "Border", 336, 246, 928, 408, Paper);
            Box(confirmation, "Face", 340, 250, 920, 400, Ink);
            TMP_Text text = Label(confirmation, "Question", "", 380, 290, 840, 120, 27, Paper);
            UIButton cancel = Button(confirmation, "Cancel", "CANCEL", 380, 446, 840, 64);
            UIButton leave = Button(confirmation, "Confirm", "CONFIRM", 380, 546, 840, 64);
            UnityEventTools.AddPersistentListener(cancel.onClick, pause.CancelLeave);
            UnityEventTools.AddPersistentListener(leave.onClick, pause.ConfirmLeave);
            RectTransform settingsPanel = Rect(layout, "Settings", 0, 0, 1600, 900);
            Box(settingsPanel, "Dim", -5000, -5000, 10000, 10000, new Color(0, 0, 0, 0.78f)).raycastTarget = true;
            Box(settingsPanel, "Border", 66, 116, 1468, 668, Paper);
            Box(settingsPanel, "Face", 70, 120, 1460, 660, Ink);
            Heading(settingsPanel, "SETTINGS", "CHANGES SAVED AUTOMATICALLY");
            TerminalSettings settingsControls = CreateSettingsControls(settingsPanel.gameObject, null);
            UIButton settingsBack = Button(settingsPanel, "Settings Back", "BACK  [ESC]", 100, 710, 500, 54);
            UnityEventTools.AddPersistentListener(settingsBack.onClick, pause.CloseSettings);
            SerializedObject fields = new SerializedObject(pause);
            Set(fields, "pausePanel", panel.gameObject); Set(fields, "confirmationPanel", confirmation.gameObject);
            Set(fields, "settingsPanel", settingsPanel.gameObject); Set(fields, "settings", settingsControls);
            Set(fields, "settingsButton", settingsButton.gameObject); Set(fields, "visibility", root.GetComponent<CanvasGroup>());
            Set(fields, "resumeButton", resume.gameObject); Set(fields, "cancelButton", cancel.gameObject);
            Set(fields, "confirmationText", text); Set(fields, "inputHint", hint);
            fields.ApplyModifiedPropertiesWithoutUndo();
            TerminalShipUpgrade.ConfigurePause(root);
            PrefabUtility.SaveAsPrefabAsset(root, Folder + "/Resources/SilentSunsPause.prefab");
            Object.DestroyImmediate(root);
        }

        private static void ConfigureInteractionAudio(GameObject root)
        {
            TerminalInteractionAudio interaction = root.AddComponent<TerminalInteractionAudio>();
            SerializedObject fields = new SerializedObject(interaction);
            string[] names = { "KeyboardPress", "MouseClick" };
            string[] references = { "keyboardPress", "mouseClick" };
            for (int i = 0; i < names.Length; i++)
            {
                string path = Folder + "/Audio/Interaction/" + names[i] + ".wav";
                AudioImporter importer = (AudioImporter)AssetImporter.GetAtPath(path);
                AudioImporterSampleSettings samples = importer.defaultSampleSettings;
                samples.loadType = AudioClipLoadType.DecompressOnLoad;
                samples.compressionFormat = AudioCompressionFormat.PCM;
                samples.preloadAudioData = true;
                importer.defaultSampleSettings = samples;
                importer.SaveAndReimport();
                Set(fields, references[i], AssetDatabase.LoadAssetAtPath<AudioClip>(path));
            }
            fields.ApplyModifiedPropertiesWithoutUndo();
        }

        // Both menus use the same controls and saved preferences.
        private static TerminalSettings CreateSettingsControls(GameObject panel, TerminalMenu menu)
        {
            TerminalSettings settings = panel.AddComponent<TerminalSettings>();
            SerializedObject fields = new SerializedObject(settings);
            SettingsCategory(panel.transform, "AUDIO", 294);
            SettingsCategory(panel.transform, "CONTROLS", 454);
            SettingsCategory(panel.transform, "DISPLAY & STARTUP", 554);
            string[] names = { "Sound Effects", "Music", "Mouse Sensitivity" };
            float[] rows = { 332, 392, 492 };
            string[] sliderFields = { "fxSlider", "musicSlider", "sensitivitySlider" };
            string[] valueFields = { "fxValue", "musicValue", "sensitivityValue" };
            Slider[] sliders = new Slider[3];
            for (int i = 0; i < names.Length; i++)
            {
                float y = rows[i];
                Label(panel.transform, names[i] + " label", i == 0 ? "SOUND EFFECTS (SFX)" : names[i].ToUpperInvariant(), 100, y, 480, 40, 27, Paper);
                sliders[i] = MakeSlider(panel.transform, 620, y, 590, 40);
                sliders[i].name = names[i];
                TMP_Text value = Label(panel.transform, names[i] + " value", "", 1240, y, 220, 40, 28, Accent, TextAlignmentOptions.Right);
                Set(fields, sliderFields[i], sliders[i]); Set(fields, valueFields[i], value);
            }
            sliders[2].minValue = 0.1f;
            sliders[2].maxValue = 3f;
            Toggle motion = MakeToggle(panel.transform, "Reduced Motion", "REDUCED MOTION", 100, 590);
            Toggle boot = MakeToggle(panel.transform, "Play Boot", "PLAY DIAGNOSTICS ON START", 100, 648);
            Set(fields, "motionToggle", motion); Set(fields, "bootToggle", boot); Set(fields, "menu", menu);
            fields.ApplyModifiedPropertiesWithoutUndo();
            UnityEventTools.AddPersistentListener(sliders[0].onValueChanged, settings.SetFX);
            UnityEventTools.AddPersistentListener(sliders[1].onValueChanged, settings.SetMusic);
            UnityEventTools.AddPersistentListener(sliders[2].onValueChanged, settings.SetSensitivity);
            UnityEventTools.AddPersistentListener(motion.onValueChanged, settings.SetMotion);
            UnityEventTools.AddPersistentListener(boot.onValueChanged, settings.SetBoot);
            return settings;
        }

        private static void SettingsCategory(Transform parent, string title, float y)
        {
            Label(parent, title + " category", title, 100, y, 480, 28, 21, Dim);
            Box(parent, title + " rule", 620, y + 14, 840, 1, Rule);
        }

        // Unity's mixer creation API is editor-only. The result is a normal mixer asset.
        private static void CreateAudioMixer()
        {
            string path = Folder + "/Resources/SilentSunsAudio.mixer";
            if (AssetDatabase.LoadAssetAtPath<AudioMixer>(path) != null)
                return;
            System.Type type = System.AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetType("UnityEditor.Audio.AudioMixerController"))
                .First(found => found != null);
            Object mixer = (Object)type.GetMethod("CreateMixerControllerAtPath").Invoke(null, new object[] { path });
            object master = type.GetProperty("masterGroup").GetValue(mixer);
            foreach (string name in new[] { "FX", "Music" })
            {
                Object group = (Object)type.GetMethod("CreateNewGroup").Invoke(mixer, new object[] { name, false });
                type.GetMethod("AddChildToParent").Invoke(mixer, new object[] { group, master });
                object guid = group.GetType().GetMethod("GetGUIDForVolume").Invoke(group, null);
                System.Type pathType = type.Assembly.GetType("UnityEditor.Audio.AudioGroupParameterPath");
                object parameterPath = System.Activator.CreateInstance(pathType, group, guid);
                type.GetMethod("AddExposedParameter").Invoke(mixer, new[] { parameterPath });
                SerializedObject mixerData = new SerializedObject(mixer);
                SerializedProperty exposed = mixerData.FindProperty("m_ExposedParameters");
                for (int i = 0; i < exposed.arraySize; i++)
                {
                    SerializedProperty parameter = exposed.GetArrayElementAtIndex(i);
                    if (parameter.FindPropertyRelative("name").stringValue.StartsWith("MyExposedParam"))
                        parameter.FindPropertyRelative("name").stringValue = name + "Volume";
                }
                mixerData.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(group);
            }
            EditorUtility.SetDirty(mixer);
            AssetDatabase.SaveAssets();
        }

        // Build a local Windows review copy using the same scene list as the project.
        public static void BuildPreview()
        {
            CreateInitialScene();
            string output = System.Environment.GetEnvironmentVariable("SILENT_SUNS_MENU_BUILD");
            if (string.IsNullOrEmpty(output))
                output = "Builds/MenuPreview/SilentSuns.exe";
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            UnityEditor.Build.Reporting.BuildReport report = BuildPipeline.BuildPlayer(
                EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(), output,
                BuildTarget.StandaloneWindows64, BuildOptions.None);
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                throw new System.Exception("Menu preview build failed: " + report.summary.result);
        }

        // Keep all layout positions in a single 1600 by 900 terminal coordinate space.
        private static RectTransform Rect(Transform parent, string name, float x, float y, float width, float height)
        {
            RectTransform rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
            return rect;
        }

        // Decorative graphics never intercept button presses.
        private static Image Box(Transform parent, string name, float x, float y, float width, float height, Color tint)
        {
            Image image = Rect(parent, name, x, y, width, height).gameObject.AddComponent<Image>();
            image.color = tint;
            image.raycastTarget = false;
            return image;
        }

        // Reuse the project's DOS font throughout the terminal.
        private static TMP_Text Label(Transform parent, string name, string text, float x, float y, float width, float height, float size, Color tint, TextAlignmentOptions alignment = TextAlignmentOptions.Left)
        {
            TextMeshProUGUI label = Rect(parent, name, x, y, width, height).gameObject.AddComponent<TextMeshProUGUI>();
            label.font = font;
            label.text = text;
            label.fontSize = size;
            label.color = tint;
            label.alignment = alignment;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.raycastTarget = false;
            return label;
        }

        // Each screen is a separate group in the scene hierarchy.
        private static CanvasGroup Panel(Transform parent, string name)
        {
            return Rect(parent, name, 0, 0, 1600, 900).gameObject.AddComponent<CanvasGroup>();
        }

        // Keep the button treatment consistent across each terminal screen.
        private static UIButton Button(Transform parent, string name, string text, float x, float y, float width, float height)
        {
            Image image = Box(parent, name, x, y, width, height, Color.white);
            image.raycastTarget = true;
            UIButton button = image.gameObject.AddComponent<UIButton>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.normalColor = new Color32(0, 0, 115, 255);
            colors.highlightedColor = new Color32(208, 210, 215, 255);
            colors.selectedColor = colors.highlightedColor;
            colors.pressedColor = new Color32(183, 188, 195, 255);
            colors.fadeDuration = 0f;
            button.colors = colors;
            Box(image.transform, "Edge", 0, 0, 3, height, Accent);
            Label(image.transform, "Label", text, 24, 0, width - 70, height, 28, Paper);
            Label(image.transform, "Arrow", ">", width - 48, 0, 30, height, 28, Accent);
            return button;
        }

        // Settings headings share the same layout as the main title.
        private static void Heading(Transform parent, string title, string subtitle)
        {
            Label(parent, "Heading", title, 100, 148, 1400, 90, 62, Paper);
            Label(parent, "Subtitle", subtitle, 100, 252, 1400, 34, 24, Dim);
        }

        // Main menu options share one command line for hover and navigation feedback.
        private static void AddCommand(UIButton button, TMP_Text commandLine, string command)
        {
            SerializedObject fields = new SerializedObject(button.gameObject.AddComponent<TerminalMenuOption>());
            Set(fields, "commandLine", commandLine);
            Set(fields, "arrow", button.transform.Find("Arrow").GetComponent<TMP_Text>());
            Set(fields, "edge", button.transform.Find("Edge").GetComponent<Image>());
            fields.FindProperty("command").stringValue = command;
            fields.ApplyModifiedPropertiesWithoutUndo();
        }

        // A real slider supports mouse, keyboard and controller input.
        private static Slider MakeSlider(Transform parent, float x, float y, float width, float height)
        {
            RectTransform root = Rect(parent, "Terminal Volume", x, y, width, height);
            Image hitArea = root.gameObject.AddComponent<Image>();
            hitArea.color = Color.clear;
            Box(root, "Track", 0, 16, width, 8, Rule);
            Slider slider = root.gameObject.AddComponent<TerminalSlider>();
            RectTransform fillArea = Rect(root, "Fill Area", 0, 16, width, 8);
            Image fill = Box(fillArea, "Fill", 0, 0, width, 8, Accent);
            fill.rectTransform.anchorMin = Vector2.zero;
            fill.rectTransform.anchorMax = Vector2.one;
            fill.rectTransform.offsetMin = fill.rectTransform.offsetMax = Vector2.zero;
            RectTransform handleArea = Rect(root, "Handle Area", 8, 4, width - 16, 32);
            Image handle = Box(handleArea, "Handle", 0, 0, 16, 32, Paper);
            handle.rectTransform.anchorMin = Vector2.zero;
            handle.rectTransform.anchorMax = Vector2.up;
            handle.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            handle.rectTransform.sizeDelta = new Vector2(16f, 0f);
            handle.rectTransform.anchoredPosition = Vector2.zero;
            handle.raycastTarget = true;
            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            return slider;
        }

        // Checked boxes remain visible without using a particular hue.
        private static Toggle MakeToggle(Transform parent, string name, string text, float x, float y)
        {
            Image background = Box(parent, name, x, y, 1260, 48, Color.white);
            background.raycastTarget = true;
            Toggle toggle = background.gameObject.AddComponent<TerminalToggle>();
            Image box = Box(background.transform, "Box", 14, 10, 28, 28, Rule);
            Image check = Box(background.transform, "Check", 20, 16, 16, 16, Accent);
            toggle.targetGraphic = background;
            toggle.graphic = check;
            ColorBlock colors = toggle.colors;
            colors.normalColor = new Color32(0, 0, 115, 255);
            colors.highlightedColor = new Color32(208, 210, 215, 255);
            colors.selectedColor = colors.highlightedColor;
            colors.pressedColor = new Color32(183, 188, 195, 255);
            toggle.colors = colors;
            Label(background.transform, "Label", text, 68, 0, 1160, 48, 26, Paper);
            return toggle;
        }

        // Assign serialized references without exposing fields solely for the builder.
        private static void Set(SerializedObject target, string name, Object value)
        {
            target.FindProperty(name).objectReferenceValue = value;
        }
    }
}
