using UnityEngine;
using TMPro;

public class RunTimer : MonoBehaviour
{
    public static RunTimer Instance;

    [Header("UI Reference")]
    [Tooltip("The label that displays the survival time. Found in the children if left empty.")]
    public TextMeshProUGUI timerText;

    [Header("Settings")]
    [Tooltip("Starts counting as soon as the run begins.")]
    public bool runOnStart = true;

    private float elapsed;
    private bool isRunning;
    private int lastDisplayedSecond = -1;

    /// <summary>Seconds survived this run, excluding paused time.</summary>
    public float ElapsedTime => elapsed;

    /// <summary>The survival time formatted for display, e.g. "02:47".</summary>
    public string FormattedTime => Format(elapsed);

    private void Awake()
    {
        Instance = this;

        if (timerText == null)
        {
            timerText = GetComponentInChildren<TextMeshProUGUI>(true);
        }
    }

    private void Start()
    {
        if (runOnStart)
        {
            StartTimer();
        }

        Refresh();
    }

    private void Update()
    {
        if (!isRunning) return;

        // Scaled time, so the clock freezes whenever Time.timeScale is 0
        // (pause menu, upgrade selection, game over).
        elapsed += Time.deltaTime;

        Refresh();
    }

    public void StartTimer()
    {
        isRunning = true;
    }

    public void StopTimer()
    {
        isRunning = false;
    }

    public void ResetTimer()
    {
        elapsed = 0f;
        Refresh();
    }

    private void Refresh()
    {
        if (timerText == null) return;

        // Only rewrite the label when the whole second changes, so TMP does not
        // rebuild its mesh every frame.
        int second = Mathf.FloorToInt(elapsed);

        if (second == lastDisplayedSecond) return;

        lastDisplayedSecond = second;
        timerText.text = Format(elapsed);
    }

    /// <summary>Formats seconds as "MM:SS", or "H:MM:SS" once past an hour.</summary>
    public static string Format(float seconds)
    {
        if (seconds < 0f) seconds = 0f;

        int total = Mathf.FloorToInt(seconds);
        int hours = total / 3600;
        int minutes = (total % 3600) / 60;
        int secs = total % 60;

        if (hours > 0)
        {
            return string.Format("{0}:{1:00}:{2:00}", hours, minutes, secs);
        }

        return string.Format("{0:00}:{1:00}", minutes, secs);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
