using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Full screen intro video that plays once per launch, before the title screen is usable.
//
// The whole overlay is built in code on purpose. The title scene is a large hand maintained
// YAML file, so adding a canvas, a video surface, a hint label and a progress bar by hand
// would be easy to get wrong and painful to merge. Nothing here needs inspector wiring.
//
// Skipping is deliberately awkward: any mouse button reveals the hint, and only a three
// second hold on the left button actually cuts the video short.
public class IntroVideoPlayer : MonoBehaviour
{
    private const string TitleSceneName = "Title Screen and Main Menu";
    private const string VideoResourceName = "mrBeanFillerVideo";

    private const float HoldToSkipSeconds = 3f;
    private const float HintIdleFadeDelay = 4f;
    private const float HintFadeSpeed = 4f;

    // Give up rather than sit on a black screen if the clip never becomes playable.
    private const float PrepareTimeoutSeconds = 8f;

    // The music the menu would have started on its own, faded back in when the intro ends.
    private const float MusicFadeInSeconds = 1.5f;

    private static bool hasPlayedThisLaunch;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        // This callback only fires for the scene the game boots into, so returning to the
        // title later never replays the video. The flag keeps that true even if the callback
        // behaviour ever changes.
        if (hasPlayedThisLaunch) return;
        if (SceneManager.GetActiveScene().name != TitleSceneName) return;

