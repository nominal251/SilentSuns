using System.Collections;
using System.Linq;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.UI;
using UnityEngine.TestRunner;
using UnityEngine.TestTools;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.TestTools;
[assembly: TestPlayerBuildModifier(typeof(SilentSuns.Menu.Tests.HiddenDisplayTestBuild))]
#endif
[assembly: TestRunCallback(typeof(SilentSuns.Menu.Tests.DisplayPlayerResults))]

namespace SilentSuns.Menu.Tests
{
    public class DisplayPlayerResults : ITestRunCallback
    {
        public void RunStarted(ITest test) { }
        public void TestStarted(ITest test) { }
        public void TestFinished(ITestResult result) { }
        public void RunFinished(ITestResult result)
        {
            string path = System.Environment.GetEnvironmentVariable("SILENT_SUNS_DISPLAY_RESULTS");
            if (Application.isEditor || string.IsNullOrEmpty(path)) return;
            System.IO.File.WriteAllText(path, result.ToXml(true).OuterXml);
            Debug.Log("Display validation finished: " + result.ResultState);
            Application.Quit(result.FailCount == 0 ? 0 : 1);
        }
    }
#if UNITY_EDITOR
    // Let the shell launch this test build hidden instead of opening a window over the desktop.
    public class HiddenDisplayTestBuild : ITestPlayerBuildModifier
    {
        public BuildPlayerOptions ModifyOptions(BuildPlayerOptions options)
        {
            string path = System.Environment.GetEnvironmentVariable("SILENT_SUNS_DISPLAY_TEST");
            if (!string.IsNullOrEmpty(path))
            {
                options.locationPathName = path;
                options.options &= ~BuildOptions.AutoRunPlayer;
            }
            return options;
        }
    }
#endif

