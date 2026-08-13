using UnityEngine;
using UnityEngine.UI;

public class InkXPUI : MonoBehaviour
{
    [Header("References")]
    public PlayerXP playerXP;

    public Image redFillXPBar;
    public Image redFillXPBottle;

    public GameObject xpBottle;
    public Image glareImage;
    public BottleHoverEffect bottleHoverEffect;

    [Header("Settings")]
    [Range(0f, 1f)]
    public float barPortion = 0.6f;

    public float fillSpeed = 4f;

    private float displayedProgress = 0f;
    private bool isWaitingForUpgrade = false;
    private bool isUpgradeReady = false;
    private Button bottleButton;
    private Image bottleImage;

    void Start()
    {
        // Setup the bottle button
        if (xpBottle != null)
        {
            bottleImage = xpBottle.GetComponent<Image>();
            if (bottleImage == null)
            {
                bottleImage = xpBottle.AddComponent<Image>();
                Debug.Log("Added Image component to XPBottle");
            }
            bottleImage.raycastTarget = true;

            bottleButton = xpBottle.GetComponent<Button>();
            if (bottleButton == null)
            {
                bottleButton = xpBottle.AddComponent<Button>();
                Debug.Log("Added Button component to XPBottle");
            }

            bottleButton.targetGraphic = bottleImage;
            bottleButton.onClick.AddListener(OnBottleClicked);
            bottleButton.interactable = false;
            Debug.Log("Bottle button setup complete");
        }
        else
        {
            Debug.LogError("xpBottle reference is null! Please assign it in the Inspector.");
        }

        // Disable glare initially
        if (glareImage != null)
        {
            Color c = glareImage.color;
            c.a = 0f;
            glareImage.color = c;
            glareImage.gameObject.SetActive(false);
        }

        // Disable hover effect initially
        if (bottleHoverEffect != null)
        {
            bottleHoverEffect.enabled = false;
        }

        // Ensure XPBottleTarget exists for XP orbs
        if (XPBottleTarget.Instance == null)
        {
            RectTransform bottleRect = xpBottle.GetComponent<RectTransform>();
            if (bottleRect != null && playerXP != null)
            {
                XPBottleTarget.EnsureExists(bottleRect, playerXP);
                Debug.Log("XPBottleTarget created automatically");
            }
        }
        else
        {
            if (XPBottleTarget.Instance.bottleAnchor == null)
            {
                RectTransform bottleRect = xpBottle.GetComponent<RectTransform>();
                XPBottleTarget.Instance.bottleAnchor = bottleRect;
            }
            if (XPBottleTarget.Instance.playerXP == null)
            {
                XPBottleTarget.Instance.playerXP = playerXP;
            }
        }
    }

    void Update()
    {
        if (playerXP == null)
            return;

        // If waiting for upgrade panel to show (after clicking bottle)
        if (isWaitingForUpgrade)
        {
            // Keep bar at 100% but HIDE THE BOTTLE
            redFillXPBar.fillAmount = 1f;
            redFillXPBottle.fillAmount = 1f;

            // HIDE bottle when upgrade panel appears
            if (xpBottle.activeSelf)
                xpBottle.SetActive(false);

            if (glareImage != null && glareImage.gameObject.activeSelf)
            {
                glareImage.gameObject.SetActive(false);
            }

            // Disable hover effect
            if (bottleHoverEffect != null)
                bottleHoverEffect.enabled = false;

            return;
        }

        float targetProgress = Mathf.Clamp01(playerXP.xpLevel / playerXP.requiredXP);

        displayedProgress = Mathf.MoveTowards(
            displayedProgress,
            targetProgress,
            fillSpeed * Time.unscaledDeltaTime);

        // Fill the XP bar first (0% to 60%)
        float barFill = Mathf.Clamp01(displayedProgress / barPortion);
        redFillXPBar.fillAmount = barFill;

        // Then fill the bottle (60% to 100%)
        float bottleFill = 0f;
        if (displayedProgress > barPortion)
        {
            bottleFill = Mathf.Clamp01(
                (displayedProgress - barPortion) / (1f - barPortion));
        }
        redFillXPBottle.fillAmount = bottleFill;

        // Check if upgrade is ready (bottle is fully filled)
        bool upgradeReady = displayedProgress >= 0.99f && !isWaitingForUpgrade;

        if (upgradeReady && !isUpgradeReady)
        {
            OnUpgradeReady();
        }
        else if (!upgradeReady && isUpgradeReady)
        {
            OnUpgradeNotReady();
        }

        // Update glare animation if upgrade is ready
        if (isUpgradeReady && glareImage != null && glareImage.gameObject.activeSelf)
        {
            PulseGlare();
        }

        // Bottle visibility:
        // - KEEP VISIBLE when upgrade is ready (so player can click it)
        // - HIDE when waiting for upgrade panel
        if (isWaitingForUpgrade)
        {
            if (xpBottle.activeSelf)
                xpBottle.SetActive(false);
        }
        else
        {
            // Keep bottle visible at all other times
            if (!xpBottle.activeSelf)
                xpBottle.SetActive(true);
        }
    }