        hasPlayedThisLaunch = true;
        new GameObject("IntroVideoPlayer").AddComponent<IntroVideoPlayer>();
    }

    private VideoPlayer videoPlayer;
    private RenderTexture renderTexture;
    private AudioSource videoAudio;

    private Canvas canvas;
    private RawImage videoSurface;
    private CanvasGroup hintGroup;
    private RectTransform barFill;

    // The menu music source lives on MusicManager and has PlayOnAwake set, so it is already
    // running by the time we get here. We silence it directly rather than going through
    // MusicManager.PlayMusic, which early-returns when the clip is unchanged.
    private AudioSource menuMusicSource;
    private float menuMusicVolume = 1f;

    private readonly List<GraphicRaycaster> suppressedRaycasters = new List<GraphicRaycaster>();

    private float holdTime;

    // Far in the past, so the hint stays hidden until the player actually presses something.
    private float lastPressTime = float.NegativeInfinity;
    private bool finished;

    private void Awake()
    {
        SilenceMenuMusic();

        VideoClip clip = Resources.Load<VideoClip>(VideoResourceName);
        if (clip == null)
        {
            // Most likely the mp4 has not been imported by the editor yet. Never let the
            // intro stand between the player and the menu.
            Debug.LogWarning($"IntroVideoPlayer: no VideoClip named '{VideoResourceName}' in Resources; skipping intro.");
            Finish(false);
            return;
        }

        BuildOverlay(clip);
        StartVideo(clip);

        StartCoroutine(Watchdog(clip));
    }

    // ---- menu music --------------------------------------------------------

    private void SilenceMenuMusic()
    {
        if (MusicManager.Instance != null)
            menuMusicSource = MusicManager.Instance.GetComponent<AudioSource>();

        if (menuMusicSource == null)
        {
            // Called before our own audio source exists, so anything found here is the menu's.
            foreach (AudioSource source in FindObjectsByType<AudioSource>(FindObjectsSortMode.None))
            {
                if (source.isPlaying || source.playOnAwake)
                {
                    menuMusicSource = source;
                    break;
                }
            }
        }

        if (menuMusicSource == null) return;

        // Never latch a silent volume as the level to fade back to.
        menuMusicVolume = menuMusicSource.volume > 0.01f ? menuMusicSource.volume : 1f;
        menuMusicSource.Stop();
        menuMusicSource.volume = 0f;
    }

    private void RestoreMenuMusic()
    {
        if (menuMusicSource == null) return;

        menuMusicSource.volume = 0f;
        if (!menuMusicSource.isPlaying)
            menuMusicSource.Play();

        // Fade in on the MusicManager object so the fade survives this component's destruction.
        MonoBehaviour host = MusicManager.Instance;
        if (host != null && host.isActiveAndEnabled)
            host.StartCoroutine(FadeInMusic(menuMusicSource, menuMusicVolume));
        else
            menuMusicSource.volume = menuMusicVolume;
    }

    private static IEnumerator FadeInMusic(AudioSource source, float targetVolume)
    {
        float elapsed = 0f;
        while (elapsed < MusicFadeInSeconds && source != null)
        {
            elapsed += Time.unscaledDeltaTime;
            source.volume = Mathf.Lerp(0f, targetVolume, elapsed / MusicFadeInSeconds);
            yield return null;
        }

        if (source != null) source.volume = targetVolume;
    }

    // ---- overlay -----------------------------------------------------------

    private void BuildOverlay(VideoClip clip)
    {
        GameObject canvasObject = new GameObject("IntroCanvas", typeof(RectTransform));
        canvasObject.transform.SetParent(transform, false);

        canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100; // the menu canvas sits at 0

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(800f, 600f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        // Without a raycaster the backdrop cannot swallow clicks aimed at the menu buttons.
        canvasObject.AddComponent<GraphicRaycaster>();
        DisableMenuRaycaster();

        Image backdrop = CreateUI<Image>(canvasObject.transform, "Backdrop");
        Stretch(backdrop.rectTransform);
        backdrop.color = Color.black;
        backdrop.raycastTarget = true;

        videoSurface = CreateUI<RawImage>(canvasObject.transform, "VideoSurface");
        Stretch(videoSurface.rectTransform);
        videoSurface.raycastTarget = false;
        videoSurface.color = Color.white;
        videoSurface.enabled = false; // shown once the first frame is ready

        // Letterbox instead of stretching, whatever the clip's native aspect turns out to be.
        AspectRatioFitter fitter = videoSurface.gameObject.AddComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        fitter.aspectRatio = AspectOf(clip);

        BuildHint(canvasObject.transform);
    }

    private void BuildHint(Transform parent)
    {
        GameObject hintObject = new GameObject("SkipHint", typeof(RectTransform));
        hintObject.transform.SetParent(parent, false);

        RectTransform hintRect = (RectTransform)hintObject.transform;
        hintRect.anchorMin = new Vector2(1f, 0f);
        hintRect.anchorMax = new Vector2(1f, 0f);
        hintRect.pivot = new Vector2(1f, 0f);
        hintRect.anchoredPosition = new Vector2(-24f, 20f);
        hintRect.sizeDelta = new Vector2(340f, 42f);

        hintGroup = hintObject.AddComponent<CanvasGroup>();
        hintGroup.alpha = 0f;
        hintGroup.interactable = false;
        hintGroup.blocksRaycasts = false;

        TextMeshProUGUI label = CreateUI<TextMeshProUGUI>(hintRect, "Label");
        RectTransform labelRect = label.rectTransform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(0f, 10f);
        labelRect.offsetMax = Vector2.zero;
        label.text = "Hold Left Mouse Button to Skip...";
        label.fontSize = 18f;
        label.alignment = TextAlignmentOptions.BottomRight;
        label.color = Color.white;
        label.raycastTarget = false;
        if (label.font == null) label.font = TMP_Settings.defaultFontAsset;

        Image barBackground = CreateUI<Image>(hintRect, "SkipBarBackground");
        RectTransform barRect = barBackground.rectTransform;
        barRect.anchorMin = new Vector2(0f, 0f);
        barRect.anchorMax = new Vector2(1f, 0f);
        barRect.pivot = new Vector2(0.5f, 0f);
        barRect.offsetMin = Vector2.zero;
        barRect.offsetMax = new Vector2(0f, 4f);
        barBackground.color = new Color(1f, 1f, 1f, 0.25f);
        barBackground.raycastTarget = false;

        Image fill = CreateUI<Image>(barRect, "SkipBarFill");
        barFill = fill.rectTransform;
        barFill.anchorMin = Vector2.zero;
        barFill.anchorMax = new Vector2(0f, 1f);
        barFill.offsetMin = Vector2.zero;
        barFill.offsetMax = Vector2.zero;
        fill.color = Color.white;
        fill.raycastTarget = false;
    }

    private void DisableMenuRaycaster()
    {
        // CursorSensitivity drives its own raycasts, so belt and braces: take every other
        // raycaster out of play while the video is up. There is more than one canvas in the
        // scene (the cursor prefab brings its own), so this cannot stop at the first hit.
        foreach (GraphicRaycaster raycaster in FindObjectsByType<GraphicRaycaster>(FindObjectsSortMode.None))
        {
            if (raycaster.GetComponent<Canvas>() == canvas) continue;
            if (!raycaster.enabled) continue;

            raycaster.enabled = false;
            suppressedRaycasters.Add(raycaster);
        }
    }

    private void RestoreMenuRaycaster()
    {
        foreach (GraphicRaycaster raycaster in suppressedRaycasters)
        {
            if (raycaster != null) raycaster.enabled = true;
        }

        suppressedRaycasters.Clear();
    }

    // ---- video -------------------------------------------------------------

    private void StartVideo(VideoClip clip)
    {
        // Match the volume the menu would have used, so the intro is not full blast for a
        // frame before MenuManager.Start applies the saved setting.
        AudioListener.volume = Mathf.Clamp01(PlayerPrefs.GetFloat("GameVolume", 0.5f));

        videoAudio = gameObject.AddComponent<AudioSource>();
        videoAudio.playOnAwake = false;

        videoPlayer = gameObject.AddComponent<VideoPlayer>();
        videoPlayer.playOnAwake = false; // defaults to true and would start before we are wired up
        videoPlayer.clip = clip;
        videoPlayer.isLooping = false;   // loopPointReached never fires on a looping player
        videoPlayer.skipOnDrop = true;
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;

        if (clip.audioTrackCount > 0)
        {
            videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
            videoPlayer.controlledAudioTrackCount = 1;
            videoPlayer.EnableAudioTrack(0, true);
            videoPlayer.SetTargetAudioSource(0, videoAudio);
        }
        else
        {
            videoPlayer.audioOutputMode = VideoAudioOutputMode.None;
        }

        // Sized from the clip, not the screen: no resize handling and no zero sized texture
        // if this runs before the window has a size.
        int width = clip.width > 0 ? (int)clip.width : 1280;
        int height = clip.height > 0 ? (int)clip.height : 720;
        renderTexture = new RenderTexture(width, height, 0);
        renderTexture.Create();
        videoPlayer.targetTexture = renderTexture;

        videoPlayer.prepareCompleted += OnPrepared;
        videoPlayer.loopPointReached += OnReachedEnd;
        videoPlayer.errorReceived += OnVideoError;
        videoPlayer.Prepare();
    }

    private void OnPrepared(VideoPlayer source)
    {
        if (finished) return;

        videoSurface.texture = renderTexture;
        videoSurface.enabled = true;
        source.Play();
    }

    private void OnReachedEnd(VideoPlayer source)
    {
        Finish(false);
    }

    private void OnVideoError(VideoPlayer source, string message)
    {
        Debug.LogWarning($"IntroVideoPlayer: video error, skipping intro ({message}).");
        Finish(false);
    }

    private IEnumerator Watchdog(VideoClip clip)
    {
        float waited = 0f;
        while (!finished && videoPlayer != null && !videoPlayer.isPrepared)
        {
            waited += Time.unscaledDeltaTime;
            if (waited > PrepareTimeoutSeconds)
            {
                Debug.LogWarning("IntroVideoPlayer: video took too long to prepare, skipping intro.");
                Finish(false);
                yield break;
            }

            yield return null;
        }

        // Backstop in case loopPointReached is missed on some platform.
        float hardCap = (float)clip.length + 2f;
        float played = 0f;
        while (!finished && played < hardCap)
        {
            played += Time.unscaledDeltaTime;
            yield return null;
        }

        if (!finished) Finish(false);
    }

    // ---- input -------------------------------------------------------------

    private void Update()
    {
        if (finished) return;

        // MusicManager or anything else could try to bring the music back mid intro.
        if (menuMusicSource != null && menuMusicSource.volume != 0f)
            menuMusicSource.volume = 0f;

        bool leftHeld = LeftMouseHeld();

        if (AnyMouseButtonPressedThisFrame() || leftHeld)
            lastPressTime = Time.unscaledTime;

        holdTime = leftHeld ? holdTime + Time.unscaledDeltaTime : 0f;

        float progress = Mathf.Clamp01(holdTime / HoldToSkipSeconds);
        if (barFill != null)
            barFill.anchorMax = new Vector2(progress, 1f);

        // Snap to visible while actually holding, otherwise fade out once the player has been
        // quiet for a while. A later press brings it straight back.
        bool shouldShow = holdTime > 0f || Time.unscaledTime - lastPressTime < HintIdleFadeDelay;
        if (hintGroup != null)
        {
            float target = shouldShow ? 1f : 0f;
            hintGroup.alpha = Mathf.MoveTowards(hintGroup.alpha, target, HintFadeSpeed * Time.unscaledDeltaTime);
        }

        if (holdTime >= HoldToSkipSeconds)
            Finish(true);
    }

    private static bool AnyMouseButtonPressedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        Mouse mouse = Mouse.current;
        if (mouse != null)
        {
            return mouse.leftButton.wasPressedThisFrame
                || mouse.rightButton.wasPressedThisFrame
                || mouse.middleButton.wasPressedThisFrame
                || mouse.forwardButton.wasPressedThisFrame
                || mouse.backButton.wasPressedThisFrame;
        }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2);
