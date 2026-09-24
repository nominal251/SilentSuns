using UnityEngine;
using System.Collections;

public class FlipSwitch2 : MonoBehaviour, Interactable
{
    [Header("Switch Components")]
    public Transform lever;

    [Header("Rotation")]
    public Vector3 offRotation;
    public Vector3 onRotation;

    [Header("Animation")]
    [Tooltip("Time in seconds to move from one state to the other.")]
    public float animationDuration = 0.25f;

    [Header("Object to Toggle")]
    public GameObject objectToToggle;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip switchSound;

    [Header("Starting State")]
    public bool isOn = false;

    private Coroutine rotationCoroutine;

    private void Start()
    {
        if (lever != null)
        {
            lever.localRotation = Quaternion.Euler(
                isOn ? onRotation : offRotation
            );
        }

        UpdateObject();
    }

    public void Interact()
    {
        isOn = !isOn;

        if (rotationCoroutine != null)
        {
            StopCoroutine(rotationCoroutine);
        }

        rotationCoroutine = StartCoroutine(AnimateLever());
    }

    public void StopInteract()
    {
        
    }

    private IEnumerator AnimateLever()
    {
        if (lever == null)
        {
            yield break;
        }

        Quaternion startRotation = lever.localRotation;

        Quaternion targetRotation = Quaternion.Euler(
            isOn ? onRotation : offRotation
        );

        float elapsedTime = 0f;

        while (elapsedTime < animationDuration)
        {
            elapsedTime += Time.deltaTime;

            float progress = Mathf.Clamp01(
                elapsedTime / animationDuration
            );

            progress = Mathf.SmoothStep(0f, 1f, progress);

            lever.localRotation = Quaternion.Slerp(
                startRotation,
                targetRotation,
                progress
            );

            yield return null;
        }

        lever.localRotation = targetRotation;

        UpdateObject();

        PlaySwitchSound();

        rotationCoroutine = null;
    }

    private void UpdateObject()
    {
        if (objectToToggle != null)
        {
            objectToToggle.SetActive(isOn);
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