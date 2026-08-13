using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections.Generic;

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

    [Header("Cooldown Bubbles")]
    public GameObject abilityCooldownCanvas;

    [Header("Button Hover Settings")]
    public float hoverScaleMultiplier = 1.1f; // 1.1 = 10% bigger
    public float hoverAnimationSpeed = 8f;
    public List<Button> buttonsToAnimate; // Drag all buttons here

    public static float mouseSensitivity = 1f;

    private bool isPaused = false;
    private Dictionary<Button, Vector3> originalButtonScales = new Dictionary<Button, Vector3>();
    private Dictionary<Button, bool> buttonHoverStates = new Dictionary<Button, bool>();

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

        // Setup button hover effects
        SetupButtonHoverEffects();
    }

    void SetupButtonHoverEffects()
    {
        // If no buttons assigned, find all buttons in the scene
        if (buttonsToAnimate == null || buttonsToAnimate.Count == 0)
        {
            Button[] allButtons = FindObjectsOfType<Button>(true);
            buttonsToAnimate = new List<Button>(allButtons);
        }

        foreach (Button button in buttonsToAnimate)
        {
            if (button == null) continue;

            // Store original scale
            originalButtonScales[button] = button.transform.localScale;
            buttonHoverStates[button] = false;

            // Remove existing listeners to avoid duplicates
            button.onClick.RemoveAllListeners();

            // Add hover events using EventTrigger or manual setup
            SetupButtonEvents(button);
        }
    }

    void SetupButtonEvents(Button button)
    {
        // Create an EventTrigger if it doesn't exist
        UnityEngine.EventSystems.EventTrigger trigger = button.GetComponent<UnityEngine.EventSystems.EventTrigger>();
        if (trigger == null)
        {
            trigger = button.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
        }

        // Clear existing entries
        trigger.triggers.Clear();

        // Pointer Enter (hover start)
        UnityEngine.EventSystems.EventTrigger.Entry enterEntry = new UnityEngine.EventSystems.EventTrigger.Entry();
        enterEntry.eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter;
        enterEntry.callback.AddListener((data) => { OnButtonHoverEnter(button); });
        trigger.triggers.Add(enterEntry);

        // Pointer Exit (hover end)
        UnityEngine.EventSystems.EventTrigger.Entry exitEntry = new UnityEngine.EventSystems.EventTrigger.Entry();
        exitEntry.eventID = UnityEngine.EventSystems.EventTriggerType.PointerExit;
        exitEntry.callback.AddListener((data) => { OnButtonHoverExit(button); });
        trigger.triggers.Add(exitEntry);
    }

    void OnButtonHoverEnter(Button button)
    {
        if (button == null) return;
        buttonHoverStates[button] = true;
    }

    void OnButtonHoverExit(Button button)
    {
        if (button == null) return;
        buttonHoverStates[button] = false;
    }

    void Update()
    {
        // Handle button hover animations
        UpdateButtonAnimations();

        // Escape key handling
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

    void UpdateButtonAnimations()
    {
        foreach (Button button in buttonsToAnimate)
        {
            if (button == null) continue;

            bool isHovering = buttonHoverStates.ContainsKey(button) && buttonHoverStates[button];
            Vector3 targetScale = isHovering
                ? originalButtonScales[button] * hoverScaleMultiplier
                : originalButtonScales[button];

            // Smoothly animate to target scale
            button.transform.localScale = Vector3.Lerp(
                button.transform.localScale,
                targetScale,
                hoverAnimationSpeed * Time.unscaledDeltaTime
            );
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

        if (abilityCooldownCanvas != null)
            abilityCooldownCanvas.SetActive(false);

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

        if (abilityCooldownCanvas != null)
            abilityCooldownCanvas.SetActive(true);

        Time.timeScale = 1f;
        isPaused = false;
    }

    public void OpenSettings()
    {
        pauseMenu.SetActive(false);
        settingsMenu.SetActive(true);

        if (brushManager != null)
            brushManager.enabled = false;

        if (abilityCooldownCanvas != null)
            abilityCooldownCanvas.SetActive(false);

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

        if (abilityCooldownCanvas != null)
            abilityCooldownCanvas.SetActive(false);
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

        PlayerPrefs.SetInt("ReturnToMainMenu", 1);
        PlayerPrefs.Save();

        SceneManager.LoadScene("Title Screen and Main Menu");
    }

    // Call this to manually add a button to the hover effect
    public void AddButtonToHoverEffect(Button button)
    {
        if (button == null) return;
        if (buttonsToAnimate.Contains(button)) return;

        buttonsToAnimate.Add(button);
        originalButtonScales[button] = button.transform.localScale;
        buttonHoverStates[button] = false;
        SetupButtonEvents(button);
    }
}