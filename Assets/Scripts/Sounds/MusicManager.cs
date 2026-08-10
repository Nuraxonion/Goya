using System.Collections;
using UnityEngine;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance;

    private AudioSource audioSource;
    public float fadeDuration = 3f;

    // Volume the music should sit at once a fade finishes. Cached at startup so an
    // interrupted fade can never latch a near-zero volume as the new "full" volume.
    private float defaultVolume = 1f;
    private Coroutine fadeRoutine;

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

        if (audioSource != null)
        {
            defaultVolume = audioSource.volume;
            audioSource.loop = true;
        }
    }
    public void PlayMusic(AudioClip clip)
    {
        if (audioSource == null || clip == null) return;
        if (audioSource.clip == clip) return;

        // A band can change again mid-fade; only one fade may own the volume at a time.
        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        fadeRoutine = StartCoroutine(FadeToNewTrack(clip));
    }

    private IEnumerator FadeToNewTrack(AudioClip newClip)
    {
        // Fade down from wherever the volume currently is, so an interrupted fade
        // continues smoothly instead of jumping back to full.
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
        audioSource.loop = true;
        audioSource.Play();

        // Increase
        elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            audioSource.volume = Mathf.Lerp(0f, defaultVolume, elapsed / fadeDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        audioSource.volume = defaultVolume;
        fadeRoutine = null;
    }
}
