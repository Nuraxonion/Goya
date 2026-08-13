using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections.Generic;

public class GameOverManager : MonoBehaviour
{
    [Header("Game Over UI")]
    public GameObject gameOverCanvas;
    public GameObject parallaxBackground;

    public Image farBackground;
    public Image midBackground;

    public TextMeshProUGUI coinsText;
    public Button restartButton;
    public Button artShopButton;
    public Button mainMenuButton;

    [Header("Objects to Hide")]
    public GameObject experienceBarCanvas;
    public GameObject pauseMenuCanvas;
    public GameObject abilityCooldownCanvas;

    [Header("Button Hover Settings")]
    public float hoverScaleMultiplier = 1.1f;
    public float hoverAnimationSpeed = 8f;

    [Header("Parallax Settings")]
    public float farSpeed = 0.3f;
    public float midSpeed = 0.8f;
    public float responseSpeed = 8f;
    public float maxOffsetX = 100f;
    public float maxOffsetY = 50f;

    private bool isGameOver = false;
    private Vector2 farOriginalPos;
    private Vector2 midOriginalPos;
    private Vector2 farCurrentOffset;
    private Vector2 midCurrentOffset;

    // Hover tracking
    private Dictionary<Button, Vector3> originalButtonScales = new Dictionary<Button, Vector3>();
    private Dictionary<Button, bool> buttonHoverStates = new Dictionary<Button, bool>();

    void Start()
    {
        // Add button listeners
        if (restartButton != null)
        {
            restartButton.onClick.AddListener(RestartGame);
            AddHoverEvents(restartButton);
        }

        if (artShopButton != null)
        {
            artShopButton.onClick.AddListener(GoToArtShop);
            AddHoverEvents(artShopButton);
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.AddListener(GoToMainMenu);
            AddHoverEvents(mainMenuButton);
        }

        // Store original positions
        if (farBackground != null)
            farOriginalPos = farBackground.rectTransform.anchoredPosition;

        if (midBackground != null)
            midOriginalPos = midBackground.rectTransform.anchoredPosition;

        // Hide game over canvas at start
        if (gameOverCanvas != null)
            gameOverCanvas.SetActive(false);
    }

    void AddHoverEvents(Button button)
    {
        // Store original scale
        originalButtonScales[button] = button.transform.localScale;
        buttonHoverStates[button] = false;

        // Setup EventTrigger for hover
        UnityEngine.EventSystems.EventTrigger trigger = button.GetComponent<UnityEngine.EventSystems.EventTrigger>();
        if (trigger == null)
            trigger = button.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();

        trigger.triggers.Clear();

        // Pointer Enter
        UnityEngine.EventSystems.EventTrigger.Entry enterEntry = new UnityEngine.EventSystems.EventTrigger.Entry();
        enterEntry.eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter;
        enterEntry.callback.AddListener((data) => {
            if (buttonHoverStates.ContainsKey(button))
                buttonHoverStates[button] = true;
        });
        trigger.triggers.Add(enterEntry);

        // Pointer Exit
        UnityEngine.EventSystems.EventTrigger.Entry exitEntry = new UnityEngine.EventSystems.EventTrigger.Entry();
        exitEntry.eventID = UnityEngine.EventSystems.EventTriggerType.PointerExit;
        exitEntry.callback.AddListener((data) => {
            if (buttonHoverStates.ContainsKey(button))
                buttonHoverStates[button] = false;
        });
        trigger.triggers.Add(exitEntry);

        button.interactable = true;
    }

    void Update()
    {
        if (isGameOver)
        {
            // Update hover animations
            UpdateButtonHoverAnimations();

            // Update parallax
            if (parallaxBackground != null)
                UpdateParallax();
        }
    }

    void UpdateButtonHoverAnimations()
    {
        foreach (Button button in new Button[] { restartButton, artShopButton, mainMenuButton })
        {
            if (button == null) continue;
            if (!originalButtonScales.ContainsKey(button)) continue;

            bool isHovering = buttonHoverStates.ContainsKey(button) && buttonHoverStates[button];
            Vector3 originalScale = originalButtonScales[button];
            Vector3 targetScale = isHovering ? originalScale * hoverScaleMultiplier : originalScale;

            button.transform.localScale = Vector3.Lerp(
                button.transform.localScale,
                targetScale,
                hoverAnimationSpeed * Time.unscaledDeltaTime
            );
        }
    }

