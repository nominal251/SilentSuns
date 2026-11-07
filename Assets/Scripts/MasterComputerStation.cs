using System.Linq;
using SilentSuns.Menu;
using UnityEngine;
using UnityEngine.InputSystem;

// The prefab uses Danny's monitor/console models. This connects their UI to the running ship.
public class MasterComputerStation : MonoBehaviour, Interactable
{
    [SerializeField] private MasterComputerDisplay display;
    [SerializeField] private Light alertLight;
    private MasterComputerData data;
    private PlanetManager planets;
    private GameManager manager;
    private bool connected;
    private int openedFrame;
    private MasterComputerView view;
    private bool closing;
    public MasterComputerUI UI => display == null ? null : display.UI;

    public void Configure(MasterComputerDisplay screen, Light warningLight)
    {
        display = screen;
        alertLight = warningLight;
    }

    private void Update()
    {
        if (!connected && UI != null)
        {
            manager = TerminalGameSession.Manager;
            if (manager == null) manager = FindFirstObjectByType<GameManager>();
            if (manager != null)
            {
                data = MasterComputerData.For(manager);
                UI.Bind(data);
                UI.ExitRequested += RequestExit;
                connected = true;
            }
        }
        if (!connected) return;
        if (planets == null) planets = FindFirstObjectByType<PlanetManager>();
        if (planets != null)
        {
            var list = planets.planets.Where(p => p != null && !p.isAnomaly).ToArray();
            UI.SetSurvey(list.Count(p => p.gameObject.activeInHierarchy), list.Count(p => p.waveformComplete && p.compComplete), list.Length);
        }
        if (alertLight != null)
        {
            alertLight.enabled = data.UnreadCount > 0;
            alertLight.intensity = GamePreferences.ReducedMotion ? .8f : .65f + .15f * Mathf.Sin(Time.time * 2);
        }
        if (UI.InUse && (TerminalPause.BlocksGameplayInput || Camera.main == null ||
            Vector3.Distance(Camera.main.transform.position, display.Screen.bounds.center) > 4.5f)) EndUse();
        // Pause sends Escape to RequestExit. B/right click uses that same exit.
        if (UI.InUse && Time.frameCount > openedFrame &&
            ((Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame) ||
             (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame))) RequestExit();
        if (UI.InUse && !closing && view != null && view.Settled && Time.frameCount > openedFrame)
        {
            // The screen uses its raycaster; only the physical console dial uses this world ray.
            bool click = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
            bool confirm = Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame;
            if (click || confirm)
            {
                Vector2 pointer = click ? Mouse.current.position.ReadValue() : new Vector2(Screen.width * .5f, Screen.height * .5f);
                if (Physics.Raycast(Camera.main.ScreenPointToRay(pointer), out var hit, 5))
                {
                    var selector = hit.collider.GetComponent<MasterComputerSelector>();
                    if (selector != null && selector.transform.IsChildOf(display.transform)) selector.Interact(Camera.main.gameObject);
                }
            }
        }
    }

    public void Interact(GameObject interactionSource)
    {
        if (!connected || !UI.Initialized || UI.InUse || closing || TerminalPause.BlocksGameplayInput) return;
        TerminalToolInput.Begin(this, EndUse, RequestExit);
        if (view == null) view = gameObject.AddComponent<MasterComputerView>();
        view.Begin(display.Screen);
        openedFrame = Time.frameCount;
        UI.SetInUse(true);
    }

    // Releasing the interact key leaves the computer open; the player uses its EXIT button.
    public void StopInteract() { }
    private void RequestExit()
    {
        if (closing || !UI.InUse) return;
        closing = true;
        UI.SetInUse(false);
        view.Return(EndUse);
    }
    public void EndUse()
    {
        if (view != null && view.Active) view.Restore();
        closing = false;
        if (UI != null) UI.SetInUse(false);
        TerminalToolInput.End(this);
    }

    public void SetScreen(int position) => UI?.SelectPage(position);
    public bool InsertDisk(MasterComputerRecord record) => data != null && data.InsertDisk(record);
    public bool RegisterTool(MasterComputerRecord record) => data != null && data.RegisterTool(record);

    private void OnDisable() => EndUse();
    private void OnDestroy()
    {
        if (UI != null) UI.ExitRequested -= RequestExit;
    }
}
