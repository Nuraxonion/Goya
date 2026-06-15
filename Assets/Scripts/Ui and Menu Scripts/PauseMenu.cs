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

    public static float mouseSensitivity = 1f;

    private bool isPaused = false;

    [Header("Brush Script that needs disabling")]
    public MonoBehaviour brushManager;

    void Start()
    {
        pauseMenu.SetActive(false);
        settingsMenu.SetActive(false);

        if (pauseButton != null)
            pauseButton.SetActive(true);

        Time.timeScale = 1f;

        if (volumeSlider != null)
        {
            volumeSlider.value = AudioListener.volume;
            volumeSlider.onValueChanged.AddListener(SetVolume);
        }

        if (sensitivitySlider != null)
        {
            sensitivitySlider.value = mouseSensitivity;
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
    }

    public void CloseSettings()
    {
        settingsMenu.SetActive(false);
        pauseMenu.SetActive(true);

        // ok drawing can come back now
        if (brushManager != null)
            brushManager.enabled = true;
    }

    public void QuitToTitle()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("Title Screen and Main Menu");
    }

    public void SetVolume(float value)
    {
        AudioListener.volume = value;
    }

    public void SetSensitivity(float value)
    {
        mouseSensitivity = value;
    }
}