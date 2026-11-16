using UnityEngine;
using System.Collections;

public class RandomShipAudioEvent : MonoBehaviour
{
    [Header("Audio Location")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip[] audioClips;

    [Header("Random Event Settings")]
    [SerializeField] private float minDelay = 10f;
    [SerializeField] private float maxDelay = 30f;

    [Range(0f, 100f)]
    [SerializeField] private float eventChance = 100f;

    [Header("Cooldown")]
    [SerializeField] private float cooldown = 20f;

    private bool playerInside;
    private bool onCooldown;
    private Coroutine eventRoutine;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerInside = true;

        if (eventRoutine == null)
        {
            eventRoutine = StartCoroutine(RandomEventLoop());
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerInside = false;

        if (eventRoutine != null)
        {
            StopCoroutine(eventRoutine);
            eventRoutine = null;
        }
    }

    private IEnumerator RandomEventLoop()
    {
        while (playerInside)
        {
            float delay = Random.Range(minDelay, maxDelay);

            yield return new WaitForSeconds(delay);

            if (!playerInside)
                break;

            if (!onCooldown &&
                Random.Range(0f, 100f) <= eventChance)
            {
                PlayRandomSound();

                onCooldown = true;

                yield return new WaitForSeconds(cooldown);

                onCooldown = false;
            }
        }

        eventRoutine = null;
    }

    private void PlayRandomSound()
    {
        if (audioSource == null || audioClips.Length == 0)
            return;

        AudioClip clip = audioClips[
            Random.Range(0, audioClips.Length)
        ];

        audioSource.PlayOneShot(clip);
    }
}