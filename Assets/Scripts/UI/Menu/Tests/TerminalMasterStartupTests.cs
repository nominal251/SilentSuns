using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SilentSuns.Menu.Tests
{
    public partial class TerminalMenuTests
    {
        [UnityTest, Category("Menu")]
        public IEnumerator MasterComputerOwnsStartupAndReturnsToGameplay()
        {
            var computer = Object.FindFirstObjectByType<MasterComputerDisplay>();
            var intro = Object.FindFirstObjectByType<TerminalShipIntro>();
            var display = terminal.GetComponentInChildren<TerminalDisplay>();
            var telescope = GameObject.Find("Telescope/ProbeLauncherScreen").GetComponent<Renderer>();
            Material[] telescopeMaterials = telescope.sharedMaterials;
            Assert.That(intro.Screen, Is.SameAs(computer.Screen));
            Assert.That(display.ShipScreen, Is.SameAs(computer.Screen));
            Assert.That(computer.Screen.sharedMaterials.Length, Is.EqualTo(2));
            Assert.That(computer.Screen.sharedMaterials[0], Is.SameAs(computer.EffectMaterial));
            Assert.That(TerminalGameSession.Manager.Phase, Is.EqualTo(GameManager.GamePhase.Boot));
            Assert.That(TerminalGameSession.RestartScene, Is.EqualTo(TerminalShipIntro.ShipScene));
            Assert.That(TerminalGameSession.Events.Suspended, Is.True);
            Object.Destroy(CaptureGameplay("master-01-wake"));

            yield return ReadyMain();
            Object.Destroy(CaptureGameplay("master-02-menu"));
            var menu = terminal.GetComponent<TerminalMenu>();
            menu.OpenSettings();
            yield return new WaitForSecondsRealtime(.5f);
            AssertScreen("Settings");
            Object.Destroy(CaptureGameplay("master-03-settings"));
            menu.BackToMenu();
            yield return new WaitForSecondsRealtime(.5f);
            AssertScreen("Main Menu");

            Camera camera = Camera.main;
            var scene = SceneManager.GetActiveScene().handle;
            Click("Main Menu/Begin Survey");
            float deadline = Time.realtimeSinceStartup + 20;
            while (TerminalShipIntro.IsActive && Time.realtimeSinceStartup < deadline)
            {
                Assert.That(computer.UI.Power, Is.Zero, "The master computer must stay black throughout the handoff.");
                Assert.That(computer.UI.Ready, Is.False);
                yield return null;
            }
            Assert.That(TerminalShipIntro.IsActive, Is.False);
            // The intro deliberately blocks input through the following frame.
            yield return null;
            yield return null;
            Assert.That(Camera.main, Is.SameAs(camera));
            Assert.That(SceneManager.GetActiveScene().handle, Is.EqualTo(scene));
            Assert.That(Time.timeScale, Is.EqualTo(1));
            Assert.That(TerminalGameSession.Manager.Phase, Is.EqualTo(GameManager.GamePhase.SystemExploration));
            Assert.That(TerminalGameSession.Events.Suspended, Is.False);
            Assert.That(computer.Screen.sharedMaterials, Is.EqualTo(new[] { computer.EffectMaterial }));
            Assert.That(telescope.sharedMaterials, Is.EqualTo(telescopeMaterials));
            Assert.That(computer.UI.Ready, Is.False);
            Assert.That(computer.UI.InUse, Is.False);
            Assert.That(computer.UI.Power, Is.Zero);
            yield return null;
            Object.Destroy(CaptureGameplay("master-04-handoff"));

            // The real station must still acquire and release the player's controls.
            var station = Object.FindFirstObjectByType(GameplayType("MasterComputerStation"));
            Call(station, "Interact", camera.gameObject);
            yield return new WaitForSecondsRealtime(.7f);
            Assert.That(computer.UI.InUse, Is.True);
            deadline = Time.realtimeSinceStartup + 25;
            while (!computer.UI.Ready && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(computer.UI.Ready, Is.True, "Interacting should power on the computer normally.");
            Object.Destroy(CaptureGameplay("master-05-live-status"));
            computer.UI.SelectPage(1);
            yield return new WaitForSecondsRealtime(1);
            Assert.That((int)computer.UI.Page, Is.EqualTo(1));
            Assert.That(TerminalToolInput.TryExitOnEscape(), Is.True);
            yield return new WaitForSecondsRealtime(.7f);
            Assert.That(computer.UI.InUse, Is.False);
            Assert.That(TerminalToolInput.BlocksInput, Is.False);

            var pause = Object.FindFirstObjectByType<TerminalPause>();
            pause.Pause();
            Assert.That(pause.IsPaused, Is.True);
            pause.Resume();
            yield return new WaitForSecondsRealtime(.5f);
            TerminalMenu.PrepareReturn();
            yield return SceneManager.LoadSceneAsync("MainMenu");
            deadline = Time.realtimeSinceStartup + 20;
            while (!TerminalShipIntro.Ready && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(TerminalShipIntro.Ready, Is.True);
            yield return null;
            terminal = GameObject.Find("ARCA Terminal").transform;
            yield return WaitForState(TerminalMenu.PowerState.MainMenu);
            computer = Object.FindFirstObjectByType<MasterComputerDisplay>();
            Assert.That(terminal.GetComponentInChildren<TerminalDisplay>().ShipScreen, Is.SameAs(computer.Screen));
            Assert.That(TerminalGameSession.Manager.Phase, Is.EqualTo(GameManager.GamePhase.Boot));
            Assert.That(Time.timeScale, Is.Zero);
        }

        [UnityTest, Category("Menu")]
        public IEnumerator DirectShipLoadLeavesComputerAndPlayerAvailable()
        {
            yield return SceneManager.LoadSceneAsync(TerminalShipIntro.ShipScene);
            yield return null;
            Assert.That(TerminalShipIntro.IsActive, Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(1));
            var computer = Object.FindFirstObjectByType<MasterComputerDisplay>();
            Assert.That(computer.Screen.sharedMaterials, Is.EqualTo(new[] { computer.EffectMaterial }));
            Assert.That(computer.UI.Initialized, Is.True);
            Assert.That(computer.UI.InUse, Is.False);
            Assert.That(TerminalGameSession.Manager.Phase, Is.EqualTo(GameManager.GamePhase.SystemExploration));
        }
    }
}
