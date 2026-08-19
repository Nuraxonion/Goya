using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;

public class ArtShopManager : MonoBehaviour
{
    // =========================================================
    // UPGRADE SLOT
    // =========================================================

    [System.Serializable]
    public class UpgradeSlot
    {
        [Tooltip("Must exactly match the MetaUpgrade ID.")]
        public string upgradeId = "";

        [Tooltip("The WHITE upgrade bubble/button.")]
        public Button button;

        [Tooltip("The Icon Image that is a child of the button.")]
        public Image buttonIcon;

        [Tooltip("The actual pip Images. Assign the child image that turns red.")]
        public Image[] pips;

        [System.NonSerialized]
        public MetaUpgrade definition;

        [System.NonSerialized]
        public int level;

        public bool IsMaxed
        {
            get
            {
                return definition != null &&
                       level >= definition.MaxLevel;
            }
        }
    }


    // =========================================================
    // GENERAL UI
    // =========================================================

    [Header("UI References")]

    public Button backButton;
    public Button refundButton;


    // =========================================================
    // SCENE
    // =========================================================

    [Header("Scene Names")]

    public string mainSceneName =
        "Title Screen and Main Menu";


    // =========================================================
    // UPGRADE SLOTS
    // =========================================================

    [Header("Upgrade Slots")]

    [Tooltip("One slot for every upgrade.")]
    public UpgradeSlot[] upgradeSlots = new UpgradeSlot[0];


    // =========================================================
    // CENTRAL UPGRADE DISPLAY
    // =========================================================

    [Header("Central Upgrade Display")]

    [Tooltip("The Canvas Group on the entire central display.")]
    public CanvasGroup centralUpgradeDisplayGroup;

    [Tooltip("ONE Image inside the central red circle.")]
    public Image centralUpgradeIcon;

    [Tooltip("Upgrade name shown above the red circle.")]
    public TextMeshProUGUI upgradeNameText;

    [Tooltip("Upgrade stats shown below the red circle.")]
    public TextMeshProUGUI upgradeStatsText;

    [Tooltip("Upgrade description shown below the red circle.")]
    public TextMeshProUGUI upgradeDescriptionText;

    [Tooltip("Upgrade cost shown below the red circle.")]
    public TextMeshProUGUI upgradeCostText;


    // =========================================================
    // DISPLAY FADE
    // =========================================================

    [Header("Display Fade")]

    public float displayFadeDuration = 0.2f;

    private Coroutine displayFadeCoroutine;


    // =========================================================
    // PIPS
    // =========================================================

    [Header("Pips")]

    public Color upgradedPipColor = Color.red;
    public Color normalPipColor = Color.white;


    // =========================================================
    // COINS
    // =========================================================

    [Header("Coins")]

    public TextMeshProUGUI coinBalanceText;


    // =========================================================
    // FEEDBACK
    // =========================================================

    [Header("Feedback")]

    public UpgradeNotification upgradeNotification;

    public ParticleSystem upgradeParticles;


    // =========================================================
    // TESTING
    // =========================================================

    [Header("Testing")]

    [Tooltip("Coins given when the shop opens. Set to 0 when finished.")]
    public int testingCoinGrant = 10000;


    // =========================================================
    // INTERNAL
    // =========================================================

    private bool isLoading = false;
    private const string AUTO_UPGRADE_ID = "AutoUpgrade";


    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        // -----------------------------------------------------
        // TESTING COINS
        // -----------------------------------------------------

        if (testingCoinGrant > 0)
        {
            PlayerPrefs.SetInt(
                "Coins",
                testingCoinGrant
            );

            PlayerPrefs.Save();
        }


        // -----------------------------------------------------
        // LOAD UPGRADE DATA
        // -----------------------------------------------------

        ResolveSlots();


        // -----------------------------------------------------
        // BACK BUTTON
        // -----------------------------------------------------

        if (backButton != null)
        {
            backButton.onClick.RemoveAllListeners();

            backButton.onClick.AddListener(
                GoBackToMainMenu
            );

            AddHoverEffect(backButton);
        }


        // -----------------------------------------------------
        // REFUND BUTTON
        // -----------------------------------------------------

        if (refundButton != null)
        {
            refundButton.onClick.RemoveAllListeners();

            refundButton.onClick.AddListener(
                RefundAllUpgrades
            );

            AddHoverEffect(refundButton);
        }


