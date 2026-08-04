using System.Collections;
using UnityEngine;

public class PlayerXP : MonoBehaviour
{
    public int playerLevel = 1;

    public float xpLevel = 0;
    public float xpTotal = 0;
    public float requiredXP = 20;

    [Header("XP Curve")]
    [Tooltip("XP needed for the first level up. Applied at Start(), overriding any stale scene value.")]
    public float startingRequiredXP = 40f;

    [Tooltip("Each level costs this much more than the last.")]
    public float xpGrowthRate = 1.15f;

    public UpgradeManager upgradeManager;

    // Coins earned at run end = xpTotal * coinsPerXP (floored).
    public float coinsPerXP = 0.1f;

    private bool isLevelingUp = false;

    public InkXPUI inkXPUI;

    void Start()
    {
        requiredXP = startingRequiredXP;

        // Find XP UI if not assigned
        if (inkXPUI == null)
            inkXPUI = FindObjectOfType<InkXPUI>();
    }

    public void AddXP(float amount)
    {
        if (isLevelingUp)
            return;

        xpLevel += amount;
        xpTotal += amount;

        Debug.Log($"Required XP for this level: {requiredXP}");
        Debug.Log($"Player Level XP: {xpLevel}");
        Debug.Log($"Total Player XP: {xpTotal}");

        if (xpLevel >= requiredXP)
        {
            LevelUp();
        }
    }

    void LevelUp()
    {
        isLevelingUp = true;

        // Store the overflow XP
        float overflowXP = xpLevel - requiredXP;

        playerLevel++;

        requiredXP = Mathf.Round(requiredXP * xpGrowthRate);

        // Apply overflow XP to new level
        xpLevel = overflowXP;

        // Ensure we don't have negative XP
        if (xpLevel < 0)
            xpLevel = 0;

        // If overflow XP somehow exceeds the new required XP, cap it
        if (xpLevel >= requiredXP)
        {
            xpLevel = requiredXP - 1;
        }

        Debug.Log($"Level Up to level {playerLevel}. Next level requires {requiredXP} XP. Current XP: {xpLevel}");

        // Tell the UI to show full bar and hide bottle
        if (inkXPUI != null)
        {
            inkXPUI.OnLevelUp();
        }

        Time.timeScale = 0f;

        StartCoroutine(LevelUpSequence());
    }

    IEnumerator LevelUpSequence()
    {
        // Wait a moment before showing upgrades
        yield return new WaitForSecondsRealtime(0.4f);

        upgradeManager.ShowUpgrades();
    }

    // Called after the player selects an upgrade
    public void ResetXPAfterUpgrade()
    {
        isLevelingUp = false;
        // xpLevel is already set from LevelUp()
        // This just allows XP collection to resume
    }

    public bool IsLevelingUp()
    {
        return isLevelingUp;
    }

    // Call this when the run ends
    public int EndRunAndAddCoins()
    {
        int coinsEarned = Mathf.FloorToInt(xpTotal * coinsPerXP);
        CoinBank.AddCoins(coinsEarned);

        Debug.Log($"Run ended. Coins earned: {coinsEarned}. Total coins: {CoinBank.GetCoins()}");

        return coinsEarned;
    }
}