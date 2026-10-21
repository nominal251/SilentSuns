#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Unity.Collections;
using UnityEditor;
using UnityEditor.Media;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SilentSuns.Menu.Tests
{
    public partial class TerminalMenuTests
    {
        // Opt-in runtime recording. The test setup restores all player preferences afterwards.
        [UnityTest]
        [Category("Recording")]
        public IEnumerator RecordCommandBootWalkthrough()
        {
            string path = System.Environment.GetEnvironmentVariable("SILENT_SUNS_MENU_VIDEO");
            if (string.IsNullOrEmpty(path)) Assert.Ignore("Set SILENT_SUNS_MENU_VIDEO to record the runtime.");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            SceneManager.sceneLoaded += IsolateUnwiredTrackers;
            int originalRate = Time.captureFramerate;
            Time.captureFramerate = 30;
            var video = new VideoTrackAttributes { frameRate = new MediaRational(30), width = 1280, height = 720, includeAlpha = false, bitRateMode = VideoBitrateMode.High };
            var audio = new AudioTrackAttributes { sampleRate = new MediaRational(AudioSettings.outputSampleRate), channelCount = 2, language = "" };
            var target = new RenderTexture(1280, 720, 24);
            target.Create();
            var pixels = new Texture2D(1280, 720, TextureFormat.RGBA32, false);
            Assert.That(AudioRenderer.Start(), Is.True);
            int phase = 0, phaseFrames = 0, totalFrames = 0;
            float previousTime = Time.unscaledTime;
            var captureClock = System.Diagnostics.Stopwatch.StartNew();
            float bootStart = 0;
            bool done = false;
            bool pauseHoverShown = false;
            using (var encoder = new MediaEncoder(path, video, audio))
            try
            {
                while (!done && totalFrames < 3300)
                {
                    // Unscaled menu time follows the real clock. Pace the capture at the
                    // encoder rate so the recorded audio and unscaled animations agree.
                    double remaining = (totalFrames + 1) * 1000.0 / 30.0 - captureClock.Elapsed.TotalMilliseconds;
                    if (remaining >= 1.0) System.Threading.Thread.Sleep((int)remaining);
                    yield return null;
                    TerminalMenu menu = terminal != null ? terminal.GetComponent<TerminalMenu>() : null;
                    TerminalPause pause = Object.FindFirstObjectByType<TerminalPause>();
                    bool next = false;
                    switch (phase)
                    {
                        case 0:
                            if (phaseFrames >= 560 && menu.InvitationReady) { Click("Standby/Wake screen"); bootStart = Time.unscaledTime; next = true; }
                            break;
                        case 1:
                            if (menu.State == TerminalMenu.PowerState.MainMenu)
                            {
                                Assert.That(Time.unscaledTime - bootStart, Is.GreaterThan(22f));
                                next = true;
                            }
                            break;
                        case 2:
                            if (phaseFrames >= 90) { Click("Main Menu/Terminal Settings"); next = true; }
                            break;
                        case 3:
                            if (phaseFrames >= 90) { Click("Settings/Settings Back"); next = true; }
                            break;
                        case 4:
                            if (phaseFrames >= 36) { Click("Main Menu/Operator Guide"); next = true; }
                            break;
                        case 5:
                            if (phaseFrames >= 75) { Click("Operator Guide/Guide Back"); next = true; }
                            break;
                        case 6:
                            if (phaseFrames >= 45) { Click("Main Menu/Begin Survey"); next = true; }
                            break;
                        case 7:
                            if (SceneManager.GetActiveScene().name == "SampleScene" && !TerminalTransition.IsActive && !TerminalShipIntro.IsActive) next = true;
                            break;
                        case 8:
                            if (phaseFrames >= 90) { pause.Pause(); next = true; }
                            break;
                        case 9:
                            if (phaseFrames >= 30 && !pauseHoverShown)
                            {
                                Assert.That(EventSystem.current.currentSelectedGameObject, Is.Null, "Opening pause must not force a Resume highlight.");
                                var button = (RectTransform)pause.transform.Find("Layout/Pause/Settings");
                                Vector2 position = pause.GetComponent<TerminalPauseDisplay>().SourceCamera.WorldToScreenPoint(button.TransformPoint(button.rect.center));
                                InputSystem.QueueStateEvent(mouse, new MouseState { position = position, delta = new Vector2(20, 0) });
                                pauseHoverShown = true;
                            }
                            if (phaseFrames >= 75) { pause.OpenSettings(); next = true; }
                            break;
                        case 10:
                            if (phaseFrames >= 90) { pause.CloseSettings(); next = true; }
                            break;
                        case 11:
                            if (phaseFrames >= 45) { pause.Resume(); next = true; }
                            break;
                        case 12:
                            if (phaseFrames >= 90) { pause.Pause(); next = true; }
                            break;
                        case 13:
                            if (phaseFrames >= 30) { pause.AskReturnToMenu(); next = true; }
                            break;
                        case 14:
                            if (phaseFrames >= 60) { pause.ConfirmLeave(); next = true; }
                            break;
                        case 15:
                            if (TerminalShipIntro.Ready && Object.FindFirstObjectByType<TerminalMenu>() != null && Object.FindFirstObjectByType<TerminalMenu>().InvitationReady && !TerminalTransition.IsActive)
                            { terminal = GameObject.Find("ARCA Terminal").transform; next = true; }
                            break;
                        case 16:
                            if (phaseFrames >= 60) done = true;
                            break;
                    }
                    if (next)
                    {
                        Debug.Log("VIDEO phase " + phase + " completed at frame " + totalFrames);
                        phase++;
                        phaseFrames = 0;
                    }

                    Camera camera = Camera.main;
                    var overlays = new List<Canvas>();
                    if (camera != null)
                    {
                        camera.aspect = 16f / 9f;
                        camera.targetTexture = target;
                        TerminalDisplay display = Object.FindFirstObjectByType<TerminalDisplay>(FindObjectsInactive.Include);
                        if (terminal != null) terminal.SendMessage("FitCamera");
                        foreach (Canvas canvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
                            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                            {
                                overlays.Add(canvas);
                                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                                canvas.worldCamera = camera;
                                canvas.planeDistance = camera.nearClipPlane + (canvas.sortingOrder >= 1000 ? .01f : .05f);
                            }
                        Canvas.ForceUpdateCanvases();
                        if (display != null)
                        {
                            display.SendMessage("ResizeTexture");
                            RenderPipeline.SubmitRenderRequest(display.SourceCamera, new UniversalRenderPipeline.SingleCameraRequest { destination = display.SourceCamera.targetTexture });
                        }
                        var pauseDisplay = Object.FindFirstObjectByType<TerminalPauseDisplay>();
                        if (pauseDisplay != null && pauseDisplay.SourceCamera != null && pauseDisplay.SourceCamera.enabled)
                            RenderPipeline.SubmitRenderRequest(pauseDisplay.SourceCamera, new UniversalRenderPipeline.SingleCameraRequest { destination = pauseDisplay.PanelTexture });
                        foreach (Camera instrument in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
                            if (instrument != camera && instrument.enabled && instrument.targetTexture != null && instrument != display?.SourceCamera && instrument != pauseDisplay?.SourceCamera)
                                RenderPipeline.SubmitRenderRequest(instrument, new UniversalRenderPipeline.SingleCameraRequest { destination = instrument.targetTexture });
                        RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
                    }
                    else
                    {
                        // A scene activation can briefly have no tagged camera under the return fade.
                        RenderTexture beforeClear = RenderTexture.active;
                        RenderTexture.active = target;
                        GL.Clear(true, true, Color.black);
                        RenderTexture.active = beforeClear;
                    }
                    RenderTexture previous = RenderTexture.active;
                    RenderTexture.active = target;
                    pixels.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
                    pixels.Apply();
                    if (totalFrames % 150 == 0)
                        File.WriteAllBytes(Path.Combine(Path.GetDirectoryName(path), "video-" + totalFrames.ToString("D4") + ".png"), pixels.EncodeToPNG());
                    RenderTexture.active = previous;
                    if (camera != null) camera.targetTexture = null;
                    foreach (Canvas canvas in overlays) canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    // Preserve wall-clock pacing when the editor stalls to load a scene or
                    // read back a frame. Repeated frames retain that real pause in the video,
                    // and rendered audio advances by the same number of samples.
                    int framesDue = Mathf.Max(1, Mathf.RoundToInt((Time.unscaledTime - previousTime) * 30f) - totalFrames);
                    for (int frame = 0; frame < framesDue; frame++)
                    {
                        Assert.That(encoder.AddFrame(pixels), Is.True);
                        int sampleCount = AudioRenderer.GetSampleCountForCaptureFrame();
                        using (var samples = new NativeArray<float>(sampleCount * 2, Allocator.Temp))
                        {
                            Assert.That(AudioRenderer.Render(samples), Is.True);
                            Assert.That(encoder.AddSamples(samples), Is.True);
                        }
                    }
                    totalFrames += framesDue;
                    phaseFrames += framesDue;
                }
                Debug.Log("VIDEO complete frames=" + totalFrames + " simulatedSeconds=" + (Time.unscaledTime - previousTime));
                Assert.That(done, Is.True, "The full walkthrough must complete.");
                Assert.That(Mathf.Abs(totalFrames / 30f - (Time.unscaledTime - previousTime)), Is.LessThan(1.2f), "Capture must match real playback speed.");
            }
            finally
            {
                AudioRenderer.Stop();
                Time.captureFramerate = originalRate;
                target.Release();
                Object.Destroy(target);
                Object.Destroy(pixels);
            }
        }
    }
}
#endif
