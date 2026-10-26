using UnityEngine;
using System.Collections;

public class RotarySwitch2 : MonoBehaviour, Interactable
{
    [Header("Rotary Dial")]
    public Transform dial;

    [Header("Rotation")]
    public Vector3 state0Rotation;
    public Vector3 state1Rotation;
    public Vector3 state2Rotation;

    [Header("Animation")]
    [Tooltip("Time in seconds to move between states.")]
    public float animationDuration = 0.25f;

    [Header("Switch State")]
    [Range(0, 2)]
    public int currentState = 0;

    [Header("State GameObjects")]
    public GameObject state0Object;
    public GameObject state1Object;
    public GameObject state2Object;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip switchSound;

    private bool isAnimating = false;

    private void Start()
    {
        UpdateSwitchInstant();
    }

    public void Interact()
    {

        if (isAnimating)
        {
            return;
        }

        currentState++;

        if (currentState > 2)
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
        switch (state)
        {
            case 0:
                return Quaternion.Euler(state0Rotation);

            case 1:
                return Quaternion.Euler(state1Rotation);

            case 2:
                return Quaternion.Euler(state2Rotation);

            default:
                return Quaternion.Euler(state0Rotation);
        }
    }

    private void UpdateSwitchInstant()
    {
        if (dial != null)
        {
            dial.localRotation = GetRotationForState(currentState);
        }

        UpdateStateObjects();
    }

    private void UpdateStateObjects()
    {
        if (state0Object != null)
        {
            state0Object.SetActive(currentState == 0);
        }

        if (state1Object != null)
        {
            state1Object.SetActive(currentState == 1);
        }

        if (state2Object != null)
        {
            state2Object.SetActive(currentState == 2);
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