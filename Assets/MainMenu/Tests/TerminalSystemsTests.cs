using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SilentSuns.Menu.Tests
{
    public partial class TerminalMenuTests
    {
        [UnityTest, Category("Menu")]
        public IEnumerator InteractionEventsUsePlayerContextAndStopDuringPause()
        {
            yield return SceneManager.LoadSceneAsync("SampleScene");
            yield return null;
            var owner = new GameObject("Test event control");
            var trigger = owner.AddComponent<global::EventTrigger>();
            var definition = ScriptableObject.CreateInstance<GameEventDefinition>();
            definition.eventId = "interaction-context-check";
            typeof(global::EventTrigger).GetField("specificEvent", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(trigger, definition);
            int started = 0;
            Transform source = null;
            System.Action<GameEventDefinition, GameEventContext> listener = (d, context) => { started++; source = context.Source; };
            var events = TerminalGameSession.Events;
            events.EventStarted += listener;
            try
            {
                var playerView = Camera.main.transform;
                trigger.Fire(playerView);
                Assert.That(started, Is.EqualTo(1));
                Assert.That(source, Is.SameAs(playerView));
                var pause = Object.FindFirstObjectByType<TerminalPause>();
                pause.Pause();
                yield return null;
                trigger.Fire(playerView);
                Assert.That(started, Is.EqualTo(1));
                pause.Resume();
                yield return new WaitForSecondsRealtime(.5f);
                trigger.Fire(playerView);
                Assert.That(started, Is.EqualTo(2));
            }
            finally
            {
                events.EventStarted -= listener;
                Object.Destroy(owner);
                Object.Destroy(definition);
            }
        }

        [UnityTest, Category("Menu")]
        public IEnumerator NextSystemLoadsOnceAndKeepsRunResources()
        {
            yield return SceneManager.LoadSceneAsync("SampleScene");
            yield return null;
            var game = TerminalGameSession.Manager;
            var definition = ScriptableObject.CreateInstance<GameEventDefinition>();
            definition.eventId = "system-reset-check";
            definition.oncePerSystem = true;
            try
            {
                Assert.That(TerminalGameSession.Events.TryPlaySpecific(definition, "test"), Is.True);
                Assert.That(TerminalGameSession.Events.TryPlaySpecific(definition, "test"), Is.False);
                game.Resources.DrainOxygen(20);
                game.MarkCatalogReady();
                game.SubmitCatalog();
                Assert.That(game.EnterCryosleep(), Is.True);
                game.FinishCryosleepTransition();
                game.FinishCryosleepTransition();
                Assert.That(game.CurrentSystemNumber, Is.EqualTo(2));
                float deadline = Time.realtimeSinceStartup + 10;
                while (TerminalTransition.IsActive && Time.realtimeSinceStartup < deadline) yield return null;
                yield return null;
                Assert.That(game.Phase, Is.EqualTo(GameManager.GamePhase.SystemExploration));
                Assert.That(game.Resources.OxygenSeconds, Is.EqualTo(280));
                Assert.That(game.Resources.NitrogenCanisters, Is.EqualTo(2));
                Assert.That(TerminalGameSession.Events.TryPlaySpecific(definition, "test"), Is.True);
            }
            finally { Object.Destroy(definition); }
        }

        [UnityTest, Category("Menu")]
        public IEnumerator SurveyStartsOnlyWhenThePlayerBegins()
        {
            var game = TerminalGameSession.Manager;
            Assert.That(game.Phase, Is.EqualTo(GameManager.GamePhase.Boot));
            Assert.That(TerminalGameSession.Events.Suspended, Is.True);
            yield return ReadyMain();
            Click("Main Menu/Begin Survey");
            Assert.That(game.CurrentSystemNumber, Is.EqualTo(1));
            Assert.That(game.Phase, Is.EqualTo(GameManager.GamePhase.LoadingScreen));
            Assert.That(game.Resources.OxygenSeconds, Is.EqualTo(300));
            float deadline = Time.realtimeSinceStartup + 15;
            while (TerminalShipIntro.IsActive && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(game.Phase, Is.EqualTo(GameManager.GamePhase.SystemExploration));
            yield return null;
            yield return null;
            Assert.That(TerminalGameSession.Events.Suspended, Is.False);
            var pause = Object.FindFirstObjectByType<TerminalPause>();
            pause.Pause();
            yield return null;
            yield return null;
            Assert.That(TerminalGameSession.Events.Suspended, Is.True);
            pause.Resume();
            yield return new WaitForSecondsRealtime(.5f);
            Assert.That(game.Resources.OxygenSeconds, Is.EqualTo(300), "The menu must not add a second oxygen drain.");
            TerminalTransition.LoadScene("MainMenu");
            deadline = Time.realtimeSinceStartup + 15;
            while (TerminalTransition.IsActive && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(game.Phase, Is.EqualTo(GameManager.GamePhase.Boot));
            Assert.That(TerminalGameSession.Events.Suspended, Is.True);
        }

        [UnityTest, Category("Menu")]
        public IEnumerator ResourcesAndEventHistoryResetForANewRun()
        {
            yield return SceneManager.LoadSceneAsync("SampleScene");
            yield return null;
            var game = TerminalGameSession.Manager;
            var events = TerminalGameSession.Events;
            var definition = ScriptableObject.CreateInstance<GameEventDefinition>();
            definition.eventId = "run-reset-check";
            definition.oncePerRun = true;
            try
            {
                Assert.That(game.Phase, Is.EqualTo(GameManager.GamePhase.SystemExploration));
                Assert.That(events.TryPlaySpecific(definition, "test"), Is.True);
                Assert.That(events.TryPlaySpecific(definition, "test"), Is.False);
                game.Resources.DrainOxygen(50);
                game.Resources.TryConsumeShipParts(1);
                game.Resources.ApplyHullDamage(1);
                game.MarkCatalogReady();
                Assert.That(game.SubmitCatalog(), Is.True);
                Assert.That(game.SubmitCatalog(), Is.False);
                Assert.That(game.Weirdness, Is.EqualTo(5));
                Assert.That(game.EnterCryosleep(), Is.True);
                Assert.That(game.EnterCryosleep(), Is.False);
                Assert.That(game.Resources.NitrogenCanisters, Is.EqualTo(2));
                TerminalGameSession.StartNewRun();
                TerminalGameSession.ShipReady();
                yield return null;
                Assert.That(game.Resources.OxygenSeconds, Is.EqualTo(300));
                Assert.That(game.Resources.HullPips, Is.EqualTo(5));
                Assert.That(game.Resources.ShipParts, Is.EqualTo(3));
                Assert.That(game.Resources.NitrogenCanisters, Is.EqualTo(3));
                Assert.That(game.Weirdness, Is.Zero);
                Assert.That(events.TryPlaySpecific(definition, "test"), Is.True);
            }
            finally { Object.Destroy(definition); }
        }

        [UnityTest, Category("Menu"), Category("Failure")]
        public IEnumerator EmptyHullUsesFailureMenuAndRestartResetsResources()
        {
            yield return SceneManager.LoadSceneAsync("SampleScene");
            yield return null;
            var game = TerminalGameSession.Manager;
            game.Resources.ApplyHullDamage(game.Resources.MaxHullPips);
            float deadline = Time.realtimeSinceStartup + 10;
            while (SceneManager.GetActiveScene().name != "GameOverScene" && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("GameOverScene"));
            Assert.That(game.Phase, Is.EqualTo(GameManager.GamePhase.Ending));
            yield return WaitForFailureChoices();
            var restart = Object.FindObjectsByType<TerminalButton>(FindObjectsSortMode.None).First(b => b.name.Contains("RESTART SURVEY"));
            restart.onClick.Invoke();
            deadline = Time.realtimeSinceStartup + 10;
            while (TerminalTransition.IsActive && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("SampleScene"));
            Assert.That(game.Phase, Is.EqualTo(GameManager.GamePhase.SystemExploration));
            Assert.That(game.Resources.HullPips, Is.EqualTo(5));
            Assert.That(TerminalShipIntro.IsActive, Is.False);
        }

        [UnityTest, Category("Menu"), Category("Failure")]
        public IEnumerator OxygenFailureWaitsForPauseToClose()
        {
            yield return SceneManager.LoadSceneAsync("SampleScene");
            yield return null;
            var game = TerminalGameSession.Manager;
            var pause = Object.FindFirstObjectByType<TerminalPause>();
            pause.Pause();
            game.Resources.DrainOxygen(300);
            yield return new WaitForSecondsRealtime(.4f);
            Assert.That(TerminalFailure.Active, Is.False);
            pause.Resume();
            float deadline = Time.realtimeSinceStartup + 10;
            while (SceneManager.GetActiveScene().name != "GameOverScene" && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("GameOverScene"));
            Assert.That(game.Phase, Is.EqualTo(GameManager.GamePhase.Ending));
        }
    }
}
