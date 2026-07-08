using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using TMPro;

public class ArtShopManager : MonoBehaviour
{
    [Header("UI References")]
    public Button backButton;

    [Header("Scene Names")]
    public string mainSceneName = "Title Screen and Main Menu";

    [Header("Health Upgrade")]
    public Button healthUpgradeButton;
    public TextMeshProUGUI healthUpgradeStatusText;
    public TextMeshProUGUI healthUpgradeCostText;
    public TextMeshProUGUI currentHealthText;
    public TextMeshProUGUI coinBalanceText;
    public UpgradeNotification upgradeNotification;
    public ParticleSystem upgradeParticles;
    public int[] upgradeCosts = new int[] { 100, 200, 300 };
    public float healthIncreasePerUpgrade = 30f;

    private const string HEALTH_UPGRADE_KEY = "HealthUpgradeCount";
    private const string MAX_HEALTH_KEY = "MaxHealth";
    private int healthUpgradeCount = 0;
    private float maxHealth = 100f;
    private bool isLoading = false;

    void Start()
    {
        LoadHealthData();

        if (backButton != null)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(GoBackToMainMenu);
            AddHoverEffect(backButton);
        }

        if (healthUpgradeButton != null)
        {
            healthUpgradeButton.onClick.RemoveAllListeners();
            healthUpgradeButton.onClick.AddListener(UpgradeHealth);
            AddHoverEffect(healthUpgradeButton);
        }

        AddHoverEffectsToAllButtons();
        UpdateHealthUI();
        UpdateCoinUI();
    }

    private void UpdateCoinUI()
    {
        if (coinBalanceText != null)
        {
            coinBalanceText.text = "Coins: " + CoinBank.GetCoins();
        }
    }

    private void LoadHealthData()
    {
        healthUpgradeCount = PlayerPrefs.GetInt(HEALTH_UPGRADE_KEY, 0);
        maxHealth = PlayerPrefs.GetFloat(MAX_HEALTH_KEY, 100f);

        if (healthUpgradeCount == 0)
        {
            maxHealth = 100f;
        }
    }

    private void SaveHealthData()
    {
        PlayerPrefs.SetInt(HEALTH_UPGRADE_KEY, healthUpgradeCount);
        PlayerPrefs.SetFloat(MAX_HEALTH_KEY, maxHealth);
        PlayerPrefs.Save();
    }

    private void UpdateHealthUI()
    {
        if (healthUpgradeButton != null)
        {
            bool isMaxed = healthUpgradeCount >= 3;
            healthUpgradeButton.interactable = !isMaxed;

            if (healthUpgradeStatusText != null)
            {
                if (isMaxed)
                {
                    healthUpgradeStatusText.text = "MAX HEALTH";
                }
                else
                {
                    healthUpgradeStatusText.text = "Upgrade " + (healthUpgradeCount + 1) + "/3";
                }
            }

            if (healthUpgradeCostText != null)
            {
                if (isMaxed)
                {
                    healthUpgradeCostText.text = "FULLY UPGRADED";
                }
                else
                {
                    healthUpgradeCostText.text = upgradeCosts[healthUpgradeCount] + " Coins";
                }
            }
        }

        if (currentHealthText != null)
        {
            currentHealthText.text = " Max HP: " + maxHealth;
        }
    }

    public void UpgradeHealth()
    {
        if (healthUpgradeCount >= 3)
        {
            if (upgradeNotification != null)
            {
                upgradeNotification.ShowMaxUpgradeNotification();
            }

            return;
        }

        int currentCost = upgradeCosts[healthUpgradeCount];
        int playerCoins = GetPlayerCoins();

        if (playerCoins < currentCost)
        {
            if (upgradeNotification != null)
            {
                upgradeNotification.ShowInsufficientFundsNotification();
            }

            StartCoroutine(ShowInsufficientFundsFeedback());
            return;
        }

        DeductPlayerCoins(currentCost);
        healthUpgradeCount++;
        maxHealth += healthIncreasePerUpgrade;
        SaveHealthData();
        UpdateHealthUI();
        UpdateCoinUI();

        if (upgradeNotification != null)
        {
            if (healthUpgradeCount >= 3)
            {
                upgradeNotification.ShowMaxUpgradeNotification();
            }
            else
            {
                upgradeNotification.ShowUpgradeNotification(healthIncreasePerUpgrade);
            }
        }

        if (upgradeParticles != null)
        {
            upgradeParticles.Play();
        }

        UpdatePlayerHealthInGame();
    }

    private void UpdatePlayerHealthInGame()
    {
        PlayerHealth playerHealth = FindObjectOfType<PlayerHealth>();

        if (playerHealth != null)
        {
            playerHealth.IncreaseMaxHealth(healthIncreasePerUpgrade);
        }
    }

    private int GetPlayerCoins()
    {
        return CoinBank.GetCoins();
    }

    private void DeductPlayerCoins(int amount)
    {
        CoinBank.SpendCoins(amount);
        Debug.Log("Spent " + amount + " coins");
    }

    private System.Collections.IEnumerator ShowInsufficientFundsFeedback()
    {
        if (healthUpgradeButton != null)
        {
            ColorBlock colors = healthUpgradeButton.colors;
            Color originalColor = colors.normalColor;
            colors.normalColor = Color.red;
            healthUpgradeButton.colors = colors;

            yield return new WaitForSeconds(0.5f);

            colors.normalColor = originalColor;
            healthUpgradeButton.colors = colors;
        }
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
            if (button != backButton && button != healthUpgradeButton)
            {
                AddHoverEffect(button);
            }
        }
    }

    private void OnButtonHover(Button button)
    {
        button.transform.localScale = new Vector3(1.05f, 1.05f, 1.05f);

        Transform glowTransform = button.transform.Find("Glow");

        if (glowTransform != null)
        {
            glowTransform.gameObject.SetActive(true);
        }
    }

    private void OnButtonExit(Button button)
    {
        button.transform.localScale = Vector3.one;

        Transform glowTransform = button.transform.Find("Glow");

        if (glowTransform != null)
        {
            glowTransform.gameObject.SetActive(false);
        }
    }

    public void GoBackToMainMenu()
    {
        if (isLoading)
        {
            return;
        }

        isLoading = true;
        SceneManager.LoadScene(mainSceneName);
    }
}