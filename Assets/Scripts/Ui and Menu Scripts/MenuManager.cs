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
    public float mouseSensitivity = 1f;

    [Header("FADE SETTINGS")]
    public float fadeSpeed = 2f;

    private bool transitioning = false;

    void Start()
    {
        // Hide all menus first
        HideAllMenus();

        // Show title screen at startup
        ShowMenu(titleScreen);

        // Setup sliders
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

        // Make sure title screen is interactable
        titleScreen.interactable = false;
        titleScreen.blocksRaycasts = false;

        // Fade OUT title screen
        while (titleScreen.alpha > 0)
        {
            titleScreen.alpha -= Time.deltaTime * fadeSpeed;
            yield return null;
        }

        titleScreen.alpha = 0;
        titleScreen.gameObject.SetActive(false);

        // Enable Main Menu
        ShowMenu(mainMenu);

        // Fade IN Main Menu
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
