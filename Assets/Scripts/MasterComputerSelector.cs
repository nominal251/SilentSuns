using UnityEngine;
using UnityEngine.Audio;

// The screen chooses the page; Danny's rotary switch moves the physical dial.
public class MasterComputerSelector : MonoBehaviour, Interactable
{
    [SerializeField] private MasterComputerStation station;
    [SerializeField] private RotarySwitch rotarySwitch;

    public void Configure(MasterComputerStation computer, RotarySwitch knob)
    {
        station = computer;
        rotarySwitch = knob;
    }

    private void Awake()
    {
        if (rotarySwitch == null) return;
        var audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.clip = rotarySwitch.switchSound;
        audioSource.ignoreListenerPause = true;
        var mixer = Resources.Load<AudioMixer>("SilentSunsAudio");
        var groups = mixer == null ? null : mixer.FindMatchingGroups("FX");
        if (groups != null && groups.Length > 0) audioSource.outputAudioMixerGroup = groups[0];
        rotarySwitch.audioSource = audioSource;
    }

    private void LateUpdate()
    {
        if (station == null || station.UI == null || rotarySwitch == null) return;
        // Exit and pause both reset the page, so the dial follows and clicks once.
        rotarySwitch.SetState((int)station.UI.Page);
    }

    public void Interact(GameObject source)
    {
        if (station == null || station.UI == null || rotarySwitch == null) return;
        if (!station.UI.InUse) { station.Interact(source); return; }
        if (!station.UI.Ready) return;
        // An unread warning must be checked before returning to the other pages.
        station.SetScreen(station.UI.AlertBlocking ? 3 : ((int)station.UI.Page + 1) % 4);
        rotarySwitch.SetState((int)station.UI.Page);
    }

    public void StopInteract() { }
}
