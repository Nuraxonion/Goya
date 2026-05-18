using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour
{
    [Header("Menus")]
    public CanvasGroup titleScreen;
    public CanvasGroup mainMenu;
    public CanvasGroup settingsMenu;

    [Header("Settings Sliders")]
    public Slider volumeSlider;
    public Slider sensitivitySlider;

    [Header("Settings Values")]
    public float mouseSensitivity = 1f;

    [Header("Fade Settings")]
    public float fadeSpeed = 2f;

    bool transitioning = false;

    void Start()
    {
        // Setup slider values
        volumeSlider.value = AudioListener.volume;
        sensitivitySlider.value = mouseSensitivity;

        // Slider listeners
        volumeSlider.onValueChanged.AddListener(SetVolume);
        sensitivitySlider.onValueChanged.AddListener(SetSensitivity);

        // Initial menu states

        // Title Screen ON
        titleScreen.alpha = 1;
        titleScreen.interactable = true;
        titleScreen.blocksRaycasts = true;

        // Main Menu OFF
        mainMenu.alpha = 0;
        mainMenu.interactable = false;
        mainMenu.blocksRaycasts = false;

        // Settings Menu OFF
        settingsMenu.alpha = 0;
        settingsMenu.interactable = false;
        settingsMenu.blocksRaycasts = false;
    }

    public void StartGame()
    {
        Debug.Log("START BUTTON CLICKED");

        if (!transitioning)
        {
            StartCoroutine(FadeTitleToMenu());
        }
    }

    IEnumerator FadeTitleToMenu()
    {
        transitioning = true;

        // Fade OUT title screen
        while (titleScreen.alpha > 0)
        {
            titleScreen.alpha -= Time.deltaTime * fadeSpeed;
            yield return null;
        }

        titleScreen.alpha = 0;
        titleScreen.interactable = false;
        titleScreen.blocksRaycasts = false;

        // Prepare main menu
        mainMenu.alpha = 0;
        mainMenu.interactable = true;
        mainMenu.blocksRaycasts = true;

        // Fade IN main menu
        while (mainMenu.alpha < 1)
        {
            mainMenu.alpha += Time.deltaTime * fadeSpeed;
            yield return null;
        }

        mainMenu.alpha = 1;

        transitioning = false;
    }

    public void OpenSettings()
    {
        // Hide main menu
        mainMenu.alpha = 0;
        mainMenu.interactable = false;
        mainMenu.blocksRaycasts = false;

        // Show settings menu
        settingsMenu.alpha = 1;
        settingsMenu.interactable = true;
        settingsMenu.blocksRaycasts = true;
    }

    public void CloseSettings()
    {
        // Hide settings menu
        settingsMenu.alpha = 0;
        settingsMenu.interactable = false;
        settingsMenu.blocksRaycasts = false;

        // Show main menu
        mainMenu.alpha = 1;
        mainMenu.interactable = true;
        mainMenu.blocksRaycasts = true;
    }

    public void SetVolume(float volume)
    {
        AudioListener.volume = volume;
    }

    public void SetSensitivity(float sensitivity)
    {
        mouseSensitivity = sensitivity;

        Debug.Log("Mouse Sensitivity: " + sensitivity);
    }

    public void QuitGame()
    {
        Debug.Log("QUIT GAME");

        Application.Quit();
    }
}