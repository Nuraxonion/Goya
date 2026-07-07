using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

public class MenuManager : MonoBehaviour
{
    [Header("Menu Panels")]
    public CanvasGroup titleScreen;
    public CanvasGroup mainMenu;
    public CanvasGroup settingsMenu;
    public CanvasGroup creditsMenu;

    [Header("Settings")]
    public Slider volumeSlider;
    public Slider sensitivitySlider;
    public static float mouseSensitivity = 1f;

    [Header("Scene Names")]
    public string shopSceneName = "ArtShop";

    [Header("Fade Settings")]
    public float fadeSpeed = 2f;

    private bool isTransitioning = false;

    void Start()
    {
        bool returnToMainMenu = PlayerPrefs.GetInt("ReturnToMainMenu", 0) == 1;

        if (returnToMainMenu)
        {
            PlayerPrefs.SetInt("ReturnToMainMenu", 0);
            PlayerPrefs.Save();

            HideAllMenus();
            ShowMenu(mainMenu);
            StartCoroutine(AddHoverEffectsAfterLoad());
            return;
        }

        HideAllMenus();
        ShowMenu(titleScreen);

        float savedVolume = PlayerPrefs.GetFloat("GameVolume", 0.5f);
        AudioListener.volume = Mathf.Clamp01(savedVolume);

        if (volumeSlider != null)
        {
            volumeSlider.minValue = 0.0001f;
            volumeSlider.maxValue = 1f;
            volumeSlider.value = Mathf.Clamp01(savedVolume);
            volumeSlider.onValueChanged.RemoveAllListeners();
            volumeSlider.onValueChanged.AddListener(SetVolume);
        }

        mouseSensitivity = PlayerPrefs.GetFloat("MouseSensitivity", 1f);

        if (sensitivitySlider != null)
        {
            sensitivitySlider.value = mouseSensitivity;
            sensitivitySlider.onValueChanged.RemoveAllListeners();
            sensitivitySlider.onValueChanged.AddListener(SetSensitivity);
        }

        AddHoverEffectsToAllButtons();
    }

    private IEnumerator AddHoverEffectsAfterLoad()
    {
        yield return null;
        AddHoverEffectsToAllButtons();
    }

    private void AddHoverEffect(Button button)
    {
        if (button == null)
        {
            return;
        }

        EventTrigger trigger = button.gameObject.GetComponent<EventTrigger>();

        if (trigger == null)
        {
            trigger = button.gameObject.AddComponent<EventTrigger>();
        }
        else
        {
            trigger.triggers.Clear();
        }

        EventTrigger.Entry entryEnter = new EventTrigger.Entry();
        entryEnter.eventID = EventTriggerType.PointerEnter;
        entryEnter.callback.AddListener((data) => { OnButtonHover(button); });
        trigger.triggers.Add(entryEnter);

        EventTrigger.Entry entryExit = new EventTrigger.Entry();
        entryExit.eventID = EventTriggerType.PointerExit;
        entryExit.callback.AddListener((data) => { OnButtonExit(button); });
        trigger.triggers.Add(entryExit);
    }

    private void AddHoverEffectsToAllButtons()
    {
        Button[] allButtons = FindObjectsOfType<Button>(true);

        foreach (Button button in allButtons)
        {
            AddHoverEffect(button);
        }
    }

    private void OnButtonHover(Button button)
    {
        if (button != null)
        {
            button.transform.localScale = new Vector3(1.05f, 1.05f, 1.05f);
        }
    }

    private void OnButtonExit(Button button)
    {
        if (button != null)
        {
            button.transform.localScale = Vector3.one;
        }
    }

    public void StartGame()
    {
        if (!isTransitioning)
        {
            StartCoroutine(FadeTitleToMenu());
        }
    }

    IEnumerator FadeTitleToMenu()
    {
        isTransitioning = true;

        titleScreen.interactable = false;
        titleScreen.blocksRaycasts = false;

        while (titleScreen.alpha > 0)
        {
            titleScreen.alpha -= Time.deltaTime * fadeSpeed;
            yield return null;
        }

        titleScreen.alpha = 0;
        titleScreen.gameObject.SetActive(false);

        ShowMenu(mainMenu);

        mainMenu.alpha = 0;

        while (mainMenu.alpha < 1)
        {
            mainMenu.alpha += Time.deltaTime * fadeSpeed;
            yield return null;
        }

        mainMenu.alpha = 1;
        isTransitioning = false;
        AddHoverEffectsToAllButtons();
    }

    public void OpenSettings()
    {
        HideAllMenus();
        ShowMenu(settingsMenu);
        AddHoverEffectsToAllButtons();

        if (volumeSlider != null)
        {
            volumeSlider.minValue = 0.0001f;
            volumeSlider.maxValue = 1f;
            float savedVolume = PlayerPrefs.GetFloat("GameVolume", 0.5f);
            volumeSlider.value = Mathf.Clamp01(savedVolume);
            AudioListener.volume = Mathf.Clamp01(savedVolume);
        }

        if (sensitivitySlider != null)
        {
            sensitivitySlider.value = PlayerPrefs.GetFloat("MouseSensitivity", 1f);
        }
    }

    public void OpenShop()
    {
        PlayerPrefs.SetInt("ReturnToMainMenu", 1);
        PlayerPrefs.Save();
        SceneManager.LoadScene(shopSceneName);
    }

    public void OpenCredits()
    {
        HideAllMenus();
        ShowMenu(creditsMenu);
        AddHoverEffectsToAllButtons();
    }

    public void CloseSettings()
    {
        HideAllMenus();
        ShowMenu(mainMenu);
        AddHoverEffectsToAllButtons();
    }

    public void CloseCredits()
    {
        HideAllMenus();
        ShowMenu(mainMenu);
        AddHoverEffectsToAllButtons();
    }

    public void SetVolume(float value)
    {
        float clampedValue = Mathf.Clamp01(value);
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

    public void QuitGame()
    {
        Debug.Log("Quitting game...");
        Application.Quit();
    }

    private void ShowMenu(CanvasGroup menu)
    {
        menu.gameObject.SetActive(true);
        menu.alpha = 1;
        menu.interactable = true;
        menu.blocksRaycasts = true;
    }

    private void HideMenu(CanvasGroup menu)
    {
        menu.alpha = 0;
        menu.interactable = false;
        menu.blocksRaycasts = false;
        menu.gameObject.SetActive(false);
    }

    private void HideAllMenus()
    {
        HideMenu(titleScreen);
        HideMenu(mainMenu);
        HideMenu(settingsMenu);
        HideMenu(creditsMenu);
    }
}