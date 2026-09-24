using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOver : MonoBehaviour
{
    public GameObject enemy;
    public GameObject lights;

    public AudioSource audioSource;
    public AudioClip gameOverAudio;

    public string sceneToLoad;

    public float positionTolerance = 0.5f;

    private bool gameOverStarted = false;

    void Update()
    {
        if (!gameOverStarted && Vector3.Distance(transform.position, enemy.transform.position) <= positionTolerance)
        {
            gameOverStarted = true;
            StartCoroutine(GameOverSequence());
        }
    }

    IEnumerator GameOverSequence()
    {
        lights.SetActive(false);

        audioSource.PlayOneShot(gameOverAudio);

        yield return new WaitForSeconds(4f);

        SceneManager.LoadScene(sceneToLoad);
    }
}