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

            // Clear existing listeners and add new one
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
                bottleHoverEffect.enabled = false;

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

        if (bottleButton != null)
        {
            bottleButton.interactable = true;
            Debug.Log("✅ Bottle button is now INTERACTABLE!");
        }

        if (bottleHoverEffect != null)
        {
            bottleHoverEffect.enabled = true;
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
        }
    }

    // ========== PUBLIC METHOD FOR BUTTON ==========
    public void OnBottleClicked()
    {
        Debug.Log("🍾 BOTTLE CLICKED! - Button event fired!");

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
        }

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
        redFillXPBar.fillAmount = 1f;
        redFillXPBottle.fillAmount = 1f;

        if (bottleHoverEffect != null)
            bottleHoverEffect.enabled = false;
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

        if (bottleButton != null)
        {
            bottleButton.interactable = false;
        }

        if (glareImage != null)
        {
            glareImage.gameObject.SetActive(false);
        }

        if (bottleHoverEffect != null)
            bottleHoverEffect.enabled = false;

        if (playerXP != null && playerXP.IsUpgradeReady())
        {
            Debug.Log("More upgrades available!");
        }
    }
}