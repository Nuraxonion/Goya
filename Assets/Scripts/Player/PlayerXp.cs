using System.Collections;
using UnityEngine;

public class PlayerXP : MonoBehaviour
{
    public int playerLevel = 1;

    public float xpLevel = 0;
    public float xpTotal = 0;
    public float requiredXP = 20;

    public UpgradeManager upgradeManager;

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

        Debug.Log($"XP: {xpLevel} / Required: {requiredXP}");

        if (xpLevel >= requiredXP && !upgradeReady)
        {
            upgradeReady = true;
            Debug.Log("✅ Upgrade ready! Click the bottle!");
        }
    }

    // Returns true only if the level up actually went ahead, so the caller knows
    // whether it is safe to hide the bottle - a refused level up must leave it
    // visible and clickable.
    public bool TriggerLevelUp()
    {
        if (!upgradeReady || isLevelingUp)
            return false;

        if (xpLevel < requiredXP)
            return false;

        Debug.Log("🔥 Triggering level up!");
        LevelUp();

        return true;
    }

    void LevelUp()
    {
        isLevelingUp = true;
        upgradeReady = false;

        float overflowXP = xpLevel - requiredXP;

        playerLevel++;
        requiredXP = Mathf.Round(requiredXP * 1.25f);
        xpLevel = overflowXP;

        if (xpLevel < 0)
            xpLevel = 0;

        Debug.Log($"⬆️ Level {playerLevel}! Next needs {requiredXP} XP. Overflow: {xpLevel}");

        if (inkXPUI != null)
            inkXPUI.OnLevelUp();

        Time.timeScale = 0f;
        StartCoroutine(LevelUpSequence());
    }

    IEnumerator LevelUpSequence()
    {
        yield return new WaitForSecondsRealtime(0.4f);
        upgradeManager.ShowUpgrades();
    }

    // Called after ONE upgrade is selected
    public void CompleteLevelUp()
    {
        isLevelingUp = false;

        // Check if we have enough XP for ANOTHER level
        if (xpLevel >= requiredXP)
        {
            upgradeReady = true;
            Debug.Log($"🔄 Another level available! ({xpLevel} >= {requiredXP})");

            // Trigger the next level up (panel will refresh with new choices)
            LevelUp();
        }
        else
        {
            upgradeReady = false;
            Debug.Log($"❌ No more levels. ({xpLevel} < {requiredXP}) - Closing panel.");

            upgradeManager.CloseUpgradePanel();
        }
    }

    public bool IsLevelingUp()
    {
        return isLevelingUp;
    }

    public bool IsUpgradeReady()
    {
        return upgradeReady;
    }

    public int EndRunAndAddCoins()
    {
        int coinsEarned = Mathf.FloorToInt(xpTotal * coinsPerXP);
        CoinBank.AddCoins(coinsEarned);
        Debug.Log($"Run ended. Coins earned: {coinsEarned}");
        return coinsEarned;
    }
}