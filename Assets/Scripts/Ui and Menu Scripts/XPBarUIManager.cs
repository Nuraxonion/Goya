using UnityEngine;
using UnityEngine.UI;

public class InkXPUI : MonoBehaviour
{
    [Header("References")]
    public PlayerXP playerXP;

    public Image redFillXPBar;
    public Image redFillXPBottle;

    public GameObject xpBottle;

    [Header("Settings")]
    [Range(0f, 1f)]
    public float barPortion = 0.6f; // CHANGED: Now 60% for the bar, 40% for the bottle

    public float fillSpeed = 4f;

    private float displayedProgress = 0f;
    private bool bottleHiddenByUpgrade = false;
    private bool isWaitingForUpgrade = false;

    void Update()
    {
        if (playerXP == null)
            return;

        // If waiting for upgrade, keep the bar at 100%
        if (isWaitingForUpgrade)
        {
            // Keep the bar and bottle filled at 100%
            redFillXPBar.fillAmount = 1f;
            redFillXPBottle.fillAmount = 1f;

            // Keep bottle hidden during upgrade selection
            if (xpBottle.activeSelf)
                xpBottle.SetActive(false);

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

        // Hide the standing bottle when completely full or when hidden by upgrade
        if (displayedProgress >= 0.99f || bottleHiddenByUpgrade)
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

    public void ResetXPUI()
    {
        displayedProgress = 0f;

        redFillXPBar.fillAmount = 0f;
        redFillXPBottle.fillAmount = 0f;

        xpBottle.SetActive(true);
        bottleHiddenByUpgrade = false;
        isWaitingForUpgrade = false;
    }

    public void HideBottle()
    {
        bottleHiddenByUpgrade = true;
        if (xpBottle != null)
            xpBottle.SetActive(false);
    }

    public void ShowBottle()
    {
        bottleHiddenByUpgrade = false;
    }

    // Called when level up occurs - keeps bar full until upgrade selected
    public void OnLevelUp()
    {
        isWaitingForUpgrade = true;
        HideBottle();

        // Set the bar to 100% immediately
        redFillXPBar.fillAmount = 1f;
        redFillXPBottle.fillAmount = 1f;
    }

    // Called when upgrade is selected
    public void OnUpgradeSelected()
    {
        isWaitingForUpgrade = false;

        // Update with the new progress (overflow XP)
        if (playerXP != null)
        {
            displayedProgress = playerXP.xpLevel / playerXP.requiredXP;
            displayedProgress = Mathf.Clamp01(displayedProgress);
        }

        ShowBottle();
    }
}