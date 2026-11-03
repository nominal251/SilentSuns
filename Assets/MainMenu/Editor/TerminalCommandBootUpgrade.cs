using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SilentSuns.Menu.Editor
{
    // Updates the boot text and layout without changing the scene setup.
    public static class TerminalCommandBootUpgrade
    {
        public static void Apply()
        {
            var scene = EditorSceneManager.OpenScene("Assets/MainMenu/Scenes/MainMenu.unity");
            Configure(GameObject.Find("ARCA Terminal"));
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
        }

        public static void Configure(GameObject root)
        {
            Transform parent = root.transform;
            TerminalBootLog log = root.GetComponent<TerminalBootLog>();
            if (log == null) log = root.AddComponent<TerminalBootLog>();
            var fields = new SerializedObject(log);
            Set(fields, "output", parent.Find("Boot/Boot output").GetComponent<TMP_Text>());
            Set(fields, "typing", root.GetComponentInChildren<TerminalTypingAudio>(true));
            Set(fields, "display", root.GetComponentInChildren<TerminalDisplay>(true));
            fields.ApplyModifiedPropertiesWithoutUndo();
            var menu = new SerializedObject(root.GetComponent<TerminalMenu>());
            Set(menu, "bootLog", log);
            menu.ApplyModifiedPropertiesWithoutUndo();

            Text(parent, "Boot/Diagnostic heading", "ARCA/OS 4.2   /   LOCAL STARTUP", 28);
            Text(parent, "Boot/BIOS revision", "SYS: READ ONLY     NODE: 042     CONSOLE: TTY01", 21);
            TMP_Text output = parent.Find("Boot/Boot output").GetComponent<TMP_Text>();
            output.fontSize = 23;
            output.lineSpacing = 2;
            output.textWrappingMode = TextWrappingModes.NoWrap;
            output.overflowMode = TextOverflowModes.Overflow;
            Layout(output.rectTransform, 100, 244, 1400, 430);
            Layout(parent.Find("Main Menu/Wordmark"), 100, 142, 1400, 88);
            Layout(parent.Find("Main Menu/Session label"), 108, 245, 1380, 32);
            Layout(parent.Find("Main Menu/Functions heading"), 108, 312, 1380, 30);
            string[] buttons = { "Begin Survey", "Terminal Settings", "Operator Guide", "Exit Program" };
            for (int i = 0; i < buttons.Length; i++)
            {
                Transform button = parent.Find("Main Menu/" + buttons[i]);
                Layout(button, 100, 368 + i * 86, 1400, 64);
                // Builder children use fixed positions rather than stretch anchors.
                Layout(button.Find("Label"), 24, 0, 1320, 64);
                Layout(button.Find("Arrow"), 1340, 0, 42, 64);
            }
            Layout(parent.Find("Main Menu/Command line"), 108, 710, 1380, 26);
            Layout(parent.Find("Main Menu/Menu hints"), 108, 756, 1380, 26);
            Text(parent, "Settings/Sound Effects label", "SOUND EFFECTS (SFX)", 27);
        }

        private static void Text(Transform parent, string path, string content, float size)
        {
            TMP_Text text = parent.Find(path).GetComponent<TMP_Text>();
            text.text = content;
            text.fontSize = size;
        }

        private static void Layout(Transform item, float x, float y, float width, float height)
        {
            var rect = (RectTransform)item;
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
        }

        private static void Set(SerializedObject target, string field, Object value)
        {
            target.FindProperty(field).objectReferenceValue = value;
        }
    }
}
