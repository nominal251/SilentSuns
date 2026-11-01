using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace SilentSuns.Menu.Tests
{
    public partial class TerminalMenuTests
    {
        // These gameplay scripts live in Assembly-CSharp, outside the menu test assembly.
        private static Type GameplayType(string name) => Type.GetType(name + ", Assembly-CSharp", true);
        private static object ReadMember(object target, string name)
        {
            var type = target.GetType();
            var field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            return field != null ? field.GetValue(target) : type.GetProperty(name).GetValue(target);
        }
        private static object Call(object target, string name, params object[] args) => target.GetType().GetMethod(name).Invoke(target, args);
        private static Component SurveyTracker() => Object.FindFirstObjectByType(GameplayType("ObjectiveTracker")) as Component;
        private static Component SurveySystem(Component tracker) => (Component)ReadMember(tracker, "objectiveSystem");
        private static Component[] SurveyObjectives(Component system) => ((IEnumerable)ReadMember(system, "Objectives")).Cast<Component>().ToArray();
        private static void SetScans(Component tracker, int number, bool signal, bool composition)
        {
            object planet = ReadMember(tracker, "planet" + number);
            planet.GetType().GetField("waveformComplete").SetValue(planet, signal);
            planet.GetType().GetField("compComplete").SetValue(planet, composition);
        }

        [UnityTest, Category("Menu"), Category("Objectives")]
        public IEnumerator ObjectivesWaitForTheTelescopeHandoff()
        {
            var tracker = SurveyTracker();
            var system = SurveySystem(tracker);
            var objectives = SurveyObjectives(system);
            Assert.That(objectives.Length, Is.EqualTo(4));
            Assert.That(ReadMember(system, "Suspended"), Is.True);
            Assert.That(objectives.All(o => !(bool)ReadMember(o, "IsActive")), Is.True);
            SetScans(tracker, 1, true, true);
            yield return null;
            Assert.That(ReadMember(objectives[0], "IsComplete"), Is.False);
            yield return ReadyMain();
            Click("Main Menu/Begin Survey");
            yield return null;
            Assert.That(ReadMember(system, "Suspended"), Is.True);
            Assert.That(ReadMember(ReadMember(tracker, "planet1"), "waveformComplete"), Is.False);
            yield return WaitForTransition();
            Assert.That(ReadMember(system, "Suspended"), Is.False);
            Assert.That(objectives.All(o => (bool)ReadMember(o, "IsRunning")), Is.True);
            Assert.That(TerminalGameSession.Manager.Phase, Is.EqualTo(GameManager.GamePhase.SystemExploration));
        }

        [UnityTest, Category("Menu"), Category("Objectives")]
        public IEnumerator RealSurveysWaitForPauseAndKeepTheShipScreen()
        {
            yield return SceneManager.LoadSceneAsync("SampleScene");
            yield return null;
            var tracker = SurveyTracker();
            var system = SurveySystem(tracker);
            var objectives = SurveyObjectives(system);
            int completed = 0;
            Action listener = () => completed++;
            system.GetType().GetEvent("AllObjectivesCompleted").AddEventHandler(system, listener);
            var optional = new GameObject("Unrelated collection objective");
            optional.AddComponent(GameplayType("CollectItemsObjective"));
            try
            {
                Call(system, "Register", objectives[0]);
                Assert.That(SurveyObjectives(system).Length, Is.EqualTo(4), "Registering twice must not duplicate a survey.");
                SetScans(tracker, 1, true, false);
                yield return null;
                Assert.That(ReadMember(objectives[0], "IsComplete"), Is.False, "Both scans are required.");
                for (int i = 1; i <= 3; i++) SetScans(tracker, i, true, true);
                yield return null;
                Assert.That(ReadMember(system, "AllComplete"), Is.False);
                var pause = Object.FindFirstObjectByType<TerminalPause>();
                pause.Pause();
                SetScans(tracker, 4, true, true);
                yield return new WaitForSecondsRealtime(.2f);
                Assert.That(ReadMember(system, "Suspended"), Is.True);
                Assert.That(ReadMember(objectives[3], "IsComplete"), Is.False);
                Assert.That(completed, Is.Zero);
                pause.Resume();
                yield return new WaitForSecondsRealtime(.5f);
                Assert.That(ReadMember(system, "AllComplete"), Is.True);
                Assert.That(completed, Is.EqualTo(1));
                Assert.That(TerminalGameSession.Manager.Phase, Is.EqualTo(GameManager.GamePhase.SystemComplete));
                string[] expected = new string[4];
                for (int i = 1; i <= 4; i++)
                {
                    var planet = (Component)ReadMember(tracker, "planet" + i);
                    expected[i - 1] = planet.gameObject.activeInHierarchy
                        ? ReadMember(planet, "name") + "- Signal Scan: [X] - Comp Scan: [X]"
                        : "PLANET " + i + " NOT LOCATED";
                }
                Assert.That(((TMP_Text)ReadMember(tracker, "objText")).text, Is.EqualTo("Bodies Surveyed:\n" + string.Join("\n", expected)));
                Assert.That(GameProgress.HasCompletedGame, Is.False, "The player still needs to use cryosleep.");
                yield return null;
                Assert.That(completed, Is.EqualTo(1));
                Object.Destroy(CaptureGameplay("objectives-survey-screen"));
            }
            finally
            {
                system.GetType().GetEvent("AllObjectivesCompleted").RemoveEventHandler(system, listener);
                Object.Destroy(optional);
            }
        }

        [UnityTest, Category("Menu"), Category("Objectives")]
        public IEnumerator NewRunClearsSurveyProgressAndOldSubscriptions()
        {
            yield return SceneManager.LoadSceneAsync("SampleScene");
            yield return null;
            var tracker = SurveyTracker();
            var system = SurveySystem(tracker);
            for (int i = 1; i <= 4; i++) SetScans(tracker, i, true, true);
            yield return null;
            Assert.That(ReadMember(system, "AllComplete"), Is.True);
            TerminalGameSession.StartNewRun();
            Assert.That(ReadMember(system, "Suspended"), Is.True);
            Assert.That(ReadMember(system, "AllComplete"), Is.False);
            Assert.That(ReadMember(tracker, "allObjectivesComplete"), Is.False);
            for (int i = 1; i <= 4; i++)
                Assert.That(ReadMember(ReadMember(tracker, "planet" + i), "compComplete"), Is.False);
            TerminalGameSession.ShipReady();
            yield return null;
            Assert.That(SurveyObjectives(system).Length, Is.EqualTo(4));
            Assert.That(SurveyObjectives(system).All(o => (bool)ReadMember(o, "IsRunning")), Is.True);
            yield return SceneManager.LoadSceneAsync("SampleScene");
            yield return null;
            Assert.That(tracker == null, Is.True);
            Assert.That(Object.FindObjectsByType(GameplayType("ObjectiveSystem"), FindObjectsSortMode.None).Length, Is.EqualTo(1));
            var replacement = SurveySystem(SurveyTracker());
            Assert.That(SurveyObjectives(replacement).Length, Is.EqualTo(4));
            TerminalGameSession.StartNewRun();
            TerminalGameSession.ShipReady();
            yield return null;
            Assert.That(ReadMember(replacement, "AllComplete"), Is.False);
        }

        [UnityTest, Category("Menu"), Category("Objectives")]
        public IEnumerator MissingSurveyDoesNotUnlockCryosleep()
        {
            yield return SceneManager.LoadSceneAsync("SampleScene");
            yield return null;
            var tracker = SurveyTracker();
            var system = SurveySystem(tracker);
            Object.Destroy(SurveyObjectives(system)[3]);
            yield return null;
            for (int i = 1; i <= 4; i++) SetScans(tracker, i, true, true);
            yield return null;
            Assert.That(ReadMember(system, "AllComplete"), Is.False);
            Assert.That(ReadMember(tracker, "allObjectivesComplete"), Is.False);
            Assert.That(TerminalGameSession.Manager.Phase, Is.EqualTo(GameManager.GamePhase.SystemExploration));
        }

        [UnityTest, Category("Menu"), Category("Objectives")]
        public IEnumerator CollectionObjectivesStillPauseResetAndCompleteOnce()
        {
            var owner = new GameObject("Collection test");
            owner.SetActive(false);
            var system = owner.AddComponent(GameplayType("ObjectiveSystem"));
            system.GetType().GetField("collectOnAwake", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(system, false);
            owner.SetActive(true);
            var objective = owner.AddComponent(GameplayType("CollectItemsObjective"));
            int completed = 0;
            Action listener = () => completed++;
            system.GetType().GetEvent("AllObjectivesCompleted").AddEventHandler(system, listener);
            try
            {
                Assert.That(ReadMember(system, "AllComplete"), Is.False);
                Call(system, "Register", objective);
                Call(system, "Register", objective);
                Assert.That(SurveyObjectives(system).Length, Is.EqualTo(1));
                Call(system, "SetSuspended", true);
                Call(objective, "AddProgress", 5);
                Assert.That(ReadMember(objective, "IsComplete"), Is.False);
                Call(system, "SetSuspended", false);
                Call(objective, "AddProgress", 5);
                Call(objective, "AddProgress", 5);
                Assert.That(completed, Is.EqualTo(1));
                Call(system, "ResetObjectives");
                Assert.That(ReadMember(objective, "IsComplete"), Is.False);
                Assert.That(Call(objective, "GetProgressText"), Does.Contain("0/5"));
                Call(objective, "AddProgress", 5);
                Assert.That(completed, Is.EqualTo(2));
            }
            finally { Object.Destroy(owner); }
            yield return null;
        }

        [UnityTest, Category("Menu"), Category("Objectives")]
        public IEnumerator DebugCompletionDoesNotEarnARealCompletionFlag()
        {
            yield return SceneManager.LoadSceneAsync("SampleScene");
            yield return null;
            var tracker = SurveyTracker();
            object debug = ReadMember(tracker, "debugButton");
            debug.GetType().GetField("activated").SetValue(debug, true);
            object cryo = ReadMember(tracker, "cryoButton");
            cryo.GetType().GetField("activated").SetValue(cryo, true);
            float deadline = Time.realtimeSinceStartup + 5;
            while (SceneManager.GetActiveScene().name != "VictoryScene" && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("VictoryScene"));
            Assert.That(GameProgress.HasCompletedGame, Is.False);
            Assert.That(TerminalGameSession.Manager.Resources.NitrogenCanisters, Is.EqualTo(2));
        }
    }
}
