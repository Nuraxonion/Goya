using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseManager : MonoBehaviour
{
    [Header("Menus")]
    public GameObject pauseMenu;
    public GameObject settingsMenu;

    [Header("UI")]
    public GameObject pauseButton;

    [Header("Settings")]
    public Slider volumeSlider;
    public Slider sensitivitySlider;

    [Header("Audio")]
    public AudioSource musicSource;

    public static float mouseSensitivity = 1f;

    private bool isPaused = false;

    [Header("bruh")]
    public MonoBehaviour brushManager;

    void Start()
    {
        pauseMenu.SetActive(false);
        settingsMenu.SetActive(false);

        if (pauseButton != null)
            pauseButton.SetActive(true);

        Time.timeScale = 1f;

        float savedVolume = PlayerPrefs.GetFloat("GameVolume", 0.5f);

        if (musicSource != null)
            musicSource.volume = savedVolume;

        AudioListener.volume = savedVolume;

        mouseSensitivity = PlayerPrefs.GetFloat("MouseSensitivity", 1f);

        if (volumeSlider != null)
        {
            volumeSlider.minValue = 0f;
            volumeSlider.maxValue = 1f;
            volumeSlider.value = savedVolume;
            volumeSlider.onValueChanged.RemoveAllListeners();
            volumeSlider.onValueChanged.AddListener(SetVolume);
        }

        if (sensitivitySlider != null)
        {
            sensitivitySlider.value = mouseSensitivity;
            sensitivitySlider.onValueChanged.RemoveAllListeners();
            sensitivitySlider.onValueChanged.AddListener(SetSensitivity);
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (settingsMenu.activeSelf)
            {
                CloseSettings();
            }
            else if (isPaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }
    }

    public void PauseGame()
    {
        pauseMenu.SetActive(true);
        settingsMenu.SetActive(false);

        if (pauseButton != null)
            pauseButton.SetActive(false);

        if (brushManager != null)
            brushManager.enabled = false;

        Time.timeScale = 0f;
        isPaused = true;
    }

    public void ResumeGame()
    {
        pauseMenu.SetActive(false);
        settingsMenu.SetActive(false);

        if (pauseButton != null)
            pauseButton.SetActive(true);

        if (brushManager != null)
            brushManager.enabled = true;

        Time.timeScale = 1f;
        isPaused = false;
    }

    public void OpenSettings()
    {
        pauseMenu.SetActive(false);
        settingsMenu.SetActive(true);

        if (brushManager != null)
            brushManager.enabled = false;

        if (volumeSlider != null)
        {
            volumeSlider.minValue = 0f;
            volumeSlider.maxValue = 1f;
            float savedVolume = PlayerPrefs.GetFloat("GameVolume", 0.5f);
            volumeSlider.value = savedVolume;

            if (musicSource != null)
                musicSource.volume = savedVolume;

            AudioListener.volume = savedVolume;
        }

        if (sensitivitySlider != null)
        {
            sensitivitySlider.value = PlayerPrefs.GetFloat("MouseSensitivity", 1f);
        }
    }

    public void CloseSettings()
    {
        settingsMenu.SetActive(false);
        pauseMenu.SetActive(true);

        if (brushManager != null)
            brushManager.enabled = true;
    }

    public void SetVolume(float value)
    {
        float clampedValue = Mathf.Clamp01(value);

        if (musicSource != null)
            musicSource.volume = clampedValue;

        AudioListener.volume = clampedValue;

        PlayerPrefs.SetFloat("GameVolume", clampedValue);
        PlayerPrefs.Save();
    }

    public void SetSensitivity(float value)
    {
        mouseSensitivity = value;
        PlayerPrefs.SetFloat("MouseSensitivity", value);
        PlayerPrefs.Save();
    }

    public void QuitToTitle()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("Title Screen and Main Menu");
    }
}