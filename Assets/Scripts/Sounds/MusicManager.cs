using System.Collections;
using System.Collections.Generic;
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

    // Layered mode: every clip runs on its own source, all of them started together and
    // never stopped. Switching "tracks" only moves the volume around, so a layer that
    // comes back later picks up wherever it had got to instead of restarting.
    private readonly List<AudioSource> layerSources = new List<AudioSource>();
    private AudioClip[] layerClips;
    private int activeLayer = -1;

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

    // ---- Single track ------------------------------------------------------

    public void PlayMusic(AudioClip clip)
    {
        if (audioSource == null || clip == null) return;
        if (audioSource.clip == clip && layerClips == null) return;

        ClearLayers();

        // A track can change again mid-fade; only one fade may own the volume at a time.
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

    // ---- Layered tracks ----------------------------------------------------

    // Starts every clip at once, with only activeIndex audible. Calling this again with
    // the same clips is a no-op beyond selecting the active layer, so it is safe to call
    // from Start or from an Update guard.
    public void PlayLayers(AudioClip[] clips, int activeIndex)
    {
        if (clips == null || clips.Length == 0) return;

        if (SameClips(clips))
        {
            SetActiveLayer(activeIndex);
            return;
        }

        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
            fadeRoutine = null;
        }

        // The single-track source is not part of the layer set; silence it.
        if (audioSource != null)
        {
            audioSource.Stop();
            audioSource.clip = null;
        }

        ClearLayers();
        layerClips = clips;

        // Schedule every layer off one dsp timestamp so they start together instead of
        // drifting by whatever each Play() call costs.
        double startTime = AudioSettings.dspTime + 0.1;

        for (int i = 0; i < clips.Length; i++)
        {
            AudioSource src = CreateLayerSource(i, clips[i]);
            layerSources.Add(src);

            if (clips[i] == null) continue;

            src.volume = 0f;
            src.PlayScheduled(startTime);
        }

        activeLayer = -1;
        SetActiveLayer(activeIndex, instant: true);
    }

    // Crossfades to the given layer. Everything keeps playing underneath.
    public void SetActiveLayer(int index, bool instant = false)
    {
        if (layerSources.Count == 0) return;
        if (index < 0 || index >= layerSources.Count) return;
        if (index == activeLayer) return;

        activeLayer = index;

        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        if (instant)
        {
            for (int i = 0; i < layerSources.Count; i++)
                if (layerSources[i] != null)
                    layerSources[i].volume = i == index ? defaultVolume : 0f;

            fadeRoutine = null;
            return;
        }

        fadeRoutine = StartCoroutine(CrossfadeToLayer(index));
    }

    private IEnumerator CrossfadeToLayer(int index)
    {
        // Fade from wherever each layer currently sits, so an interrupted crossfade
        // continues smoothly instead of snapping.
        int count = layerSources.Count;
        float[] startVolumes = new float[count];
        for (int i = 0; i < count; i++)
            startVolumes[i] = layerSources[i] != null ? layerSources[i].volume : 0f;

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            float t = elapsed / fadeDuration;
            for (int i = 0; i < count; i++)
            {
                if (layerSources[i] == null) continue;
                float target = i == index ? defaultVolume : 0f;
                layerSources[i].volume = Mathf.Lerp(startVolumes[i], target, t);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        for (int i = 0; i < count; i++)
            if (layerSources[i] != null)
                layerSources[i].volume = i == index ? defaultVolume : 0f;

        fadeRoutine = null;
    }

    private bool SameClips(AudioClip[] clips)
    {
        if (layerClips == null || layerClips.Length != clips.Length) return false;

        for (int i = 0; i < clips.Length; i++)
            if (layerClips[i] != clips[i]) return false;

        return true;
    }

    private AudioSource CreateLayerSource(int index, AudioClip clip)
    {
        GameObject go = new GameObject("MusicLayer_" + index);
        go.transform.SetParent(transform, false);

        AudioSource src = go.AddComponent<AudioSource>();
        src.clip = clip;
        src.loop = true;
        src.playOnAwake = false;
        src.volume = 0f;

        // Inherit routing/3D settings from the manager's own source so layers sound
        // exactly like the single-track path did.
        if (audioSource != null)
        {
            src.outputAudioMixerGroup = audioSource.outputAudioMixerGroup;
            src.spatialBlend = audioSource.spatialBlend;
            src.priority = audioSource.priority;
            src.bypassEffects = audioSource.bypassEffects;
            src.bypassListenerEffects = audioSource.bypassListenerEffects;
            src.bypassReverbZones = audioSource.bypassReverbZones;
            src.ignoreListenerPause = audioSource.ignoreListenerPause;
            src.ignoreListenerVolume = audioSource.ignoreListenerVolume;
        }

        return src;
    }

    private void ClearLayers()
    {
        for (int i = 0; i < layerSources.Count; i++)
            if (layerSources[i] != null)
                Destroy(layerSources[i].gameObject);

        layerSources.Clear();
        layerClips = null;
        activeLayer = -1;
    }
}
