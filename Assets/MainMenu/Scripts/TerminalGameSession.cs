using UnityEngine;
using UnityEngine.SceneManagement;

namespace SilentSuns.Menu
{
    // Connects the terminal menus to Danny's run, resource, and event managers.
    public class TerminalGameSession : MonoBehaviour
    {
        private static TerminalGameSession instance;
        private GameManager game;
        private EventManager events;
        private bool waitingForIntro;
        private bool introShipLoaded;
        private bool sceneAlreadyLoading;
        private bool failurePending;

        public static GameManager Manager => instance != null ? instance.game : null;
        public static EventManager Events => instance != null ? instance.events : null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            instance = null;
            SceneManager.sceneLoaded -= SceneLoaded;
            SceneManager.sceneLoaded += SceneLoaded;
        }

        private static TerminalGameSession EnsureSession()
        {
            if (instance != null) return instance;
            instance = Instantiate(Resources.Load<TerminalGameSession>("SurveySystems"));
            DontDestroyOnLoad(instance.gameObject);
            return instance;
        }

        private void Awake()
        {
            game = GetComponent<GameManager>();
            events = GetComponent<EventManager>();
            events.Suspended = true;
            game.SystemLoadRequested += LoadSystem;
            game.Resources.OxygenDepleted += QueueFailure;
            game.Resources.HullChanged += HullChanged;
        }

        private static void SceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == "MainMenu")
            {
                var session = EnsureSession();
                session.waitingForIntro = true;
                session.introShipLoaded = false;
                session.failurePending = false;
                session.game.ReturnToBoot();
                session.events.Suspended = true;
            }
            else if (scene.name == "SampleScene")
            {
                var session = EnsureSession();
                // MainMenu loads this scene early so it can use the telescope screen.
                // That does not start the run. Begin Survey and the handoff do that.
                if (session.waitingForIntro && !session.introShipLoaded)
                {
                    session.introShipLoaded = true;
                    return;
                }
                session.waitingForIntro = false;
                if (session.game.Phase == GameManager.GamePhase.Boot ||
                    session.game.Phase == GameManager.GamePhase.Ending)
                    StartNewRun();
                ShipReady();
            }
            else if (scene.name == "GameOverScene" || scene.name == "VictoryScene")
                EndRun();
        }

        public static void StartNewRun()
        {
            var session = EnsureSession();
            session.failurePending = false;
            session.events.Suspended = true;
            session.game.ReturnToBoot();
            // StartNewRun resets resources. Its RunStarted event clears event history.
            session.sceneAlreadyLoading = true;
            session.game.StartNewRun();
            session.sceneAlreadyLoading = false;
        }

        private void LoadSystem(int systemNumber)
        {
            // The intro already loaded the ship; restart already has a scene transition.
            if (waitingForIntro || sceneAlreadyLoading || TerminalTransition.IsActive) return;
            // SampleScene is the current prototype system. A generator can replace this load later.
            TerminalTransition.LoadScene("SampleScene");
        }

        public static void ShipReady()
        {
            if (instance == null) return;
            instance.waitingForIntro = false;
            instance.game.NotifySystemLoaded();
            instance.RefreshEventGate();
        }

        public static void EndRun()
        {
            if (instance == null) return;
            instance.game.SetEnding();
            instance.events.Suspended = true;
        }

        private bool IsPlaying => game.Phase == GameManager.GamePhase.SystemExploration ||
            game.Phase == GameManager.GamePhase.SystemComplete ||
            game.Phase == GameManager.GamePhase.PlanetaryExploration;

        private void RefreshEventGate()
        {
            events.Suspended = !IsPlaying || TerminalPause.BlocksGameplayInput;
        }

        private void QueueFailure() { failurePending = true; }
        private void HullChanged(int remaining, int maximum)
        {
            if (remaining == 0) QueueFailure();
        }

        private void Update()
        {
            RefreshEventGate();
            if (!failurePending || events.Suspended) return;
            failurePending = false;
            if (game.Resources.IsOxygenDepleted || game.Resources.HullPips == 0)
                TerminalFailure.Begin("GameOverScene");
        }

        private void OnDestroy()
        {
            if (game != null && game.Resources != null)
            {
                game.SystemLoadRequested -= LoadSystem;
                game.Resources.OxygenDepleted -= QueueFailure;
                game.Resources.HullChanged -= HullChanged;
            }
            if (instance == this) instance = null;
        }
    }
}
