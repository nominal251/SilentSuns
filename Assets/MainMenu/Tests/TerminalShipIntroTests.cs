using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace SilentSuns.Menu.Tests
{
    public partial class TerminalMenuTests
    {
        [UnityTest, Category("Menu")]
        public IEnumerator ColdScreenWarmsBeforeInvitation()
        {
            var menu = terminal.GetComponent<TerminalMenu>();
            var display = terminal.Find("CRT Display").GetComponent<TerminalDisplay>();
            var face = terminal.Find("Standby/Wake screen").GetComponent<Image>();
            var prompt = terminal.Find("Standby/Wake screen/Invitation").GetComponent<TMP_Text>();
            Assert.That(menu.State, Is.EqualTo(TerminalMenu.PowerState.WarmingUp));
            Assert.That(display.Illumination, Is.LessThan(.02f));
            Assert.That(prompt.text, Is.Empty);
            Assert.That(face.material.GetFloat("_Reveal"), Is.Zero);
            Object.Destroy(CaptureGameplay("00-cold-glass"));
            menu.PowerOn();
            Assert.That(menu.State, Is.EqualTo(TerminalMenu.PowerState.WarmingUp), "Early input cannot skip the arrival.");
            yield return new WaitForSecondsRealtime(1.3f);
            Assert.That(display.Illumination, Is.InRange(.01f, .95f));
            Assert.That(face.material.GetFloat("_Reveal"), Is.Zero, "The orb must wait until the raster has settled.");
            Assert.That(prompt.text, Is.Empty);
            Object.Destroy(CaptureGameplay("00-raster-warming"));
            yield return WaitForState(TerminalMenu.PowerState.WaitingForTouch);
            while (!menu.InvitationReady) yield return null;
            Assert.That(display.Illumination, Is.EqualTo(1));
            Assert.That(face.material.GetFloat("_Reveal"), Is.EqualTo(1));
            Assert.That(prompt.text, Is.EqualTo("hello?"));
            Assert.That(terminal.GetComponentInChildren<TerminalPowerAudio>().Running, Is.True);
            Assert.That(terminal.Find("Standby/Wake screen/Wake action"), Is.Null);
            Object.Destroy(CaptureGameplay("00-first-invitation"));
        }

        [UnityTest, Category("Menu")]
        public IEnumerator TelescopePulseInvitesTouchOnShipScreen()
        {
            var display = terminal.Find("CRT Display").GetComponent<TerminalDisplay>();
            var screen = GameObject.Find("Telescope/ProbeLauncherScreen").GetComponent<Renderer>();
            Assert.That(display.ShipScreen, Is.SameAs(screen));
            foreach (var casing in screen.transform.parent.GetComponentsInChildren<MeshRenderer>())
                if (casing != screen)
                {
                    Assert.That(casing.GetComponent<MeshFilter>().sharedMesh.subMeshCount, Is.EqualTo(1));
                    Assert.That(casing.sharedMaterials.Length, Is.EqualTo(2));
                    Assert.That(casing.sharedMaterials[0].shader.name, Does.Not.Contain("Ship Screen Spill"), "Native casing lighting must remain present throughout startup.");
                }
            Assert.That(Time.timeScale, Is.Zero);
            Assert.That(TerminalPause.BlocksGameplayInput, Is.True);
            Assert.That(terminal.Find("Menu Music").GetComponent<AudioSource>().isPlaying, Is.False);
            Assert.That(display.TryGetContentPoint(new Vector2(2, 2), out _), Is.False, "Only a tap on the monitor should wake it.");
            var prompt = terminal.Find("Standby/Wake screen/Invitation").GetComponent<TMP_Text>();
            Assert.That(prompt.text, Is.EqualTo("hello?"));
            Assert.That(terminal.Find("Standby/Wake screen/Wake action"), Is.Null);
            var wake = terminal.Find("Standby/Wake screen").GetComponent<Image>().material;
            Assert.That(wake.shader.name, Is.EqualTo("Silent Suns/Wake Pulse"));
            Assert.That(Object.FindFirstObjectByType<TerminalShipIntro>().RoomLightLevel, Is.Zero);
            foreach (Light lamp in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (lamp.gameObject.scene == SceneManager.GetActiveScene()) Assert.That(lamp.intensity, Is.Zero);
            yield return new WaitForSecondsRealtime(2.2f);
            var dark = CaptureGameplay("01-hello-pulse");
            Assert.That(dark.GetPixel(100, 500).maxColorComponent, Is.LessThan(.025f), "The room around the monitor must be dark during the invitation.");
            Assert.That(dark.GetPixel(960, 598).b, Is.GreaterThan(dark.GetPixel(1100, 598).b + .08f), "A luminous central orb must be visible, not just text and bands.");
            Object.Destroy(dark);
            float pulse = wake.GetFloat("_Pulse");
            yield return new WaitForSecondsRealtime(2.7f);
            Assert.That(prompt.text, Is.EqualTo("are you there?"));
            Assert.That(Mathf.Abs(wake.GetFloat("_Pulse") - pulse), Is.GreaterThan(.02f));
            yield return new WaitForSecondsRealtime(2.2f);
            Object.Destroy(CaptureGameplay("02-are-you-there"));
            yield return new WaitForSecondsRealtime(.3f);
            Assert.That(prompt.text, Is.EqualTo("i need you to respond."));
            yield return new WaitForSecondsRealtime(3.6f);
            Assert.That(prompt.text, Is.EqualTo("touch the light."));
            Click("Standby/Wake screen");
            yield return WaitForState(TerminalMenu.PowerState.Booting);
            yield return new WaitForSecondsRealtime(2);
            Object.Destroy(CaptureGameplay("03-telescope-diagnostics"));
            Assert.That(display.SourceCamera.aspect, Is.EqualTo(display.ScreenRight.magnitude / display.ScreenUp.magnitude).Within(.001f));
            Assert.That(display.EffectMaterial.GetFloat("_ContentFit"), Is.EqualTo(1), "The whole native display should be filled without letterboxing.");
        }

        [UnityTest, Category("Menu")]
        public IEnumerator StartupLightingRestoresSceneExactly()
        {
            yield return SceneManager.LoadSceneAsync("SampleScene");
            yield return null; yield return null;
            var screen = GameObject.Find("Telescope/ProbeLauncherScreen").GetComponent<Renderer>();
            Light[] lamps = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
            var levels = new float[lamps.Length];
            for (int i = 0; i < lamps.Length; i++) levels[i] = lamps[i].intensity;
            float ambient = RenderSettings.ambientIntensity, reflections = RenderSettings.reflectionIntensity;
            var mode = RenderSettings.ambientMode;
            var cameraFlags = Camera.main.clearFlags;
            var casing = screen.transform.parent.GetComponentInChildren<MeshRenderer>();
            var before = new MaterialPropertyBlock(); casing.GetPropertyBlock(before, 0);
            using (var lights = new TerminalRoomLighting(screen, Camera.main))
            {
                Assert.That(RenderSettings.reflectionIntensity, Is.Zero);
                for (int i = 0; i < lamps.Length; i++) Assert.That(lamps[i].intensity, Is.Zero);
                lights.SetLevel(.4f);
                for (int i = 0; i < lamps.Length; i++) Assert.That(lamps[i].intensity, Is.EqualTo(levels[i] * .4f).Within(.00001f));
                yield return null;
            }
            for (int i = 0; i < lamps.Length; i++) Assert.That(lamps[i].intensity, Is.EqualTo(levels[i]));
            Assert.That(RenderSettings.ambientIntensity, Is.EqualTo(ambient));
            Assert.That(RenderSettings.reflectionIntensity, Is.EqualTo(reflections));
            Assert.That(RenderSettings.ambientMode, Is.EqualTo(mode));
            Assert.That(Camera.main.clearFlags, Is.EqualTo(cameraFlags));
            var after = new MaterialPropertyBlock(); casing.GetPropertyBlock(after, 0);
            Assert.That(after.isEmpty, Is.EqualTo(before.isEmpty));
        }

        [UnityTest, Category("Menu")]
        public IEnumerator NativeInstrumentReturnsWithoutBrightnessJump()
        {
            var display = terminal.Find("CRT Display").GetComponent<TerminalDisplay>();
            Assert.That(display.ShipScreen.GetComponent<MeshFilter>().sharedMesh.subMeshCount, Is.EqualTo(1));
            Assert.That(display.ShipScreen.sharedMaterials[0].name, Is.EqualTo("TelescopeTexture"));
            display.SetGameplayBlend(1);
            yield return null;
            var before = CaptureGameplay(null);
            display.RestoreInstrument();
            var after = CaptureGameplay(null);
            float jump = 0;
            for (int y = 150; y < 900; y += 20)
                for (int x = 450; x < 1500; x += 20)
                {
                    Color a = before.GetPixel(x, y), b = after.GetPixel(x, y);
                    jump = Mathf.Max(jump, Mathf.Abs(a.r - b.r), Mathf.Abs(a.g - b.g), Mathf.Abs(a.b - b.b));
                }
            Assert.That(jump, Is.LessThan(.01f), "Removing the fully faded boot layer must preserve the instrument's actual brightness.");
            Object.Destroy(before); Object.Destroy(after);
        }

        [UnityTest, Category("Menu")]
        public IEnumerator ReducedEffectsKeepsTheFullSurveyHandoff()
        {
            yield return ReadyMain();
            terminal.GetComponent<TerminalMenu>().SetReducedMotion(true);
            float started = Time.unscaledTime;
            TerminalShipIntro.BeginSurvey();
            yield return new WaitForSecondsRealtime(1.2f);
            Assert.That(TerminalShipIntro.IsActive, Is.True, "Reduced effects must not rush the camera into gameplay.");
            Assert.That(TerminalPause.BlocksGameplayInput, Is.True);
            float deadline = started + 10;
            while (TerminalShipIntro.IsActive && Time.unscaledTime < deadline) yield return null;
            Assert.That(TerminalShipIntro.IsActive, Is.False);
            Assert.That(Time.unscaledTime - started, Is.InRange(6.1f, 8f));
        }

        [UnityTest, Category("Menu")]
        public IEnumerator NewSurveyRevealsShipMonitorAndOnlyThenShowsHint()
        {
            Camera originalCamera = Camera.main;
            var sceneHandle = SceneManager.GetActiveScene().handle;
            Click("Standby/Wake screen");
            yield return WaitForState(TerminalMenu.PowerState.Booting);
            Click("Boot/Skip Boot");
            yield return new WaitForSecondsRealtime(.35f);
            Click("Main Menu/Begin Survey");
            Assert.That(Cursor.visible, Is.False, "Hide the system pointer as soon as Begin Survey is activated.");
            if (!Application.isEditor)
                Assert.That(Cursor.lockState, Is.EqualTo(CursorLockMode.Locked));
            float deadline = Time.realtimeSinceStartup + 30;
            while (SceneManager.GetActiveScene().name != "SampleScene" && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(TerminalShipIntro.IsActive, Is.True);
            Assert.That(TerminalShipIntro.HintVisible, Is.False);
            Assert.That(TerminalPause.BlocksGameplayInput, Is.True);
            yield return new WaitForSecondsRealtime(.35f);
            var closeup = CaptureGameplay("40-console-closeup");
            float bluePicture = 0;
            for (int y = 350; y < 700; y += 25)
                for (int x = 700; x < 1200; x += 25)
                    bluePicture += closeup.GetPixel(x, y).b;
            Assert.That(bluePicture / (14 * 20), Is.GreaterThan(.1f), "The existing ship screen must show the boot picture, not a cleared render texture.");
            Object.Destroy(closeup);
            var monitor = Object.FindFirstObjectByType<TerminalShipIntro>();
            Assert.That(monitor.Illumination, Is.GreaterThan(.95f));
            var pause = Object.FindFirstObjectByType<TerminalPause>();
            pause.Pause();
            Assert.That(pause.IsPaused, Is.False, "Pause cannot steal focus during the handoff.");
            float recoveryStart = Time.unscaledTime;
            float previousRoom = monitor.RoomLightLevel;
            int powerFailures = 0;
            bool nativeWasInterrupted = false;
            var bootDisplay = terminal.GetComponentInChildren<TerminalDisplay>(true);
            while (TerminalShipIntro.IsActive)
            {
                Assert.That(Camera.main, Is.SameAs(originalCamera));
                Assert.That(SceneManager.GetActiveScene().handle, Is.EqualTo(sceneHandle));
                Assert.That(TerminalTransition.IsActive, Is.False, "Begin Survey must not cover or reload the ship.");
                float room = monitor.RoomLightLevel;
                if (room < previousRoom - .07f) powerFailures++;
                previousRoom = room;
                if (Time.unscaledTime - recoveryStart < 4.1f)
                    Assert.That(room, Is.LessThanOrEqualTo(.24f), "The room must stay mostly dark through the failed recoveries.");
                Material signal = bootDisplay.EffectMaterial;
                if (signal.GetFloat("_GameplayBlend") > .9f && signal.GetFloat("_HandoffFault") > .5f)
                    nativeWasInterrupted = true;
                yield return null;
            }
            Assert.That(powerFailures, Is.GreaterThanOrEqualTo(3), "Room power must visibly fail with the screen recovery attempts.");
            Assert.That(nativeWasInterrupted, Is.True, "Interference must persist as the actual telescope interface comes back.");
            Assert.That(monitor.RoomLightLevel, Is.EqualTo(1));
            Object.Destroy(CaptureGameplay("41-console-pullback"));
            yield return WaitForTransition();
            Assert.That(TerminalShipIntro.IsActive, Is.False);
            Assert.That(TerminalPause.BlocksGameplayInput, Is.False);
            Assert.That(TerminalShipIntro.HintVisible, Is.True);
            Assert.That(Camera.main.fieldOfView, Is.EqualTo(60).Within(.01f));
            Assert.That(Time.timeScale, Is.EqualTo(1));
            Assert.That(Camera.main.GetComponentInParent<Rigidbody>().isKinematic, Is.False);
            Assert.That(monitor.Illumination, Is.Zero);
            var original = GameObject.Find("Telescope/ProbeLauncherScreen").GetComponent<Renderer>();
            Assert.That(original.enabled, Is.True);
            Assert.That(monitor.Screen, Is.SameAs(original));
            Assert.That(original.sharedMaterial.name, Is.EqualTo("TelescopeTexture"));
            yield return new WaitForSecondsRealtime(.5f);
            Object.Destroy(CaptureGameplay("42-operator-notice"));
            var notice = GameObject.Find("First survey ESC notice/BIOS notice/Message").GetComponent<TMP_Text>();
            Assert.That(notice.text, Does.EndWith("You can press ESC to access your pause menu at any time"));
            notice.ForceMeshUpdate();
            Assert.That(notice.isTextOverflowing, Is.False);
            pause.Pause();
            yield return new WaitForSecondsRealtime(.3f);
            var display = pause.GetComponent<TerminalPauseDisplay>();
            Assert.That(display, Is.Not.Null);
            RenderPipeline.SubmitRenderRequest(display.SourceCamera, new UniversalRenderPipeline.SingleCameraRequest { destination = display.PanelTexture });
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = display.PanelTexture;
            var rgba = new Texture2D(display.PanelTexture.width, display.PanelTexture.height, TextureFormat.RGBA32, false);
            rgba.ReadPixels(new Rect(0, 0, rgba.width, rgba.height), 0, 0); rgba.Apply();
            RenderTexture.active = previous;
            Assert.That(rgba.GetPixel(4, 4).a, Is.Zero.Within(.001f));
            Assert.That(rgba.GetPixel(rgba.width - 5, rgba.height - 5).a, Is.Zero.Within(.001f));
            Assert.That(rgba.GetPixel(rgba.width / 2, rgba.height / 2).a, Is.GreaterThan(.99f));
            Object.Destroy(rgba);
            var face = display.Presentation.GetComponentInChildren<RawImage>();
            Material effect = face.material;
            effect.SetFloat("_DisplayTime", 0);
            effect.SetFloat("_DiagnosticGlitch", 0);
            effect.SetFloat("_Disturbance", 0);
            var calm = CaptureGameplay(null);
            effect.SetFloat("_DisplayTime", 300);
            effect.SetFloat("_DiagnosticGlitch", 1);
            var glitch = CaptureGameplay("43-pause-panel-glitch");
            float outside = 0, inside = 0;
            for (int y = 100; y < 950; y += 10)
                for (int x = 100; x < 1800; x += 10)
                {
                    float difference = Mathf.Abs(calm.GetPixel(x, y).grayscale - glitch.GetPixel(x, y).grayscale);
                    if (x < 350 || x > 1570) outside = Mathf.Max(outside, difference);
                    else inside += difference;
                }
            Assert.That(outside, Is.LessThan(.005f), "Animated CRT effects must not change the gameplay pixels outside the pause panel.");
            Assert.That(inside, Is.GreaterThan(.1f), "The shader must actually affect the panel.");
            Object.Destroy(calm); Object.Destroy(glitch);
            effect.SetFloat("_DiagnosticGlitch", 0);
            pause.OpenSettings();
            yield return null;
            Assert.That(pause.transform.Find("Layout/Settings/Tabbed settings/BACK [ESC]/Label").GetComponent<TMP_Text>().text, Is.EqualTo("BACK [ESC]"));
            var slider = SettingsSlider("AUDIO", "GAMEPLAY SOUND EFFECTS", pause.GetComponentInChildren<TerminalSettings>());
            Canvas.ForceUpdateCanvases();
            var area = slider.handleRect.parent as RectTransform;
            var pointer = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left };
            pointer.pointerPressRaycast = new RaycastResult { module = pause.GetComponent<GraphicRaycaster>() };
            pointer.position = display.SourceCamera.WorldToScreenPoint(area.TransformPoint(new Vector3(Mathf.Lerp(area.rect.xMin, area.rect.xMax, .7f), area.rect.center.y)));
            ExecuteEvents.Execute(slider.gameObject, pointer, ExecuteEvents.dragHandler);
            Assert.That(slider.value, Is.EqualTo(.7f).Within(.03f), "Rendering into a texture must preserve mouse slider coordinates.");
            pause.Resume(); yield return new WaitForSecondsRealtime(.3f);
            // A direct scene load (and future save restore) must not replay a new-game introduction.
            yield return SceneManager.LoadSceneAsync("SampleScene"); yield return null;
            Assert.That(TerminalShipIntro.IsActive, Is.False);
            Assert.That(TerminalShipIntro.HintVisible, Is.False);
            Assert.That(Object.FindFirstObjectByType<TerminalShipIntro>(), Is.Null);
        }

        [UnityTest, Category("Menu")]
        public IEnumerator BootContradictionsAreReadableAndTimeLimited()
        {
            Click("Standby/Wake screen");
            yield return WaitForState(TerminalMenu.PowerState.Booting);
            var log = terminal.GetComponent<TerminalBootLog>();
            var output = terminal.Find("Boot/Boot output").GetComponent<TMP_Text>();
            foreach (var expected in new[] { "MOTION: 04 RETURNS / MANIFEST: 0", "RELEASE REQUEST / ORIGIN INSIDE", "RX: 128 BYTES / ANTENNA DISCONNECTED", "FRAME TIME: +00:08 / CLOCK SYNC: OK", "BODY COUNT: 02 / MANIFEST: 01", "RETURN 02: 0.4 M / REAR SENSOR", "INNER HANDLE ACTIVE / MANIFEST: 0", "0.0 KPA / MOTION: 06 RETURNS" })
            {
                float deadline = Time.realtimeSinceStartup + 10;
                while (!output.text.Contains(expected) && Time.realtimeSinceStartup < deadline) yield return null;
                Assert.That(output.text, Does.Contain(expected));
                Assert.That(output.text, Does.Contain("<color=#FFD3A0>"));
                Capture("anomaly-" + Mathf.FloorToInt(log.Elapsed), 1920, 1080);
                yield return new WaitForSecondsRealtime(.7f);
                Assert.That(output.text, Does.Contain("<color=#FFD3A0>"), "Each contradiction needs a readable hold.");
                output.ForceMeshUpdate();
                Assert.That(output.isTextOverflowing, Is.False, "The diagnostic line must fit the actual ship screen.");
            }
            while (log.Running) yield return null;
            Assert.That(terminal.GetComponentInChildren<TerminalDisplay>().DiagnosticGlitch, Is.Zero);
        }

        private Texture2D CaptureGameplay(string name)
        {
            Camera camera = Camera.main;
            var overlays = new List<Canvas>();
            foreach (Canvas canvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
                if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    overlays.Add(canvas); canvas.renderMode = RenderMode.ScreenSpaceCamera;
                    canvas.worldCamera = camera; canvas.planeDistance = camera.nearClipPlane + .05f;
                }
            float aspect = camera.aspect;
            camera.aspect = 16f / 9f;
            var target = new RenderTexture(1920, 1080, 24); target.Create();
            camera.targetTexture = target;
            Canvas.ForceUpdateCanvases();
            var pauseDisplay = Object.FindFirstObjectByType<TerminalPauseDisplay>();
            if (pauseDisplay != null && pauseDisplay.SourceCamera.enabled)
                RenderPipeline.SubmitRenderRequest(pauseDisplay.SourceCamera, new UniversalRenderPipeline.SingleCameraRequest { destination = pauseDisplay.PanelTexture });
            // Include the team's instrument cameras, which feed the ship's real displays.
            foreach (Camera instrument in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
                if (instrument != camera && instrument.enabled && instrument.targetTexture != null && instrument != pauseDisplay?.SourceCamera)
                    RenderPipeline.SubmitRenderRequest(instrument, new UniversalRenderPipeline.SingleCameraRequest { destination = instrument.targetTexture });
            RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
            var image = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
            var previous = RenderTexture.active; RenderTexture.active = target;
            image.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0); image.Apply();
            RenderTexture.active = previous;
            string folder = System.Environment.GetEnvironmentVariable("SILENT_SUNS_MENU_CAPTURES");
            if (name != null && !string.IsNullOrEmpty(folder))
            {
                Directory.CreateDirectory(folder);
                File.WriteAllBytes(Path.Combine(folder, name + ".png"), image.EncodeToPNG());
            }
            camera.targetTexture = null; camera.aspect = aspect;
            foreach (Canvas canvas in overlays) canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            target.Release(); Object.Destroy(target);
            return image;
        }
    }
}
