using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SilentSuns.Menu.Tests
{
    public partial class TerminalMenuTests
    {
        private IEnumerator WaitForFailureChoices()
        {
            float deadline = Time.realtimeSinceStartup + 8;
            var failure = Object.FindFirstObjectByType<TerminalFailure>();
            while (failure != null && !failure.ChoicesReady && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(failure, Is.Not.Null);
            Assert.That(failure.ChoicesReady, Is.True);
        }

        [UnityTest, Category("Menu"), Category("Failure")]
        public IEnumerator DeathGlitchesTheLastViewBeforeShowingChoices()
        {
            yield return LoadSurface();
            var resources = TerminalGameSession.Manager.Resources;
            resources.DrainOxygen(resources.OxygenCapacity);
            yield return null;
            yield return null;
            Assert.That(TerminalFailure.Active, Is.True);
            Assert.That(TerminalPause.BlocksGameplayInput, Is.True);
            Assert.That(Time.timeScale, Is.Zero);
            Object.Destroy(CaptureGameplay("death-01-onset"));
            yield return new WaitForSecondsRealtime(1);
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("ObjectiveCheck"), "Death must begin in the player's scene, not jump straight to the light.");
            var transition = Object.FindFirstObjectByType<TerminalFailure>();
            var glitch = (Material)ReadMember(transition, "signal");
            var lastView = (Texture2D)ReadMember(transition, "lastView");
            Assert.That(glitch.shader, Is.SameAs(TerminalUIAssets.Current.crtMaterial.shader), "Death must reuse the ship's glitch effect.");
            Assert.That(glitch, Is.Not.SameAs(TerminalUIAssets.Current.crtMaterial));
            Assert.That(lastView.width, Is.GreaterThan(1), "The effect needs the actual scene, not a blank noise texture.");
            Assert.That(glitch.GetFloat("_HandoffFault"), Is.LessThan(.5f), "The glitch must build up gradually.");
            float lightStart = (float)ReadMember(transition, "lightStarted");
            Object.Destroy(CaptureGameplay("death-02-fading"));
            yield return new WaitForSecondsRealtime(2.5f);
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("ObjectiveCheck"));
            Assert.That(glitch.GetFloat("_HandoffFault"), Is.GreaterThan(.6f));
            var approaching = (Material)ReadMember(transition, "wake");
            Assert.That(approaching.GetFloat("_OverScene"), Is.EqualTo(1));
            Assert.That(approaching.GetFloat("_Reveal"), Is.GreaterThan(.5f), "The light must arrive while the scene is still glitching.");
            Assert.That(approaching.GetFloat("_Blackout"), Is.InRange(.01f, .9f), "Darkness should still be closing in around the light.");
            Object.Destroy(CaptureGameplay("death-03-glitch"));
            yield return new WaitForSecondsRealtime(1.1f);
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("ObjectiveCheck"), "Let the failing picture hold before fading it away.");
            Object.Destroy(CaptureGameplay("death-04-closing-vision"));
            float deadline = Time.realtimeSinceStartup + 5;
            while (SceneManager.GetActiveScene().name != "GameOverScene" && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("GameOverScene"));
            var failure = Object.FindFirstObjectByType<TerminalFailure>();
            Assert.That((float)ReadMember(failure, "lightStarted"), Is.EqualTo(lightStart), "Changing scenes must not restart the orb's pulse.");
            Assert.That(((Material)ReadMember(failure, "wake")).GetFloat("_Reveal"), Is.EqualTo(1), "The light must stay visible when the last of the scene disappears.");
            var restart = Object.FindObjectsByType<TerminalButton>(FindObjectsSortMode.None).First(b => b.name.Contains("RESTART SURVEY"));
            Assert.That(restart.IsInteractable(), Is.False);
            restart.onClick.Invoke();
            Assert.That(TerminalTransition.IsActive, Is.False, "Hidden choices must ignore accidental presses.");
            Object.Destroy(CaptureGameplay("death-05-light-remains"));
            yield return new WaitForSecondsRealtime(1);
            Assert.That(failure.ChoicesReady, Is.False);
            Object.Destroy(CaptureGameplay("death-06-invitation"));
            yield return new WaitForSecondsRealtime(1.6f);
            yield return WaitForFailureChoices();
            Assert.That(restart.IsInteractable(), Is.True);
            Assert.That(resources.OxygenSeconds, Is.Zero, "Revealing the choices must not start a new run.");
            Object.Destroy(CaptureGameplay("death-07-choices"));
            Object.FindObjectsByType<TerminalButton>(FindObjectsSortMode.None).First(b => b.name.Contains("MAIN MENU")).onClick.Invoke();
            yield return WaitForTransition();
            Assert.That(TerminalMenu.Active.State, Is.EqualTo(TerminalMenu.PowerState.MainMenu));
            Assert.That(TerminalFailure.Active, Is.False);
        }

        [UnityTest, Category("Menu"), Category("Failure")]
        public IEnumerator ReducedMotionKeepsTheLightStillAndControlsGated()
        {
            GamePreferences.SetReducedMotion(true);
            yield return LoadSurface();
            TerminalGameSession.Manager.Resources.DrainOxygen(300);
            yield return new WaitForSecondsRealtime(1);
            var transition = Object.FindFirstObjectByType<TerminalFailure>();
            var glitch = (Material)ReadMember(transition, "signal");
            Assert.That(glitch.GetFloat("_InterferenceStrength"), Is.Zero);
            Assert.That(glitch.GetFloat("_HandoffFault"), Is.Zero);
            Assert.That(glitch.GetFloat("_DisplayTime"), Is.Zero);
            float deadline = Time.realtimeSinceStartup + 8;
            while (SceneManager.GetActiveScene().name != "GameOverScene" && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("GameOverScene"));
            yield return new WaitForSecondsRealtime(1.2f);
            var failure = Object.FindFirstObjectByType<TerminalFailure>();
            var light = (Material)ReadMember(failure, "wake");
            Assert.That(light.GetFloat("_ReducedMotion"), Is.EqualTo(1));
            Assert.That(light.GetFloat("_Pulse"), Is.EqualTo(.5f));
            Assert.That(light.GetVector("_Origin"), Is.EqualTo(new Vector4(.5f, .56f, 0, 0)));
            Assert.That(failure.ChoicesReady, Is.False);
            yield return WaitForFailureChoices();
            Assert.That(light.GetVector("_Origin"), Is.EqualTo(new Vector4(.5f, .56f, 0, 0)));
        }
    }
}