        // -----------------------------------------------------
        // UPGRADE BUTTONS
        // -----------------------------------------------------

        if (upgradeSlots != null)
        {
            for (int i = 0; i < upgradeSlots.Length; i++)
            {
                UpgradeSlot slot = upgradeSlots[i];

                if (slot == null)
                    continue;

                if (slot.button == null)
                    continue;


                UpgradeSlot selectedSlot = slot;


                // CLICK
                selectedSlot.button.onClick.RemoveAllListeners();

                selectedSlot.button.onClick.AddListener(
                    () => Purchase(selectedSlot)
                );


                // HOVER
                AddHoverEffect(
                    selectedSlot.button
                );


                // PIPS
                UpdatePips(
                    selectedSlot
                );


                // BUTTON STATE
                UpdateInteractable(
                    selectedSlot
                );
            }
        }


        // -----------------------------------------------------
        // COINS
        // -----------------------------------------------------

        UpdateCoinUI();


        // -----------------------------------------------------
        // REFUND BUTTON
        // -----------------------------------------------------

        UpdateRefundButton();


        // -----------------------------------------------------
        // CENTRAL DISPLAY STARTS HIDDEN
        // -----------------------------------------------------

        HideUpgradeDisplayImmediate();
    }


    // =========================================================
    // RESOLVE UPGRADE SLOTS
    // =========================================================

    private void ResolveSlots()
    {
        if (upgradeSlots == null)
        {
            upgradeSlots = new UpgradeSlot[0];
            return;
        }


        for (int i = 0; i < upgradeSlots.Length; i++)
        {
            UpgradeSlot slot = upgradeSlots[i];

            if (slot == null)
                continue;


            if (string.IsNullOrEmpty(slot.upgradeId))
            {
                Debug.LogWarning(
                    "ArtShopManager: Upgrade Slot " +
                    i +
                    " has no Upgrade ID."
                );

                continue;
            }


            slot.definition =
                MetaUpgrades.Find(
                    slot.upgradeId
                );


            if (slot.definition == null)
            {
                Debug.LogWarning(
                    "ArtShopManager: Could not find MetaUpgrade with ID: " +
                    slot.upgradeId
                );

                continue;
            }


            slot.level =
                MetaUpgrades.GetLevel(
                    slot.definition
                );
        }
    }


    // =========================================================
    // COIN UI
    // =========================================================

    private void UpdateCoinUI()
    {
        if (coinBalanceText == null)
            return;


        coinBalanceText.text =
            "Coins: " +
            CoinBank.GetCoins();
    }


    // =========================================================
    // BUTTON STATE
    // =========================================================

    private void UpdateInteractable(
        UpgradeSlot slot)
    {
        if (slot == null)
            return;

        if (slot.button == null)
            return;


        slot.button.interactable =
            !slot.IsMaxed;
    }


    // =========================================================
    // PIPS
    // =========================================================

    private void UpdatePips(
        UpgradeSlot slot)
    {
        if (slot == null)
            return;

        if (slot.pips == null)
            return;


        for (int i = 0; i < slot.pips.Length; i++)
        {
            Image pip = slot.pips[i];

            if (pip == null)
                continue;


            if (i < slot.level)
            {
                pip.color =
                    upgradedPipColor;
            }
            else
            {
                pip.color =
                    normalPipColor;
            }
        }
    }


    // =========================================================
    // PURCHASE
    // =========================================================

    public void Purchase(
        UpgradeSlot slot)
    {
        if (slot == null)
            return;

        if (slot.definition == null)
            return;


        MetaUpgrade upgrade =
            slot.definition;


        // -----------------------------------------------------
        // MAXED
        // -----------------------------------------------------

        if (slot.IsMaxed)
        {
            if (upgradeNotification != null)
            {
                upgradeNotification
                    .ShowMaxUpgradeNotification(
                        upgrade.upgradeName
                    );
            }

            return;
        }


        // -----------------------------------------------------
        // COST
        // -----------------------------------------------------

        int currentCost =
            upgrade.CostForLevel(
                slot.level
            );


        // -----------------------------------------------------
        // COINS
        // -----------------------------------------------------

        if (!CoinBank.HasCoins(currentCost))
        {
            if (upgradeNotification != null)
            {
                upgradeNotification
                    .ShowInsufficientFundsNotification();
            }


            StartCoroutine(
                ShowInsufficientFundsFeedback(
                    slot.button
                )
            );

            return;
        }


        // -----------------------------------------------------
        // SPEND
        // -----------------------------------------------------

        if (!CoinBank.SpendCoins(currentCost))
            return;


        // -----------------------------------------------------
        // LEVEL UP
        // -----------------------------------------------------

        slot.level++;


        MetaUpgrades.SetLevel(
            upgrade,
            slot.level
        );


        // -----------------------------------------------------
        // CHECK IF AUTO-UPGRADE WAS PURCHASED
        // -----------------------------------------------------

        if (slot.upgradeId == AUTO_UPGRADE_ID && slot.level >= 1)
        {
            // Auto-Upgrade is now ACTIVE
            PlayerPrefs.SetInt("AutoUpgradeEnabled", 1);
            PlayerPrefs.Save();
            Debug.Log("🚀 Auto-Upgrade ENABLED!");
        }


        // -----------------------------------------------------
        // UPDATE UI
        // -----------------------------------------------------

        UpdatePips(slot);

        UpdateInteractable(slot);

        UpdateCoinUI();

        UpdateRefundButton();


        // -----------------------------------------------------
        // UPDATE CENTRAL DISPLAY
        // -----------------------------------------------------

        ShowUpgradeDisplay(slot);


        // -----------------------------------------------------
        // NOTIFICATION
        // -----------------------------------------------------

        if (upgradeNotification != null)
        {
            if (slot.IsMaxed)
            {
                upgradeNotification
                    .ShowMaxUpgradeNotification(
                        upgrade.upgradeName
                    );
            }
            else
            {
                upgradeNotification
                    .ShowUpgradeNotification(
                        upgrade.upgradeName,
                        upgrade.statsText
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
    }


    // =========================================================
    // ADD HOVER EVENTS
    // =========================================================

    private void AddHoverEffect(
        Button button)
    {
        if (button == null)
            return;


        EventTrigger trigger =
            button.GetComponent<EventTrigger>();


        if (trigger == null)
        {
            trigger =
                button.gameObject.AddComponent<EventTrigger>();
        }


        // POINTER ENTER

        EventTrigger.Entry enter =
            new EventTrigger.Entry();

        enter.eventID =
            EventTriggerType.PointerEnter;


        enter.callback.AddListener(
            (data) =>
            {
                OnButtonHover(button);
            }
        );


        trigger.triggers.Add(
            enter
        );


        // POINTER EXIT

        EventTrigger.Entry exit =
            new EventTrigger.Entry();

        exit.eventID =
            EventTriggerType.PointerExit;


        exit.callback.AddListener(
            (data) =>
            {
                OnButtonExit(button);
            }
        );


        trigger.triggers.Add(
            exit
        );
    }


    // =========================================================
    // HOVER ENTER
    // =========================================================

    private void OnButtonHover(
        Button button)
    {
        if (button == null)
            return;


        // Make the white bubble slightly bigger.

        button.transform.localScale =
            new Vector3(
                1.05f,
                1.05f,
                1f
            );


        // Find the upgrade belonging
        // to this button.

        UpgradeSlot slot =
            FindSlot(button);


        if (slot == null)
            return;


        // Display that upgrade
        // in the ONE central display.

        ShowUpgradeDisplay(slot);
    }


    // =========================================================
    // HOVER EXIT
    // =========================================================

    private void OnButtonExit(
        Button button)
    {
        if (button == null)
            return;


        // Return bubble to normal size.

        button.transform.localScale =
            Vector3.one;


        UpgradeSlot slot =
            FindSlot(button);


        if (slot == null)
            return;


        // Fade the central display out.

        HideUpgradeDisplay();
    }


    // =========================================================
    // FIND UPGRADE SLOT
    // =========================================================

    private UpgradeSlot FindSlot(
        Button button)
    {
        if (button == null)
            return null;


        if (upgradeSlots == null)
            return null;


        for (int i = 0; i < upgradeSlots.Length; i++)
        {
            UpgradeSlot slot =
                upgradeSlots[i];


            if (slot == null)
                continue;


            if (slot.button == button)
            {
                return slot;
            }
        }


        return null;
    }


    // =========================================================
    // SHOW CENTRAL DISPLAY
    // =========================================================

    private void ShowUpgradeDisplay(
        UpgradeSlot slot)
    {
        if (slot == null)
            return;


        if (slot.definition == null)
            return;


        MetaUpgrade upgrade =
            slot.definition;


        // -----------------------------------------------------
        // ICON
        // -----------------------------------------------------

        if (centralUpgradeIcon != null)
        {
            Sprite displaySprite = null;


            // FIRST:
            // Use the icon directly from the
            // hovered white upgrade button.

            if (slot.buttonIcon != null)
            {
                displaySprite =
                    slot.buttonIcon.sprite;
            }


            // FALLBACK:
            // Use the MetaUpgrade icon.

            if (displaySprite == null)
            {
                displaySprite =
                    upgrade.icon;
            }


            centralUpgradeIcon.sprite =
                displaySprite;


            centralUpgradeIcon.enabled =
                displaySprite != null;
        }


        // -----------------------------------------------------
        // NAME
        // -----------------------------------------------------

        if (upgradeNameText != null)
        {
            upgradeNameText.text =
                upgrade.upgradeName;

            upgradeNameText.gameObject.SetActive(
                true
            );
        }


        // -----------------------------------------------------
        // STATS
        // -----------------------------------------------------

        if (upgradeStatsText != null)
        {
            upgradeStatsText.text =
                upgrade.statsText +
                "\nLevel " +
                slot.level +
                "/" +
                upgrade.MaxLevel;

            upgradeStatsText.gameObject.SetActive(
                true
            );
        }


        // -----------------------------------------------------
        // DESCRIPTION
        // -----------------------------------------------------

        if (upgradeDescriptionText != null)
        {
            if (
                slot.IsMaxed &&
                !string.IsNullOrEmpty(
                    upgrade.maxedDescription
                )
            )
            {
                upgradeDescriptionText.text =
                    upgrade.maxedDescription;
            }
            else
            {
                upgradeDescriptionText.text =
                    upgrade.description;
            }


            upgradeDescriptionText.gameObject.SetActive(
                true
            );
        }


        // -----------------------------------------------------
        // COST
        // -----------------------------------------------------

        if (upgradeCostText != null)
        {
            if (slot.IsMaxed)
            {
                upgradeCostText.text =
                    "Upgrade Maxed Out";
            }
            else
            {
                int cost =
                    upgrade.CostForLevel(
                        slot.level
                    );


                upgradeCostText.text =
                    cost +
                    " Coins";
            }


            upgradeCostText.gameObject.SetActive(
                true
            );
        }


        // -----------------------------------------------------
        // FADE IN
        // -----------------------------------------------------

        if (centralUpgradeDisplayGroup != null)
        {
            if (displayFadeCoroutine != null)
            {
                StopCoroutine(
                    displayFadeCoroutine
                );
            }


            displayFadeCoroutine =
                StartCoroutine(
                    FadeDisplay(
                        centralUpgradeDisplayGroup.alpha,
                        1f
                    )
                );
        }
    }


    // =========================================================
    // HIDE CENTRAL DISPLAY
    // =========================================================

    private void HideUpgradeDisplay()
    {
        if (centralUpgradeDisplayGroup == null)
            return;


        if (displayFadeCoroutine != null)
        {
            StopCoroutine(
                displayFadeCoroutine
            );
        }


        displayFadeCoroutine =
            StartCoroutine(
                FadeDisplay(
                    centralUpgradeDisplayGroup.alpha,
                    0f
                )
            );
    }


    // =========================================================
    // HIDE DISPLAY IMMEDIATELY
    // =========================================================

    private void HideUpgradeDisplayImmediate()
    {
        if (displayFadeCoroutine != null)
        {
            StopCoroutine(
                displayFadeCoroutine
            );

            displayFadeCoroutine = null;
        }


        if (centralUpgradeDisplayGroup != null)
        {
            centralUpgradeDisplayGroup.alpha = 0f;
        }


        if (centralUpgradeIcon != null)
        {
            centralUpgradeIcon.sprite = null;
            centralUpgradeIcon.enabled = false;
        }


        if (upgradeNameText != null)
            upgradeNameText.gameObject.SetActive(false);

        if (upgradeStatsText != null)
            upgradeStatsText.gameObject.SetActive(false);

        if (upgradeDescriptionText != null)
            upgradeDescriptionText.gameObject.SetActive(false);

        if (upgradeCostText != null)
            upgradeCostText.gameObject.SetActive(false);
    }


    // =========================================================
    // FADE COROUTINE
    // =========================================================

    private System.Collections.IEnumerator FadeDisplay(
        float startAlpha,
        float targetAlpha)
    {
        if (centralUpgradeDisplayGroup == null)
            yield break;


        float elapsed = 0f;


        while (elapsed < displayFadeDuration)
        {
            elapsed += Time.deltaTime;


            float progress =
                Mathf.Clamp01(
                    elapsed /
                    displayFadeDuration
                );


            progress =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    progress
                );


            centralUpgradeDisplayGroup.alpha =
                Mathf.Lerp(
                    startAlpha,
                    targetAlpha,
                    progress
                );


            yield return null;
        }


        centralUpgradeDisplayGroup.alpha =
            targetAlpha;


        displayFadeCoroutine = null;
    }


    // =========================================================
    // REFUND
    // =========================================================

    public void RefundAllUpgrades()
    {
        MetaUpgradeSet set =
            MetaUpgrades.Set;


        if (set == null ||
            set.upgrades == null)
        {
            return;
        }


        int refund = 0;


        foreach (
            MetaUpgrade upgrade
            in set.upgrades)
        {
            if (upgrade == null)
                continue;


            int level =
                MetaUpgrades.GetLevel(
                    upgrade
                );


            refund +=
                upgrade.TotalSpentAt(
                    level
                );


            MetaUpgrades.SetLevel(
                upgrade,
                0
            );
        }


        if (refund > 0)
        {
            CoinBank.AddCoins(
                refund
            );
        }


        // DISABLE AUTO-UPGRADE ON REFUND
        PlayerPrefs.SetInt("AutoUpgradeEnabled", 0);
        PlayerPrefs.Save();
        Debug.Log("🔴 Auto-Upgrade DISABLED (refunded)");


        // Reload all slots.

        ResolveSlots();


        // Update all pips.

        if (upgradeSlots != null)
        {
            for (int i = 0;
                 i < upgradeSlots.Length;
                 i++)
            {
                UpgradeSlot slot =
                    upgradeSlots[i];


                if (slot == null)
                    continue;


                UpdatePips(slot);

                UpdateInteractable(slot);
            }
        }


        UpdateCoinUI();

        UpdateRefundButton();

        HideUpgradeDisplay();


        if (upgradeNotification != null)
        {
            upgradeNotification
                .ShowRefundNotification(
                    refund
                );
        }
    }


    // =========================================================
    // REFUND BUTTON STATE
    // =========================================================

    private void UpdateRefundButton()
    {
        if (refundButton == null)
            return;


        bool anythingBought = false;


        MetaUpgradeSet set =
            MetaUpgrades.Set;


        if (set != null &&
            set.upgrades != null)
        {
            foreach (
                MetaUpgrade upgrade
                in set.upgrades)
            {
                if (upgrade == null)
                    continue;


                if (
                    MetaUpgrades.GetLevel(
                        upgrade
                    ) > 0
                )
                {
                    anythingBought = true;
                    break;
                }
            }
        }


        refundButton.interactable =
            anythingBought;
    }


    // =========================================================
    // NOT ENOUGH COINS FEEDBACK
    // =========================================================

    private System.Collections.IEnumerator
        ShowInsufficientFundsFeedback(
            Button button)
    {
        if (button == null)
            yield break;


        ColorBlock colors =
            button.colors;


        Color originalColor =
            colors.normalColor;


        colors.normalColor =
            Color.red;


        button.colors =
            colors;


        yield return new WaitForSeconds(
            0.5f
        );


        colors.normalColor =
            originalColor;


        button.colors =
            colors;
    }


    // =========================================================
    // BACK TO MAIN MENU
    // =========================================================

    public void GoBackToMainMenu()
    {
        if (isLoading)
            return;


        isLoading = true;


        SceneManager.LoadScene(
            mainSceneName
        );
    }


    // =========================================================
    // CHECK IF AUTO-UPGRADE IS ACTIVE (Static Helper)
    // =========================================================

    public static bool IsAutoUpgradeActive()
    {
        // Check if AutoUpgrade is purchased (level >= 1)
        MetaUpgrade autoUpgrade = MetaUpgrades.Find("AutoUpgrade");
        if (autoUpgrade == null) return false;

        int level = MetaUpgrades.GetLevel(autoUpgrade);
        return level >= 1;
    }
}