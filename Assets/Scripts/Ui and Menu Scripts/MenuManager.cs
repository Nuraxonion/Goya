using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour
{
    [Header("MAIN MENUS")]
    public CanvasGroup titleScreen;
    public CanvasGroup mainMenu;
    public CanvasGroup settingsMenu;
    public CanvasGroup metaShopMenu;
    public CanvasGroup creditsMenu;

    [Header("SETTINGS")]
    public Slider volumeSlider;
    public Slider sensitivitySlider;

    [Header("SETTINGS VALUES")]
    public static float mouseSensitivity = 1f;

    [Header("FADE SETTINGS")]
    public float fadeSpeed = 2f;

    private bool transitioning = false;

    void Start()
    {
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
    }

    public void StartGame()
    {
        if (!transitioning)
        {
            StartCoroutine(FadeTitleToMenu());
        }
    }

    IEnumerator FadeTitleToMenu()
    {
        transitioning = true;

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
        transitioning = false;
    }

    public void OpenSettings()
    {
        HideAllMenus();
        ShowMenu(settingsMenu);

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

    public void OpenMetaShop()
    {
        HideAllMenus();
        ShowMenu(metaShopMenu);
    }

    public void OpenCredits()
    {
        HideAllMenus();
        ShowMenu(creditsMenu);
    }

    public void CloseSettings()
    {
        HideAllMenus();
        ShowMenu(mainMenu);
    }

    public void CloseMetaShop()
    {
        HideAllMenus();
        ShowMenu(mainMenu);
    }

    public void CloseCredits()
    {
        HideAllMenus();
        ShowMenu(mainMenu);
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
        Debug.Log("QUIT GAME");
        Application.Quit();
    }

    void ShowMenu(CanvasGroup menu)
    {
        menu.gameObject.SetActive(true);
        menu.alpha = 1;
        menu.interactable = true;
        menu.blocksRaycasts = true;
    }

    void HideMenu(CanvasGroup menu)
    {
        menu.alpha = 0;
        menu.interactable = false;
        menu.blocksRaycasts = false;
        menu.gameObject.SetActive(false);
    }

    void HideAllMenus()
    {
        HideMenu(titleScreen);
        HideMenu(mainMenu);
        HideMenu(settingsMenu);
        HideMenu(metaShopMenu);
        HideMenu(creditsMenu);
    }
}