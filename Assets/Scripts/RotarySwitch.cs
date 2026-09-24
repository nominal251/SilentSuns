using UnityEngine;

public class RotarySwitch : MonoBehaviour, Interactable
{
    [Header("Rotary Dial")]
    public Transform dial;

    [Tooltip("Rotation of the dial when the switch is at state 0.")]
    public Vector3 state0Rotation;

    [Tooltip("Rotation of the dial when the switch is at state 1.")]
    public Vector3 state1Rotation;

    [Tooltip("Rotation of the dial when the switch is at state 2.")]
    public Vector3 state2Rotation;

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

    private void Start()
    {
        UpdateSwitch();
    }

    public void Interact()
    {
        currentState++;

        if (currentState > 2)
        {
            currentState = 0;
        }

        UpdateSwitch();

        if (audioSource != null && switchSound != null)
        {
            audioSource.PlayOneShot(switchSound);
        }
    }

    public void StopInteract()
    {

    }

    private void UpdateSwitch()
    {

        if (dial != null)
        {
            switch (currentState)
            {
                case 0:
                    dial.localEulerAngles = state0Rotation;
                    break;

                case 1:
                    dial.localEulerAngles = state1Rotation;
                    break;

                case 2:
                    dial.localEulerAngles = state2Rotation;
                    break;
            }
        }

        if (state0Object != null)
        {
            state0Object.SetActive(false);
        }

        if (state1Object != null)
        {
            state1Object.SetActive(false);
        }

        if (state2Object != null)
        {
            state2Object.SetActive(false);
        }

        switch (currentState)
        {
            case 0:
                if (state0Object != null)
                    state0Object.SetActive(true);
                break;

            case 1:
                if (state1Object != null)
                    state1Object.SetActive(true);
                break;

            case 2:
                if (state2Object != null)
                    state2Object.SetActive(true);
                break;
        }
    }
}