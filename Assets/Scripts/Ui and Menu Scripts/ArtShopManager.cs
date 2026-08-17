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


    // =========================================================
    // HEALTH UPGRADE BUTTON
    // =========================================================

    [Header("Health Upgrade Button")]

    public Button healthUpgradeButton;

    // Background behind the white health upgrade button
    public Transform healthUpgradeBackground;


    // =========================================================
    // UPGRADE DISPLAY
    // =========================================================

    [Header("Upgrade Display")]

    // Icon displayed inside the red circle
    public Image centralUpgradeIcon;

    // Name displayed above the red circle
    public TextMeshProUGUI upgradeNameText;

    // Information displayed below the red circle
    public TextMeshProUGUI upgradeStatsText;
    public TextMeshProUGUI upgradeDescriptionText;
    public TextMeshProUGUI upgradeCostText;


    // =========================================================
    // HEALTH UPGRADE INFORMATION
    // =========================================================

    [Header("Health Upgrade Information")]

    public string healthUpgradeName = "Vitality";

    [TextArea(2, 4)]
    public string healthUpgradeDescription =
        "Increase your maximum health.";

    public string healthUpgradeStats =
        "+30 Max Health";

    public Sprite healthUpgradeIcon;


    // =========================================================
    // PIPS
    // =========================================================

    [Header("Upgrade Pips")]

    // Drag the CHILD Image objects here:
    //
    // Element 0 = Pip1
    // Element 1 = Pip2
    // Element 2 = Pip3
    // Element 3 = Pip4
    // Element 4 = Pip5
    // Element 5 = Pip6
    //
    // Do NOT drag the PipBackground objects.

    public Image[] healthPips;

    public Color upgradedPipColor = Color.red;
    public Color normalPipColor = Color.white;


    // =========================================================
    // UPGRADE SETTINGS
    // =========================================================

    [Header("Upgrade Settings")]

    // Six upgrade levels
    public int[] upgradeCosts =
    {
        100,
        200,
        300,
        400,
        500,
        600
    };

    public float healthIncreasePerUpgrade = 30f;


    // =========================================================
    // OTHER UI
    // =========================================================

    [Header("Other UI")]

    public TextMeshProUGUI currentHealthText;

    // Drag TXT_Coins here
    public TextMeshProUGUI coinBalanceText;


    // =========================================================
    // FEEDBACK
    // =========================================================

    [Header("Feedback")]

    public UpgradeNotification upgradeNotification;
    public ParticleSystem upgradeParticles;


    // =========================================================
    // SAVE DATA
    // =========================================================

    private const string HEALTH_UPGRADE_KEY =
        "HealthUpgradeCount";

    private const string MAX_HEALTH_KEY =
        "MaxHealth";

    private int healthUpgradeCount = 0;

    private float maxHealth = 100f;

    private bool isLoading = false;


    // =========================================================
    // START
    // =========================================================

    void Start()
    {
        // =====================================================
        // TESTING ONLY
        // =====================================================
        // Gives the player 10,000 coins whenever this
        // Art Shop scene starts.
        //
        // REMOVE THESE TWO LINES WHEN YOU ARE DONE TESTING.
        // =====================================================

        PlayerPrefs.SetInt("Coins", 10000);
        PlayerPrefs.Save();


        // =====================================================
        // LOAD HEALTH DATA
        // =====================================================

        LoadHealthData();


        // =====================================================
        // BACK BUTTON
        // =====================================================

        if (backButton != null)
        {
            backButton.onClick.RemoveAllListeners();

            backButton.onClick.AddListener(
                GoBackToMainMenu
            );

            AddHoverEffect(backButton);
        }


        // =====================================================
        // HEALTH UPGRADE BUTTON
        // =====================================================

        if (healthUpgradeButton != null)
        {
            healthUpgradeButton.onClick.RemoveAllListeners();

            healthUpgradeButton.onClick.AddListener(
                UpgradeHealth
            );

            AddHoverEffect(healthUpgradeButton);
        }


        // =====================================================
        // INITIAL UI UPDATE
        // =====================================================

        UpdateHealthPips();
        UpdateHealthUI();
        UpdateCoinUI();


        // Don't show information until
        // the white bubble is hovered.
        HideUpgradeDisplay();
    }


    // =========================================================
    // COIN DISPLAY
    // =========================================================

    private void UpdateCoinUI()
    {
        if (coinBalanceText != null)
        {
            coinBalanceText.text =
                "Coins: " + CoinBank.GetCoins();
        }
    }


    // =========================================================
    // LOAD HEALTH DATA
    // =========================================================

    private void LoadHealthData()
    {
        healthUpgradeCount =
            PlayerPrefs.GetInt(
                HEALTH_UPGRADE_KEY,
                0
            );

        maxHealth =
            PlayerPrefs.GetFloat(
                MAX_HEALTH_KEY,
                100f
            );

        // Keep upgrade level between 0 and 6
        healthUpgradeCount =
            Mathf.Clamp(
                healthUpgradeCount,
                0,
                6
            );

        // If no upgrades have been purchased,
        // make sure health starts at 100.
        if (healthUpgradeCount == 0)
        {
            maxHealth = 100f;
        }
    }


    // =========================================================
    // SAVE HEALTH DATA
    // =========================================================

    private void SaveHealthData()
    {
        PlayerPrefs.SetInt(
            HEALTH_UPGRADE_KEY,
            healthUpgradeCount
        );

        PlayerPrefs.SetFloat(
            MAX_HEALTH_KEY,
            maxHealth
        );

        PlayerPrefs.Save();
    }


    // =========================================================
    // HEALTH UI
    // =========================================================

    private void UpdateHealthUI()
    {
        bool isMaxed =
            healthUpgradeCount >= 6;


        // -----------------------------------------------------
        // ENABLE / DISABLE BUTTON
        // -----------------------------------------------------

        if (healthUpgradeButton != null)
        {
            healthUpgradeButton.interactable =
                !isMaxed;
        }


        // -----------------------------------------------------
        // UPDATE COST
        // -----------------------------------------------------

        if (upgradeCostText != null)
        {
            if (isMaxed)
            {
                upgradeCostText.text =
                    "Upgrade Maxed Out";
            }
            else
            {
                upgradeCostText.text =
                    upgradeCosts[healthUpgradeCount]
                    + " Coins";
            }
        }


        // -----------------------------------------------------
        // CURRENT HEALTH
        // -----------------------------------------------------

        if (currentHealthText != null)
        {
            currentHealthText.text =
                "Max HP: " + maxHealth;
        }
    }


    // =========================================================
    // UPDATE PIPS
    // =========================================================

    private void UpdateHealthPips()
    {
        if (healthPips == null)
        {
            return;
        }

        for (int i = 0; i < healthPips.Length; i++)
        {
            if (healthPips[i] == null)
            {
                continue;
            }

            // Purchased upgrades turn red
            if (i < healthUpgradeCount)
            {
                healthPips[i].color =
                    upgradedPipColor;
            }
            else
            {
                // Unpurchased upgrades remain white
                healthPips[i].color =
                    normalPipColor;
            }
        }
    }


    // =========================================================
    // PURCHASE HEALTH UPGRADE
    // =========================================================

    public void UpgradeHealth()
    {
        // -----------------------------------------------------
        // MAX LEVEL CHECK
        // -----------------------------------------------------

        if (healthUpgradeCount >= 6)
        {
            if (upgradeNotification != null)
            {
                upgradeNotification
                    .ShowMaxUpgradeNotification();
            }

            return;
        }


        // -----------------------------------------------------
        // CURRENT COST
        // -----------------------------------------------------

        int currentCost =
            upgradeCosts[healthUpgradeCount];


        // -----------------------------------------------------
        // CHECK COINS
        // -----------------------------------------------------

        int playerCoins =
            CoinBank.GetCoins();

        if (playerCoins < currentCost)
        {
            if (upgradeNotification != null)
            {
                upgradeNotification
                    .ShowInsufficientFundsNotification();
            }

            StartCoroutine(
                ShowInsufficientFundsFeedback()
            );

            return;
        }


        // -----------------------------------------------------
        // SPEND COINS
        // -----------------------------------------------------

        bool successfullySpent =
            CoinBank.SpendCoins(currentCost);

        if (!successfullySpent)
        {
            return;
        }


        // -----------------------------------------------------
        // INCREASE LEVEL
        // -----------------------------------------------------

        healthUpgradeCount++;


        // -----------------------------------------------------
        // INCREASE MAX HEALTH
        // -----------------------------------------------------

        maxHealth +=
            healthIncreasePerUpgrade;


        // -----------------------------------------------------
        // SAVE
        // -----------------------------------------------------

        SaveHealthData();


        // -----------------------------------------------------
        // UPDATE UI
        // -----------------------------------------------------

        UpdateHealthPips();
        UpdateHealthUI();
        UpdateCoinUI();


        // -----------------------------------------------------
        // NOTIFICATION
        // -----------------------------------------------------

        if (upgradeNotification != null)
        {
            if (healthUpgradeCount >= 6)
            {
                upgradeNotification
                    .ShowMaxUpgradeNotification();
            }
            else
            {
                upgradeNotification
                    .ShowUpgradeNotification(
                        healthIncreasePerUpgrade
                    );
            }
        }


        // -----------------------------------------------------
        // PARTICLES
        // -----------------------------------------------------

        if (upgradeParticles != null)
        {
            upgradeParticles.Play();
        }


        // -----------------------------------------------------
        // UPDATE PLAYER HEALTH
        // -----------------------------------------------------

        UpdatePlayerHealthInGame();
    }


    // =========================================================
    // UPDATE PLAYER HEALTH
    // =========================================================

    private void UpdatePlayerHealthInGame()
    {
        PlayerHealth playerHealth =
            FindObjectOfType<PlayerHealth>();

        if (playerHealth != null)
        {
            playerHealth.IncreaseMaxHealth(
                healthIncreasePerUpgrade
            );
        }
    }


    // =========================================================
    // HOVER EFFECT
    // =========================================================

    private void AddHoverEffect(Button button)
    {
        if (button == null)
        {
            return;
        }

        EventTrigger trigger =
            button.gameObject.GetComponent<EventTrigger>();

        if (trigger == null)
        {
            trigger =
                button.gameObject.AddComponent<EventTrigger>();
        }


        // -----------------------------------------------------
        // MOUSE ENTER
        // -----------------------------------------------------

        EventTrigger.Entry entryEnter =
            new EventTrigger.Entry();

        entryEnter.eventID =
            EventTriggerType.PointerEnter;

        entryEnter.callback.AddListener(
            (data) =>
            {
                OnButtonHover(button);
            }
        );

        trigger.triggers.Add(entryEnter);


        // -----------------------------------------------------
        // MOUSE EXIT
        // -----------------------------------------------------

        EventTrigger.Entry entryExit =
            new EventTrigger.Entry();

        entryExit.eventID =
            EventTriggerType.PointerExit;

        entryExit.callback.AddListener(
            (data) =>
            {
                OnButtonExit(button);
            }
        );

        trigger.triggers.Add(entryExit);
    }


    // =========================================================
    // BUTTON HOVER
    // =========================================================

    private void OnButtonHover(Button button)
    {
        // -----------------------------------------------------
        // HEALTH UPGRADE BUTTON
        // -----------------------------------------------------

        if (button == healthUpgradeButton)
        {
            // Enlarge white bubble
            button.transform.localScale =
                new Vector3(
                    1.05f,
                    1.05f,
                    1.05f
                );


            // Enlarge background
            if (healthUpgradeBackground != null)
            {
                healthUpgradeBackground.localScale =
                    new Vector3(
                        1.05f,
                        1.05f,
                        1.05f
                    );
            }


            // Enable glow
            Transform glowTransform =
                button.transform.Find("Glow");

            if (glowTransform != null)
            {
                glowTransform.gameObject.SetActive(true);
            }


            // Show upgrade information
            ShowHealthUpgradeDisplay();

            return;
        }


        // -----------------------------------------------------
        // OTHER BUTTONS
        // -----------------------------------------------------

        button.transform.localScale =
            new Vector3(
                1.05f,
                1.05f,
                1.05f
            );

        Transform otherGlow =
            button.transform.Find("Glow");

        if (otherGlow != null)
        {
            otherGlow.gameObject.SetActive(true);
        }
    }


    // =========================================================
    // BUTTON EXIT
    // =========================================================

    private void OnButtonExit(Button button)
    {
        // -----------------------------------------------------
        // HEALTH BUTTON
        // -----------------------------------------------------

        if (button == healthUpgradeButton)
        {
            // Return button to normal size
            button.transform.localScale =
                Vector3.one;


            // Return background to normal size
            if (healthUpgradeBackground != null)
            {
                healthUpgradeBackground.localScale =
                    Vector3.one;
            }


            // Disable glow
            Transform glowTransform =
                button.transform.Find("Glow");

            if (glowTransform != null)
            {
                glowTransform.gameObject.SetActive(false);
            }


            // Hide information
            HideUpgradeDisplay();

            return;
        }


        // -----------------------------------------------------
        // OTHER BUTTONS
        // -----------------------------------------------------

        button.transform.localScale =
            Vector3.one;

        Transform otherGlow =
            button.transform.Find("Glow");

        if (otherGlow != null)
        {
            otherGlow.gameObject.SetActive(false);
        }
    }


    // =========================================================
    // SHOW UPGRADE DISPLAY
    // =========================================================

    private void ShowHealthUpgradeDisplay()
    {
        bool isMaxed =
            healthUpgradeCount >= 6;


        // -----------------------------------------------------
        // NAME
        // -----------------------------------------------------

        if (upgradeNameText != null)
        {
            upgradeNameText.text =
                healthUpgradeName;

            upgradeNameText.gameObject.SetActive(true);
        }


        // -----------------------------------------------------
        // ICON
        // -----------------------------------------------------

        if (centralUpgradeIcon != null)
        {
            centralUpgradeIcon.sprite =
                healthUpgradeIcon;

            centralUpgradeIcon.enabled =
                healthUpgradeIcon != null;
        }


        // -----------------------------------------------------
        // STATS
        // -----------------------------------------------------

        if (upgradeStatsText != null)
        {
            if (isMaxed)
            {
                upgradeStatsText.text =
                    "MAXIMUM HEALTH";
            }
            else
            {
                upgradeStatsText.text =
                    healthUpgradeStats;
            }

            upgradeStatsText.gameObject.SetActive(true);
        }


        // -----------------------------------------------------
        // DESCRIPTION
        // -----------------------------------------------------

        if (upgradeDescriptionText != null)
        {
            if (isMaxed)
            {
                upgradeDescriptionText.text =
                    "Maximum health has been fully upgraded.";
            }
            else
            {
                upgradeDescriptionText.text =
                    healthUpgradeDescription;
            }

            upgradeDescriptionText.gameObject.SetActive(true);
        }


        // -----------------------------------------------------
        // COST
        // -----------------------------------------------------

        if (upgradeCostText != null)
        {
            if (isMaxed)
            {
                upgradeCostText.text =
                    "Upgrade Maxed Out";
            }
            else
            {
                upgradeCostText.text =
                    upgradeCosts[healthUpgradeCount]
                    + " Coins";
            }

            upgradeCostText.gameObject.SetActive(true);
        }
    }


    // =========================================================
    // HIDE UPGRADE DISPLAY
    // =========================================================

    private void HideUpgradeDisplay()
    {
        if (upgradeNameText != null)
        {
            upgradeNameText.gameObject.SetActive(false);
        }

        if (upgradeStatsText != null)
        {
            upgradeStatsText.gameObject.SetActive(false);
        }

        if (upgradeDescriptionText != null)
        {
            upgradeDescriptionText.gameObject.SetActive(false);
        }

        if (upgradeCostText != null)
        {
            upgradeCostText.gameObject.SetActive(false);
        }

        if (centralUpgradeIcon != null)
        {
            centralUpgradeIcon.sprite = null;
            centralUpgradeIcon.enabled = false;
        }
    }


    // =========================================================
    // INSUFFICIENT FUNDS FEEDBACK
    // =========================================================

    private System.Collections.IEnumerator
        ShowInsufficientFundsFeedback()
    {
        if (healthUpgradeButton != null)
        {
            ColorBlock colors =
                healthUpgradeButton.colors;

            Color originalColor =
                colors.normalColor;

            colors.normalColor =
                Color.red;

            healthUpgradeButton.colors =
                colors;

            yield return new WaitForSeconds(0.5f);

            colors.normalColor =
                originalColor;

            healthUpgradeButton.colors =
                colors;
        }
    }


    // =========================================================
    // BACK TO MAIN MENU
    // =========================================================

    public void GoBackToMainMenu()
    {
        if (isLoading)
        {
            return;
        }

        isLoading = true;

        SceneManager.LoadScene(
            mainSceneName
        );
    }
}