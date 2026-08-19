using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using TMPro;

// Drives the Art Shop. Every upgrade on sale is one entry in upgradeSlots, so
// adding another is an inspector entry plus a panel in the scene - the logic
// below is written once and runs for all of them.
//
// The numbers themselves (costs, effect per level, text) are NOT here: they live
// in Assets/Resources/MetaUpgradeSet.asset, which the gameplay scenes read too.
// This class only owns the buying and the drawing.
public class ArtShopManager : MonoBehaviour
{
    // =========================================================
    // UPGRADE SLOT
    // =========================================================

    // The scene half of an upgrade: which definition it shows, and the objects
    // that draw it. Every reference is optional - a slot with no button or pips
    // simply is not displayed, which is how an upgrade can be wired up in logic
    // before anyone has built its UI.
    [System.Serializable]
    public class UpgradeSlot
    {
        [Tooltip("Matches MetaUpgrade.id in the MetaUpgradeSet asset.")]
        public string upgradeId = "";

        public Button button;

        // Background behind the white upgrade button
        public Transform background;

        // The six child Image objects. Element 0 = Pip1 ... Element 5 = Pip6.
        // Do NOT drag the PipBackground objects.
        public Image[] pips;

        [System.NonSerialized] public MetaUpgrade definition;
        [System.NonSerialized] public int level;

        public bool IsMaxed
        {
            get { return definition != null && level >= definition.MaxLevel; }
        }
    }


    // =========================================================
    // UI REFERENCES
    // =========================================================

    [Header("UI References")]
    public Button backButton;

    // Resets every upgrade to level 0 and pays back what they cost.
    public Button refundButton;

    [Header("Scene Names")]
    public string mainSceneName = "Title Screen and Main Menu";


    // =========================================================
    // UPGRADE SLOTS
    // =========================================================

    [Header("Upgrade Slots")]

    // One entry per upgrade on sale. Use Tools > Art Shop > Add Upgrade Panel to
    // add another without rebuilding the button and pips by hand.
    public UpgradeSlot[] upgradeSlots = new UpgradeSlot[0];


    // =========================================================
    // UPGRADE DISPLAY
    // =========================================================

    // Shared by every slot: hovering a button fills these in, leaving it blanks
    // them again.
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
    // PIPS
    // =========================================================

    [Header("Upgrade Pips")]

    public Color upgradedPipColor = Color.red;
    public Color normalPipColor = Color.white;


    // =========================================================
    // OTHER UI
    // =========================================================

    [Header("Other UI")]

    public TextMeshProUGUI currentHealthText;

    [Tooltip("Player health before any Vitality levels. Mirrors PlayerHealth.baseMaxHealth - only used for the Max HP readout.")]
    public float basePlayerHealth = 100f;

    // Drag TXT_Coins here
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

    [Tooltip("Coins granted every time the shop opens, so upgrades can be tried out. Set to 0 before shipping.")]
    public int testingCoinGrant = 10000;


    private bool isLoading = false;


    // =========================================================
    // START
    // =========================================================

    void Start()
    {
        if (testingCoinGrant > 0)
        {
            PlayerPrefs.SetInt("Coins", testingCoinGrant);
            PlayerPrefs.Save();
        }


        // =====================================================
        // LOAD UPGRADES
        // =====================================================

        ResolveSlots();


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
        // REFUND BUTTON
        // =====================================================

        if (refundButton != null)
        {
            refundButton.onClick.RemoveAllListeners();

            refundButton.onClick.AddListener(
                RefundAllUpgrades
            );

            AddHoverEffect(refundButton);
        }


        // =====================================================
        // UPGRADE BUTTONS
        // =====================================================

        for (int i = 0; i < upgradeSlots.Length; i++)
        {
            UpgradeSlot slot = upgradeSlots[i];

            if (slot == null || slot.button == null)
            {
                continue;
            }

            slot.button.onClick.RemoveAllListeners();

            // Captured in a local so every listener buys its own upgrade rather
            // than whichever slot the loop finished on.
            UpgradeSlot purchased = slot;

            slot.button.onClick.AddListener(
                () => Purchase(purchased)
            );

            AddHoverEffect(slot.button);

            UpdatePips(slot);
            UpdateInteractable(slot);
        }


        // =====================================================
        // INITIAL UI UPDATE
        // =====================================================

        UpdateCoinUI();
        UpdateHealthReadout();
        UpdateRefundButton();


        // Do not show information until
        // a white bubble is hovered.
        HideUpgradeDisplay();
    }


    // =========================================================
    // RESOLVE SLOTS
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
            {
                continue;
            }

            slot.definition = MetaUpgrades.Find(slot.upgradeId);