    public class TerminalDisplayPlayerTests
    {
        private IEnumerator ClickDisplayButton(Mouse mouse, string name)
        {
            yield return null; yield return null;
            Canvas.ForceUpdateCanvases();
            var modal = TerminalModal.Current;
            var button = modal.GetComponentsInChildren<TerminalButton>().First(b => b.name == name);
            var rect = (RectTransform)button.transform;
            Vector2 point = modal.GetComponent<TerminalPauseDisplay>().SourceCamera.WorldToScreenPoint(rect.TransformPoint(rect.rect.center));
            var hits = new System.Collections.Generic.List<RaycastResult>();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = point }, hits);
            Assert.That(hits.Count, Is.GreaterThan(0));
            Assert.That(hits[0].gameObject, Is.SameAs(button.gameObject));
            InputSystem.QueueStateEvent(mouse, new MouseState { position = point });
            yield return null;
            InputSystem.QueueStateEvent(mouse, new MouseState { position = point }.WithButton(MouseButton.Left));
            yield return null;
            InputSystem.QueueStateEvent(mouse, new MouseState { position = point });
            yield return null;
            Assert.That(TerminalModal.Current, Is.Null, name + " must respond after the window size changes.");
        }

        [UnityTest, Category("DisplayPlayer")]
        public IEnumerator WindowModesAndResolutionChangeInPlayer()
        {
            if (Application.isEditor) Assert.Ignore("Window changes need a standalone player.");
            int oldWidth = Screen.width, oldHeight = Screen.height;
            FullScreenMode oldMode = Screen.fullScreenMode;
            string[] keys = { "SilentSuns.Display.Width", "SilentSuns.Display.Height", "SilentSuns.Display.Mode" };
            bool[] existed = keys.Select(PlayerPrefs.HasKey).ToArray();
            int[] values = keys.Select(key => PlayerPrefs.GetInt(key)).ToArray();
            var originalInput = InputSystem.settings;
            var originalFlags = originalInput.hideFlags;
            originalInput.hideFlags = HideFlags.DontUnloadUnusedAsset;
            var testInput = Object.Instantiate(originalInput);
            testInput.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings = testInput;
            var mouse = InputSystem.AddDevice<Mouse>();
            GameObject events = null;
            if (EventSystem.current == null)
            {
                events = new GameObject("Display test input", typeof(EventSystem), typeof(InputSystemUIInputModule));
                events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            }
            try
            {
                var native = Screen.currentResolution;
                var sizes = new[] { new Vector2Int(1280, 720), new Vector2Int(1024, 768), new Vector2Int(native.width, native.height) };
                for (int i = 0; i < sizes.Length; i++)
                {
                    FullScreenMode mode = i < 2 ? FullScreenMode.Windowed : FullScreenMode.FullScreenWindow;
                    TerminalDisplayOptions.Apply(sizes[i].x, sizes[i].y, mode);
                    float deadline = Time.realtimeSinceStartup + 8;
                    do { yield return null; }
                    while ((Screen.fullScreenMode != mode || Screen.width != sizes[i].x || Screen.height != sizes[i].y) &&
                        Time.realtimeSinceStartup < deadline);
                    Assert.That(Screen.fullScreenMode, Is.EqualTo(mode));
                    Assert.That(Screen.width, Is.EqualTo(sizes[i].x));
                    Assert.That(Screen.height, Is.EqualTo(sizes[i].y));
                    Debug.Log("Verified display: " + mode + " " + Screen.width + " x " + Screen.height);
                    if (i == 1)
                    {
                        yield return ClickDisplayButton(mouse, "REVERT");
                        yield return new WaitForSecondsRealtime(.5f);
                        Assert.That(Screen.width, Is.EqualTo(1280), "Reverting must restore the actual window size.");
                        Assert.That(Screen.height, Is.EqualTo(720));
                    }
                    else
                    {
                        yield return ClickDisplayButton(mouse, "KEEP THIS MODE");
                        Assert.That(PlayerPrefs.GetInt(keys[0]), Is.EqualTo(sizes[i].x));
                    }
                    yield return null;
                    GamePreferences.BeginEdit();
                    GamePreferences.SetOption("SafeArea", 0);
                    TerminalCalibration.Boundaries();
                    yield return null; yield return null;
                    Canvas.ForceUpdateCanvases();
                    var guides = GameObject.Find("Screen boundary guides").transform.Find("Boundary preview");
                    var corners = new Vector3[4];
                    foreach (RectTransform bracket in guides)
                    {
                        bracket.GetWorldCorners(corners);
                        foreach (var corner in corners)
                        {
                            Assert.That(corner.x, Is.InRange(0, Screen.width));
                            Assert.That(corner.y, Is.InRange(0, Screen.height));
                        }
                    }
                    yield return ClickDisplayButton(mouse, "X");
                    GamePreferences.EndEdit();
                }
                int committedWidth = PlayerPrefs.GetInt(keys[0]);
                int committedHeight = PlayerPrefs.GetInt(keys[1]);
                TerminalDisplayOptions.BeginEdit();
                TerminalDisplayOptions.Apply(1024, 768, FullScreenMode.Windowed);
                yield return new WaitForSecondsRealtime(.6f);
                yield return ClickDisplayButton(mouse, "KEEP THIS MODE");
                Assert.That(TerminalDisplayOptions.HasChanges, Is.True);
                Assert.That(PlayerPrefs.GetInt(keys[0]), Is.EqualTo(committedWidth), "Keep must not commit a settings preview.");
                TerminalDisplayOptions.DiscardEdits();
                yield return new WaitForSecondsRealtime(.6f);
                Assert.That(Screen.width, Is.EqualTo(committedWidth));
                Assert.That(Screen.height, Is.EqualTo(committedHeight));
                Assert.That(Screen.fullScreenMode, Is.EqualTo(FullScreenMode.FullScreenWindow));
                TerminalDisplayOptions.Apply(1024, 768, FullScreenMode.Windowed);
                yield return new WaitForSecondsRealtime(.6f);
                yield return ClickDisplayButton(mouse, "KEEP THIS MODE");
                TerminalDisplayOptions.ApplyEdits();
                Assert.That(PlayerPrefs.GetInt(keys[0]), Is.EqualTo(1024));
                Assert.That(PlayerPrefs.GetInt(keys[1]), Is.EqualTo(768));
                Assert.That(TerminalDisplayOptions.HasChanges, Is.False);
            }
            finally
            {
                if (TerminalModal.Current != null) TerminalModal.Current.Close();
                GamePreferences.EndEdit();
                TerminalDisplayOptions.EndEdit();
                Screen.SetResolution(oldWidth, oldHeight, oldMode);
                for (int i = 0; i < keys.Length; i++)
                    if (existed[i]) PlayerPrefs.SetInt(keys[i], values[i]);
                    else PlayerPrefs.DeleteKey(keys[i]);
                PlayerPrefs.Save();
                InputSystem.RemoveDevice(mouse);
                InputSystem.settings = originalInput;
                originalInput.hideFlags = originalFlags;
                Object.Destroy(testInput);
                if (events != null) Object.Destroy(events);
            }
        }
    }
}
