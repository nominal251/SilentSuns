
using UnityEngine;
using System.Collections;

public class RotarySwitch : MonoBehaviour, Interactable
{
    [Header("Rotary Dial")]
    public Transform dial;

    [Header("Notch Settings")]
    public Vector3[] stateRotations;

    [Header("Animation")]
    [Tooltip("Time in seconds to move between states.")]
    public float animationDuration = 0.25f;

    [Header("Switch State")]
    public int currentState = 0;

    [Header("State GameObjects")]
    public GameObject[] stateObjects;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip switchSound;

    private bool isAnimating = false;

    private void Start()
    {
        currentState = Mathf.Clamp(currentState, 0, stateRotations.Length - 1);
        UpdateSwitchInstant();
    }

    public void Interact(GameObject interactionSource)
    {
        if (isAnimating || stateRotations.Length == 0)
        {
            return;
        }

        currentState++;

        if (currentState >= stateRotations.Length)
        {
            currentState = 0;
        }

        StartCoroutine(AnimateSwitch());
    }

    public void StopInteract()
    {

    }

    private IEnumerator AnimateSwitch()
    {
        isAnimating = true;

        Quaternion startRotation = dial.localRotation;
        Quaternion targetRotation = GetRotationForState(currentState);

        float elapsedTime = 0f;

        while (elapsedTime < animationDuration)
        {
            elapsedTime += Time.deltaTime;

            float progress = Mathf.Clamp01(
                elapsedTime / animationDuration
            );

            progress = Mathf.SmoothStep(0f, 1f, progress);

            dial.localRotation = Quaternion.Slerp(
                startRotation,
                targetRotation,
                progress
            );

            yield return null;
        }

        dial.localRotation = targetRotation;

        UpdateStateObjects();
        PlaySwitchSound();

        isAnimating = false;
    }

    private Quaternion GetRotationForState(int state)
    {
        return Quaternion.Euler(stateRotations[state]);
    }

    private void UpdateSwitchInstant()
    {
        if (dial != null && stateRotations.Length > 0)
        {
            dial.localRotation = GetRotationForState(currentState);
        }

        UpdateStateObjects();
    }

    private void UpdateStateObjects()
    {
        for (int i = 0; i < stateObjects.Length; i++)
        {
            if (stateObjects[i] != null)
            {
                stateObjects[i].SetActive(currentState == i);
            }
        }
    }

    private void PlaySwitchSound()
    {
        if (audioSource != null && switchSound != null)
        {
            audioSource.PlayOneShot(switchSound);
        }
    }
}