            if (slot.definition == null)
            {
                // Loud on purpose: a mistyped id would otherwise look like a
                // button that just does nothing when clicked.
                Debug.LogWarning(
                    "Art Shop slot " + i + " has no matching upgrade for id '" +
                    slot.upgradeId + "'. Check MetaUpgradeSet.asset.");

                continue;
            }

            slot.level = MetaUpgrades.GetLevel(slot.definition);
        }
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
    // HEALTH READOUT
    // =========================================================

    private void UpdateHealthReadout()
    {
        if (currentHealthText == null)
        {
            return;
        }

        float maxHealth =
            basePlayerHealth +
            MetaUpgrades.GetTotalValue(MetaUpgradeIds.Vitality);

        currentHealthText.text =
            "Max HP: " + maxHealth;
    }


    // =========================================================
    // BUTTON STATE
    // =========================================================

    private void UpdateInteractable(UpgradeSlot slot)
    {
        if (slot == null || slot.button == null)
        {
            return;
        }

        slot.button.interactable = !slot.IsMaxed;
    }


    // =========================================================
    // UPDATE PIPS
    // =========================================================

    private void UpdatePips(UpgradeSlot slot)
    {
        if (slot == null || slot.pips == null)
        {
            return;
        }

        for (int i = 0; i < slot.pips.Length; i++)
        {
            if (slot.pips[i] == null)
            {
                continue;
            }

            // Purchased upgrades turn red
            if (i < slot.level)
            {
                slot.pips[i].color =
                    upgradedPipColor;
            }
            else
            {
                // Unpurchased upgrades remain white
                slot.pips[i].color =
                    normalPipColor;
            }
        }
    }


    // =========================================================
    // PURCHASE
    // =========================================================

    public void Purchase(UpgradeSlot slot)
    {
        if (slot == null || slot.definition == null)
        {
            return;
        }

        MetaUpgrade upgrade = slot.definition;


        // -----------------------------------------------------
        // MAX LEVEL CHECK
        // -----------------------------------------------------

        if (slot.IsMaxed)
        {
            if (upgradeNotification != null)
            {
                upgradeNotification
                    .ShowMaxUpgradeNotification(upgrade.upgradeName);
            }

            return;
        }


        // -----------------------------------------------------
        // CURRENT COST
        // -----------------------------------------------------

        int currentCost =
            upgrade.CostForLevel(slot.level);


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
                ShowInsufficientFundsFeedback(slot.button)
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
        // INCREASE LEVEL AND SAVE
        // -----------------------------------------------------

        // Nothing to poke in the live scene: the gameplay scripts read their
        // levels on spawn, so a purchase applies from the next run onwards.
        slot.level++;

        MetaUpgrades.SetLevel(upgrade, slot.level);


        // -----------------------------------------------------
        // UPDATE UI
        // -----------------------------------------------------

        UpdatePips(slot);
        UpdateInteractable(slot);
        UpdateCoinUI();
        UpdateHealthReadout();
        UpdateRefundButton();

        // Refresh the hovered information so the cost and level counter move up
        // without the player having to leave the button and come back.
        ShowUpgradeDisplay(slot);


        // -----------------------------------------------------
        // NOTIFICATION
        // -----------------------------------------------------

        if (upgradeNotification != null)
        {
            if (slot.IsMaxed)
            {
                upgradeNotification
                    .ShowMaxUpgradeNotification(upgrade.upgradeName);
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
    // REFUND
    // =========================================================

    public void RefundAllUpgrades()
    {
        MetaUpgradeSet set = MetaUpgrades.Set;

        if (set == null || set.upgrades == null)
        {
            return;
        }

        int refund = 0;

        // Driven by the upgrade set rather than upgradeSlots: an upgrade whose panel
        // has not been built yet is still owned by the player and still has to pay back.
        foreach (MetaUpgrade upgrade in set.upgrades)
        {
            if (upgrade == null)
            {
                continue;
            }

            refund += upgrade.TotalSpentAt(MetaUpgrades.GetLevel(upgrade));

            MetaUpgrades.SetLevel(upgrade, 0);
        }

        if (refund > 0)
        {
            CoinBank.AddCoins(refund);
        }


        // -----------------------------------------------------
        // UPDATE UI
        // -----------------------------------------------------

        // Re-read rather than assuming zero: a slot whose id does not resolve has no
        // definition to refund and keeps whatever level it was showing.
        ResolveSlots();

        for (int i = 0; i < upgradeSlots.Length; i++)
        {
            UpdatePips(upgradeSlots[i]);
            UpdateInteractable(upgradeSlots[i]);
        }

        UpdateCoinUI();
        UpdateHealthReadout();
        UpdateRefundButton();

        HideUpgradeDisplay();

        if (upgradeNotification != null)
        {
            upgradeNotification.ShowRefundNotification(refund);
        }
    }


    // =========================================================
    // REFUND BUTTON STATE
    // =========================================================

    // Nothing bought means nothing to give back, so the button greys out rather than
    // paying out zero coins.
    private void UpdateRefundButton()
    {
        if (refundButton == null)
        {
            return;
        }

        bool anythingBought = false;

        MetaUpgradeSet set = MetaUpgrades.Set;

        if (set != null && set.upgrades != null)
        {
            foreach (MetaUpgrade upgrade in set.upgrades)
            {
                if (upgrade != null && MetaUpgrades.GetLevel(upgrade) > 0)
                {
                    anythingBought = true;
                    break;
                }
            }
        }

        refundButton.interactable = anythingBought;
    }


    // =========================================================
    // FIND SLOT
    // =========================================================

    private UpgradeSlot FindSlot(Button button)
    {
        if (button == null || upgradeSlots == null)
        {
            return null;
        }

        for (int i = 0; i < upgradeSlots.Length; i++)
        {
            if (upgradeSlots[i] != null && upgradeSlots[i].button == button)
            {
                return upgradeSlots[i];
            }
        }

        return null;
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
        // Enlarge white bubble
        button.transform.localScale =
            new Vector3(
                1.05f,
                1.05f,
                1.05f
            );


        // Enable glow
        Transform glowTransform =
            button.transform.Find("Glow");

        if (glowTransform != null)
        {
            glowTransform.gameObject.SetActive(true);
        }


        // -----------------------------------------------------
        // UPGRADE BUTTONS
        // -----------------------------------------------------

        UpgradeSlot slot = FindSlot(button);

        if (slot == null)
        {
            // Back button and anything else: no information to show.
            return;
        }


        // Enlarge background
        if (slot.background != null)
        {
            slot.background.localScale =
                new Vector3(
                    1.05f,
                    1.05f,
                    1.05f
                );
        }


        // Show upgrade information
        ShowUpgradeDisplay(slot);
    }


    // =========================================================
    // BUTTON EXIT
    // =========================================================

    private void OnButtonExit(Button button)
    {
        // Return button to normal size
        button.transform.localScale =
            Vector3.one;


        // Disable glow
        Transform glowTransform =
            button.transform.Find("Glow");

        if (glowTransform != null)
        {
            glowTransform.gameObject.SetActive(false);
        }


        // -----------------------------------------------------
        // UPGRADE BUTTONS
        // -----------------------------------------------------

        UpgradeSlot slot = FindSlot(button);

        if (slot == null)
        {
            return;
        }


        // Return background to normal size
        if (slot.background != null)
        {
            slot.background.localScale =
                Vector3.one;
        }


        // Hide information
        HideUpgradeDisplay();
    }


    // =========================================================
    // SHOW UPGRADE DISPLAY
    // =========================================================

    private void ShowUpgradeDisplay(UpgradeSlot slot)
    {
        if (slot == null || slot.definition == null)
        {
            return;
        }

        MetaUpgrade upgrade = slot.definition;

        bool isMaxed = slot.IsMaxed;


        // -----------------------------------------------------
        // NAME
        // -----------------------------------------------------

        if (upgradeNameText != null)
        {
            upgradeNameText.text =
                upgrade.upgradeName;

            upgradeNameText.gameObject.SetActive(true);
        }


        // -----------------------------------------------------
        // ICON
        // -----------------------------------------------------

        if (centralUpgradeIcon != null)
        {
            centralUpgradeIcon.sprite =
                upgrade.icon;

            centralUpgradeIcon.enabled =
                upgrade.icon != null;
        }


        // -----------------------------------------------------
        // STATS
        // -----------------------------------------------------

        if (upgradeStatsText != null)
        {
            upgradeStatsText.text =
                upgrade.statsText +
                "  (Level " + slot.level +
                "/" + upgrade.MaxLevel + ")";

            upgradeStatsText.gameObject.SetActive(true);
        }


        // -----------------------------------------------------
        // DESCRIPTION
        // -----------------------------------------------------

        if (upgradeDescriptionText != null)
        {
            if (isMaxed && !string.IsNullOrEmpty(upgrade.maxedDescription))
            {
                upgradeDescriptionText.text =
                    upgrade.maxedDescription;
            }
            else
            {
                upgradeDescriptionText.text =
                    upgrade.description;
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
                    upgrade.CostForLevel(slot.level)
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
        ShowInsufficientFundsFeedback(Button button)
    {
        if (button != null)
        {
            ColorBlock colors =
                button.colors;

            Color originalColor =
                colors.normalColor;

            colors.normalColor =
                Color.red;

            button.colors =
                colors;

            yield return new WaitForSeconds(0.5f);

            colors.normalColor =
                originalColor;

            button.colors =
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
