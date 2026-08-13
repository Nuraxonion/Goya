using System.Collections;
using UnityEngine;

public class PlayerXP : MonoBehaviour
{
    public int playerLevel = 1;

    public float xpLevel = 0;
    public float xpTotal = 0;
    public float requiredXP = 20;

    public UpgradeManager upgradeManager;

    // Coins earned at run end = xpTotal * coinsPerXP (floored).
    public float coinsPerXP = 0.1f;

    private bool isLevelingUp = false;
    private bool upgradeReady = false;

    public InkXPUI inkXPUI;

    void Start()
    {
        if (inkXPUI == null)
            inkXPUI = FindObjectOfType<InkXPUI>();
    }

    public void AddXP(float amount)
    {
        if (isLevelingUp)
            return;

        xpLevel += amount;
        xpTotal += amount;

        Debug.Log("Required XP for this level: " + requiredXP);
        Debug.Log("Player Level XP: " + xpLevel);
        Debug.Log("Total Player XP: " + xpTotal);
        Debug.Log("Current Player Level: " + playerLevel);

        // ONLY mark upgrade as ready - DO NOT auto level up
        if (xpLevel >= requiredXP && !upgradeReady)
        {
            upgradeReady = true;
            Debug.Log("UPGRADE READY! Click the bottle to level up!");
        }
    }

    // Called when bottle is clicked
    public void TriggerLevelUp()
    {
        if (isLevelingUp)
            return;

        if (!upgradeReady)
        {
            Debug.Log("Not ready to level up yet. Keep collecting XP.");
            return;
        }

        if (xpLevel < requiredXP)
        {
            Debug.Log("Not enough XP for level up. Required: " + requiredXP + ", Current: " + xpLevel);
            return;
        }

        Debug.Log("Bottle clicked! Triggering level up!");
        LevelUp();
    }

    void LevelUp()
    {
        isLevelingUp = true;
        upgradeReady = false;

        // Store the overflow XP
        float overflowXP = xpLevel - requiredXP;

        playerLevel++;

        requiredXP *= 1.25f;

        // Apply overflow XP to new level
        xpLevel = overflowXP;

        Debug.Log("Level Up to level " + playerLevel);
        Debug.Log("New required XP: " + requiredXP);
        Debug.Log("Overflow XP carried over: " + xpLevel);

        if (inkXPUI != null)
        {
            inkXPUI.OnLevelUp();
        }

        Time.timeScale = 0f;

        StartCoroutine(LevelUpSequence());
    }

    IEnumerator LevelUpSequence()
    {
        // Wait while the bottle disappears before showing upgrades
        yield return new WaitForSecondsRealtime(0.4f);

        upgradeManager.ShowUpgrades();
    }

    // Called after the player selects an upgrade
    public void ResetXP()
    {
        isLevelingUp = false;

        // Check if we have enough XP for ANOTHER level
        if (xpLevel >= requiredXP)
        {
            upgradeReady = true;
            Debug.Log("More XP available for another level! Click the bottle again.");
            // Panel will close, bottle will reappear with glare
        }
        else
        {
            upgradeReady = false;
            Debug.Log("No more XP for levels.");
        }

        // Close the panel after upgrade
        upgradeManager.CloseUpgradePanel();
    }

    public bool IsLevelingUp()
    {
        return isLevelingUp;
    }

    public bool IsUpgradeReady()
    {
        return upgradeReady;
    }

    public int GetLevelPoints()
    {
        // Calculate how many level ups you can afford with current XP
        int points = 0;
        float tempXP = xpLevel;
        float tempRequired = requiredXP;

        while (tempXP >= tempRequired)
        {
            tempXP -= tempRequired;
            tempRequired *= 1.25f;
            points++;
        }

        return points;
    }

    // Call this when the run ends
    public int EndRunAndAddCoins()
    {
        int coinsEarned = Mathf.FloorToInt(xpTotal * coinsPerXP);
        CoinBank.AddCoins(coinsEarned);

        Debug.Log("Run ended. Coins earned: " + coinsEarned +
                  ". Total coins: " + CoinBank.GetCoins());

        return coinsEarned;
    }
}