#else
        return false;
#endif
    }

    private static bool LeftMouseHeld()
    {
#if ENABLE_INPUT_SYSTEM
        Mouse mouse = Mouse.current;
        if (mouse != null) return mouse.leftButton.isPressed;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetMouseButton(0);
#else
        return false;
#endif
    }

    // ---- teardown ----------------------------------------------------------

    private void Finish(bool skipped)
    {
        if (finished) return;
        finished = true;

        if (videoPlayer != null)
        {
            videoPlayer.prepareCompleted -= OnPrepared;
            videoPlayer.loopPointReached -= OnReachedEnd;
            videoPlayer.errorReceived -= OnVideoError;
            videoPlayer.Stop();
        }

        RestoreMenuMusic();

        if (skipped && isActiveAndEnabled)
        {
            // Hide the overlay immediately but keep swallowing input until the button that
            // skipped the video is released, so that click cannot land on a menu button.
            if (canvas != null) canvas.gameObject.SetActive(false);
            StartCoroutine(DestroyAfterRelease());
        }
        else
        {
            RestoreMenuRaycaster();
            Destroy(gameObject);
        }
    }

    private IEnumerator DestroyAfterRelease()
    {
        while (LeftMouseHeld()) yield return null;
        yield return null;

        RestoreMenuRaycaster();
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        // Runs on every exit path, including an unexpected one, so the menu is never left
        // unclickable or leaking a texture.
        RestoreMenuRaycaster();

        if (videoPlayer != null) videoPlayer.targetTexture = null;
        if (videoSurface != null) videoSurface.texture = null;
        if (renderTexture != null)
        {
            renderTexture.Release();
            Destroy(renderTexture);
            renderTexture = null;
        }
    }

    // ---- small helpers -----------------------------------------------------

    private static float AspectOf(VideoClip clip)
    {
        if (clip.width == 0 || clip.height == 0) return 16f / 9f;
        return (float)clip.width / clip.height;
    }

    private static T CreateUI<T>(Transform parent, string name) where T : Component
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.AddComponent<T>();
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
