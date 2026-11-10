using UnityEngine;
using System.Collections;

public class RotarySwitch : MonoBehaviour, Interactable
{
    [Header("Rotary Dial")]
    public Transform dial;

    [Header("Notch Settings")]
    public Vector3[] stateRotations = { new Vector3(0, -45, 0), Vector3.zero, new Vector3(0, 45, 0) };

    [Header("Animation")]
    [Tooltip("Time in seconds to move between states.")]
    public float animationDuration = 0.25f;
    [Tooltip("Lets the computer return its dial while the game is paused.")]
    public bool useUnscaledTime;

    [Header("Switch State")]
    public int currentState;

    [Header("State GameObjects")]
    public GameObject[] stateObjects = new GameObject[0];

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip switchSound;

    private Coroutine turn;

    private void Start()
    {
        if (stateRotations == null || stateRotations.Length == 0) return;
        currentState = Mathf.Clamp(currentState, 0, stateRotations.Length - 1);
        if (dial != null) dial.localRotation = Quaternion.Euler(stateRotations[currentState]);
        UpdateStateObjects();
    }

    public void Interact(GameObject interactionSource)
    {
        if (turn != null || stateRotations == null || stateRotations.Length == 0) return;
        SetState((currentState + 1) % stateRotations.Length);
    }

    // The master computer calls this when its page changes or it returns to Status.
    public void SetState(int state)
    {
        if (stateRotations == null || stateRotations.Length == 0) return;
        state = Mathf.Clamp(state, 0, stateRotations.Length - 1);
        if (currentState == state) return;
        currentState = state;
        if (turn != null) { StopCoroutine(turn); turn = null; }
        UpdateStateObjects();
        // Click once at the start of the turn, including an automatic return during pause.
        if (audioSource != null && switchSound != null) audioSource.PlayOneShot(switchSound);
        if (dial == null) return;
        if (animationDuration <= 0) dial.localRotation = Quaternion.Euler(stateRotations[currentState]);
        else turn = StartCoroutine(AnimateSwitch());
    }

    public void StopInteract() { }

    private IEnumerator AnimateSwitch()
    {
        Quaternion startRotation = dial.localRotation;
        Quaternion targetRotation = Quaternion.Euler(stateRotations[currentState]);
        float elapsedTime = 0;
        while (elapsedTime < animationDuration)
        {
            elapsedTime += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            float progress = Mathf.SmoothStep(0, 1, Mathf.Clamp01(elapsedTime / animationDuration));
            dial.localRotation = Quaternion.Slerp(startRotation, targetRotation, progress);
            yield return null;
        }
        dial.localRotation = targetRotation;
        turn = null;
    }

    private void UpdateStateObjects()
    {
        if (stateObjects == null) return;
        for (int i = 0; i < stateObjects.Length; i++)
            if (stateObjects[i] != null) stateObjects[i].SetActive(currentState == i);
    }

    private void OnDisable()
    {
        if (turn != null) StopCoroutine(turn);
        turn = null;
    }
}