    void PulseGlare()
    {
        if (glareImage == null) return;

        float pulse = Mathf.PingPong(Time.unscaledTime * 2f, 1f);
        float alpha = Mathf.Lerp(0.3f, 1f, pulse);

        Color c = glareImage.color;
        c.a = alpha;
        glareImage.color = c;
    }

    void OnUpgradeReady()
    {
        isUpgradeReady = true;

        // Show glare effect
        if (glareImage != null)
        {
            glareImage.gameObject.SetActive(true);
            Color c = glareImage.color;
            c.a = 0.3f;
            glareImage.color = c;
        }

        // Make the button interactable
        if (bottleButton != null)
        {
            bottleButton.interactable = true;
            Debug.Log("Bottle button is now INTERACTABLE!");
        }

        // ENABLE hover effect on bottle
        if (bottleHoverEffect != null)
        {
            bottleHoverEffect.enabled = true;
            Debug.Log("Bottle hover effect ENABLED!");
        }

        // BOTTLE STAYS VISIBLE - DO NOT HIDE IT
        // The bottle should be visible so player can click it

        Debug.Log("Upgrade Ready! Click the bottle to level up!");
    }

    void OnUpgradeNotReady()
    {
        isUpgradeReady = false;

        // Hide glare
        if (glareImage != null)
        {
            glareImage.gameObject.SetActive(false);
        }

        // Make the button non-interactable
        if (bottleButton != null)
        {
            bottleButton.interactable = false;
        }

        // DISABLE hover effect on bottle
        if (bottleHoverEffect != null)
        {
            bottleHoverEffect.enabled = false;
        }
    }

    void OnBottleClicked()
    {
        Debug.Log("BOTTLE CLICKED! - Button event fired!");

        if (isWaitingForUpgrade)
        {
            Debug.Log("Already waiting for upgrade, ignoring click");
            return;
        }

        if (playerXP == null)
        {
            Debug.LogError("playerXP is null!");
            return;
        }

        if (!isUpgradeReady)
        {
            Debug.Log("Upgrade not ready yet! isUpgradeReady = false");
            return;
        }

        Debug.Log("Bottle clicked! Triggering level up!");

        // HIDE the bottle immediately when clicked (so upgrade panel bottle shows)
        isWaitingForUpgrade = true;
        xpBottle.SetActive(false);

        if (glareImage != null)
        {
            glareImage.gameObject.SetActive(false);
        }

        if (bottleButton != null)
        {
            bottleButton.interactable = false;
        }

        // Disable hover effect
        if (bottleHoverEffect != null)
        {
            bottleHoverEffect.enabled = false;
        }

        // Trigger the level up
        playerXP.TriggerLevelUp();
    }

    public void ResetXPUI()
    {
        displayedProgress = 0f;

        redFillXPBar.fillAmount = 0f;
        redFillXPBottle.fillAmount = 0f;

        xpBottle.SetActive(true);
        isWaitingForUpgrade = false;
        isUpgradeReady = false;

        if (glareImage != null)
        {
            glareImage.gameObject.SetActive(false);
        }

        if (bottleButton != null)
        {
            bottleButton.interactable = false;
        }

        if (bottleHoverEffect != null)
        {
            bottleHoverEffect.enabled = false;
        }
    }

    public void HideBottle()
    {
        if (xpBottle != null)
            xpBottle.SetActive(false);

        if (bottleHoverEffect != null)
            bottleHoverEffect.enabled = false;
    }

    public void ShowBottle()
    {
        if (xpBottle != null)
            xpBottle.SetActive(true);
    }

    public void OnLevelUp()
    {
        // The bottle is already hidden from OnBottleClicked
        // Just make sure the bar shows 100%
        redFillXPBar.fillAmount = 1f;
        redFillXPBottle.fillAmount = 1f;

        // Disable hover effect
        if (bottleHoverEffect != null)
            bottleHoverEffect.enabled = false;
    }

    public void OnUpgradeSelected()
    {
        // After upgrade is selected, show the bottle again with new progress
        isWaitingForUpgrade = false;
        isUpgradeReady = false;

        if (playerXP != null)
        {
            displayedProgress = playerXP.xpLevel / playerXP.requiredXP;
            displayedProgress = Mathf.Clamp01(displayedProgress);
        }

        xpBottle.SetActive(true);

        if (bottleButton != null)
        {
            bottleButton.interactable = false;
        }

        if (glareImage != null)
        {
            glareImage.gameObject.SetActive(false);
        }

        // Disable hover effect until upgrade is ready again
        if (bottleHoverEffect != null)
            bottleHoverEffect.enabled = false;

        // Check if we have more XP for another upgrade
        if (playerXP != null && playerXP.IsUpgradeReady())
        {
            // The Update loop will handle showing the glare again
            Debug.Log("More upgrades available!");
        }
    }
}