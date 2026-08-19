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

    [Tooltip("Grows the bottle's clickable area beyond its own rect (left, bottom, right, top). The click Image lives on XPBottle, whose rect is 100x100, but the visible bottle is drawn by larger children - InkBottleFrame is 240x220 - so without this only the middle fifth of the bottle is clickable. Raise this if the artwork grows.")]
    public Vector4 bottleClickPadding = new Vector4(70f, 70f, 70f, 70f);

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

            // Negative padding EXPANDS the hit rect. Without this the clickable
            // area is XPBottle's own 100x100 rect, while the bottle you can see is
            // its 240x220 InkBottleFrame child - so most clicks on the bottle miss.
            bottleImage.raycastPadding = -bottleClickPadding;

            bottleButton = xpBottle.GetComponent<Button>();
            if (bottleButton == null)
            {
                bottleButton = xpBottle.AddComponent<Button>();
                Debug.Log("Added Button component to XPBottle");
            }

            bottleButton.targetGraphic = bottleImage;
            bottleButton.onClick.RemoveAllListeners();
            bottleButton.onClick.AddListener(OnBottleClicked);
            bottleButton.interactable = false;

            Debug.Log($"✅ Bottle button setup complete");
        }
        else
        {
            Debug.LogError("❌ xpBottle reference is null! Please assign it in the Inspector.");
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
            bottleHoverEffect.ResetScale();
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

        if (isWaitingForUpgrade)
        {
            redFillXPBar.fillAmount = 1f;
            redFillXPBottle.fillAmount = 1f;

            if (xpBottle.activeSelf)
                xpBottle.SetActive(false);

            if (glareImage != null && glareImage.gameObject.activeSelf)
            {
                glareImage.gameObject.SetActive(false);
            }

            if (bottleHoverEffect != null)
            {
                bottleHoverEffect.enabled = false;
                bottleHoverEffect.ResetScale();
            }

            return;
        }

        float targetProgress = Mathf.Clamp01(playerXP.xpLevel / playerXP.requiredXP);

        displayedProgress = Mathf.MoveTowards(
            displayedProgress,
            targetProgress,
            fillSpeed * Time.unscaledDeltaTime);

        float barFill = Mathf.Clamp01(displayedProgress / barPortion);
        redFillXPBar.fillAmount = barFill;

        float bottleFill = 0f;
        if (displayedProgress > barPortion)
        {
            bottleFill = Mathf.Clamp01(
                (displayedProgress - barPortion) / (1f - barPortion));
        }
        redFillXPBottle.fillAmount = bottleFill;

        bool upgradeReady = displayedProgress >= 0.99f && !isWaitingForUpgrade;

        if (upgradeReady && !isUpgradeReady)
        {
            OnUpgradeReady();
        }
        else if (!upgradeReady && isUpgradeReady)
        {
            OnUpgradeNotReady();
        }

        if (isUpgradeReady && glareImage != null && glareImage.gameObject.activeSelf)
        {
            PulseGlare();
        }

        if (isWaitingForUpgrade)
        {
            if (xpBottle.activeSelf)
                xpBottle.SetActive(false);
        }
        else
        {
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

        if (glareImage != null)
        {
            glareImage.gameObject.SetActive(true);
            Color c = glareImage.color;
            c.a = 0.3f;
            glareImage.color = c;
        }

        // CHECK IF AUTO-UPGRADE IS ACTIVE
        if (ArtShopManager.IsAutoUpgradeActive())
        {
            // Auto-upgrade is active - keep button disabled (no click needed)
            if (bottleButton != null)
            {
                bottleButton.interactable = false;
                Debug.Log("🔒 Auto-Upgrade active - bottle click DISABLED");
            }

            if (bottleHoverEffect != null)
            {
                bottleHoverEffect.enabled = false;
                bottleHoverEffect.ResetScale();
            }
        }
        else
        {
            // Normal mode - bottle is clickable
            if (bottleButton != null)
            {
                // Re-arm the click handler at the moment it actually has to work.
                // Idempotent - RemoveListener then AddListener leaves exactly one.
                // PauseManager used to clear every runtime onClick in the scene from
                // its Start(), which silently killed this listener depending on which
                // Start() ran first; this makes the bottle immune to that class of bug.
                bottleButton.onClick.RemoveListener(OnBottleClicked);
                bottleButton.onClick.AddListener(OnBottleClicked);

                bottleButton.interactable = true;
                Debug.Log("✅ Bottle button is now INTERACTABLE!");
            }
            else
            {
                // Warning rather than Log: if Start() never wired the button there is
                // nothing to click, and this needs to be visible even when the Console
                // is filtering plain log messages out.
                Debug.LogWarning("Upgrade is ready but the bottle Button was never set up - the bottle cannot be clicked.");
            }

            if (bottleHoverEffect != null)
            {
                bottleHoverEffect.enabled = true;
                bottleHoverEffect.ResetScale();
            }
        }

        Debug.Log("Upgrade Ready! Click the bottle to level up!");
    }

    void OnUpgradeNotReady()
    {
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
            bottleHoverEffect.ResetScale();
        }
    }

    public void OnBottleClicked()
    {
        Debug.Log("🍾 BOTTLE CLICKED! - Button event fired!");

        // If auto-upgrade is active, clicking does nothing
        if (ArtShopManager.IsAutoUpgradeActive())
        {
            Debug.Log("🔒 Auto-Upgrade active - bottle click IGNORED");
            return;
        }

        if (isWaitingForUpgrade)
        {
            Debug.LogWarning("Already waiting for upgrade, ignoring click");
            return;
        }

        if (playerXP == null)
        {
            Debug.LogError("playerXP is null!");
            return;
        }

        // Gate on the XP model, not on the animated fill. displayedProgress creeps
        // at fillSpeed and crosses 0.99 slightly before xpLevel actually reaches
        // requiredXP, so the two disagree in that window - the UI would arm the
        // button for a level-up that PlayerXP then refuses.
        if (!playerXP.IsUpgradeReady())
        {
            Debug.LogWarning("Bottle clicked but PlayerXP says the upgrade is not ready yet.");
            return;
        }

        Debug.Log("Bottle clicked! Triggering level up!");

        // Only latch AFTER the level up is confirmed. Latching first and then
        // having TriggerLevelUp early-return would leave isWaitingForUpgrade stuck
        // true forever - Update()'s waiting branch keeps the bottle hidden, and
        // only a successful upgrade ever clears it. One missed click would end the
        // run's progression.
        if (!playerXP.TriggerLevelUp())
        {
            Debug.LogWarning("TriggerLevelUp refused the level up - leaving the bottle clickable.");
            return;
        }

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

        if (bottleHoverEffect != null)
        {
            bottleHoverEffect.enabled = false;
            bottleHoverEffect.ResetScale();
        }
    }

    public void ResetXPUI()
    {
        displayedProgress = 0f;

        redFillXPBar.fillAmount = 0f;
        redFillXPBottle.fillAmount = 0f;

        xpBottle.SetActive(true);
        ResetBottleScale();
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
            bottleHoverEffect.ResetScale();
        }
    }

    public void ResetBottleScale()
    {
        if (xpBottle != null)
        {
            xpBottle.transform.localScale = Vector3.one;
        }
    }

    public void HideBottle()
    {
        if (xpBottle != null)
            xpBottle.SetActive(false);

        if (bottleHoverEffect != null)
        {
            bottleHoverEffect.enabled = false;
            bottleHoverEffect.ResetScale();
        }
    }

    public void ShowBottle()
    {
        if (xpBottle != null)
        {
            xpBottle.SetActive(true);
            ResetBottleScale();
        }

        if (bottleHoverEffect != null)
        {
            bottleHoverEffect.enabled = false;
            bottleHoverEffect.ResetScale();
        }
    }

    public void OnLevelUp()
    {
        redFillXPBar.fillAmount = 1f;
        redFillXPBottle.fillAmount = 1f;

        if (bottleHoverEffect != null)
        {
            bottleHoverEffect.enabled = false;
            bottleHoverEffect.ResetScale();
        }
    }

    public void OnUpgradeSelected()
    {
        isWaitingForUpgrade = false;
        isUpgradeReady = false;

        if (playerXP != null)
        {
            displayedProgress = playerXP.xpLevel / playerXP.requiredXP;
            displayedProgress = Mathf.Clamp01(displayedProgress);
        }

        xpBottle.SetActive(true);
        ResetBottleScale();

        if (bottleButton != null)
        {
            bottleButton.interactable = false;
        }

        if (glareImage != null)
        {
            glareImage.gameObject.SetActive(false);
        }

        if (bottleHoverEffect != null)
        {
            bottleHoverEffect.enabled = false;
            bottleHoverEffect.ResetScale();
        }

        if (playerXP != null && playerXP.IsUpgradeReady())
        {
            Debug.Log("More upgrades available!");
        }
    }
}