    public void ShowGameOver()
    {
        if (isGameOver) return;
        isGameOver = true;

        // Get coins
        int coinsEarned = 0;
        PlayerXP playerXP = FindObjectOfType<PlayerXP>();
        if (playerXP != null)
        {
            coinsEarned = playerXP.EndRunAndAddCoins();
        }

        if (coinsText != null)
        {
            coinsText.text = "Coins earned: " + coinsEarned + "\nTotal coins: " + CoinBank.GetCoins();
        }

        // HIDE THINGS
        if (experienceBarCanvas != null)
            experienceBarCanvas.SetActive(false);

        if (pauseMenuCanvas != null)
            pauseMenuCanvas.SetActive(false);

        if (abilityCooldownCanvas != null)
            abilityCooldownCanvas.SetActive(false);

        // SHOW GAME OVER
        if (parallaxBackground != null)
            parallaxBackground.SetActive(true);

        if (gameOverCanvas != null)
        {
            gameOverCanvas.SetActive(true);
            gameOverCanvas.transform.SetAsLastSibling();
        }

        // Make sure buttons work
        if (restartButton != null)
        {
            restartButton.interactable = true;
            restartButton.gameObject.SetActive(true);
        }
        if (artShopButton != null)
        {
            artShopButton.interactable = true;
            artShopButton.gameObject.SetActive(true);
        }
        if (mainMenuButton != null)
        {
            mainMenuButton.interactable = true;
            mainMenuButton.gameObject.SetActive(true);
        }

        Time.timeScale = 0f;
    }

    void UpdateParallax()
    {
        float mouseX = (Input.mousePosition.x / Screen.width - 0.5f) * 2f;
        float mouseY = (Input.mousePosition.y / Screen.height - 0.5f) * 2f;

        mouseX = Mathf.Clamp(mouseX, -1f, 1f);
        mouseY = Mathf.Clamp(mouseY, -1f, 1f);

        float deltaTime = Time.unscaledDeltaTime;
        if (deltaTime > 0.1f) deltaTime = 0.1f;

        // Far background (slow)
        float farTargetX = mouseX * farSpeed * maxOffsetX;
        float farTargetY = mouseY * farSpeed * maxOffsetY;
        farCurrentOffset.x = Mathf.Lerp(farCurrentOffset.x, farTargetX, responseSpeed * deltaTime);
        farCurrentOffset.y = Mathf.Lerp(farCurrentOffset.y, farTargetY, responseSpeed * deltaTime);

        // Mid background (fast)
        float midTargetX = mouseX * midSpeed * maxOffsetX;
        float midTargetY = mouseY * midSpeed * maxOffsetY;
        midCurrentOffset.x = Mathf.Lerp(midCurrentOffset.x, midTargetX, responseSpeed * deltaTime);
        midCurrentOffset.y = Mathf.Lerp(midCurrentOffset.y, midTargetY, responseSpeed * deltaTime);

        if (farBackground != null)
            farBackground.rectTransform.anchoredPosition = farOriginalPos + farCurrentOffset;

        if (midBackground != null)
            midBackground.rectTransform.anchoredPosition = midOriginalPos + midCurrentOffset;
    }

    // ========== BUTTON METHODS ==========

    public void RestartGame()
    {
        Debug.Log("🔄 Restart Game clicked!");
        Time.timeScale = 1f;
        isGameOver = false;

        if (parallaxBackground != null)
            parallaxBackground.SetActive(false);

        if (gameOverCanvas != null)
            gameOverCanvas.SetActive(false);

        if (experienceBarCanvas != null)
            experienceBarCanvas.SetActive(true);

        if (pauseMenuCanvas != null)
            pauseMenuCanvas.SetActive(true);

        if (abilityCooldownCanvas != null)
            abilityCooldownCanvas.SetActive(true);

        SceneManager.LoadScene("CoreGameplayLoop");
    }

    public void GoToMainMenu()
    {
        Debug.Log("🏠 Main Menu clicked!");
        Time.timeScale = 1f;
        isGameOver = false;

        PlayerPrefs.SetInt("ReturnToMainMenu", 1);
        PlayerPrefs.Save();

        SceneManager.LoadScene("Title Screen and Main Menu");
    }

    public void GoToArtShop()
    {
        Debug.Log("🛒 Art Shop clicked!");
        Time.timeScale = 1f;
        isGameOver = false;

        PlayerPrefs.SetInt("ReturnToMainMenu", 1);
        PlayerPrefs.Save();

        SceneManager.LoadScene("ArtShop");
    }

    void OnDestroy()
    {
        // Clean up listeners
        if (restartButton != null)
            restartButton.onClick.RemoveListener(RestartGame);
        if (artShopButton != null)
            artShopButton.onClick.RemoveListener(GoToArtShop);
        if (mainMenuButton != null)
            mainMenuButton.onClick.RemoveListener(GoToMainMenu);
    }
}