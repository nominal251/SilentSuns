using UnityEngine;

public class FlipSwitch : MonoBehaviour, Interactable
{
    [Header("Switch Components")]
    public Transform lever;

    [Header("Rotation")]
    public Vector3 offRotation;
    public Vector3 onRotation;

    [Header("Object to Toggle")]
    public GameObject objectToToggle;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip switchSound;

    [Header("Starting State")]
    public bool isOn = false;

    private void Start()
    {
        UpdateSwitch();
    }

    public void Interact()
    {
        isOn = !isOn;
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
        if (lever != null)
        {
            if (isOn)
            {
                lever.localEulerAngles = onRotation;
            }
            else
            {
                lever.localEulerAngles = offRotation;
            }
        }

        if (objectToToggle != null)
        {
            objectToToggle.SetActive(isOn);
        }
    }
}