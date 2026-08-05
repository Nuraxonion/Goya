using System.Collections;
using UnityEngine;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance;

    private AudioSource audioSource;
    public float fadeDuration = 3f;
    
    void Awake()
    {
        // Deletes music object duplicate
        if (FindObjectsByType<MusicManager>(FindObjectsSortMode.None).Length > 1)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Do not destroy on new scene
        DontDestroyOnLoad(gameObject);
        audioSource = GetComponent<AudioSource>();
    }
    public void PlayMusic(AudioClip clip)
    {
        if (audioSource.clip == clip) return;
        StartCoroutine(FadeToNewTrack(clip));
    }

    private IEnumerator FadeToNewTrack(AudioClip newClip)
    {
        float startVolume = audioSource.volume;
        float elapsed = 0f;

        // Fading
        while (elapsed < fadeDuration)
        {
            audioSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / fadeDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        audioSource.Stop();
        audioSource.clip = newClip;
        audioSource.Play();

        // Increase
        elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            audioSource.volume = Mathf.Lerp(0f, startVolume, elapsed / fadeDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        audioSource.volume = startVolume;
    }
}