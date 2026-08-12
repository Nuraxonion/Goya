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

    // XP collected while the upgrade panel was open, applied once it closes.
    private float pendingXP = 0f;

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
        if (amount <= 0f)
            return;

        // Held rather than dropped: the Spiral attack collects a whole field of
        // orbs at once, and the ones arriving while the upgrade panel is open used
        // to be destroyed for nothing.
        if (isLevelingUp)
        {
            pendingXP += amount;
            return;
        }

        xpLevel += amount;
        xpTotal += amount;

        // Deliberately not logged: this runs once per orb, and a Spiral collect
        // pays in a whole field of them at once.

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

        // Overflow past the next level is deliberately kept: a single large
        // payment (a Spiral collect) can grant several levels, one upgrade panel
        // each. ResetXPAfterUpgrade re-checks the threshold once this panel closes.

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

        StartCoroutine(ApplyPendingXP());
    }

    // Deferred by a frame: UpgradeManager.SelectUpgrade calls ResetXPAfterUpgrade
    // before InkXPUI.OnUpgradeSelected(), so leveling up again right here would run
    // OnLevelUp before OnUpgradeSelected and leave the XP bottle inconsistent.
    IEnumerator ApplyPendingXP()
    {
        // Unaffected by timeScale, so it still runs if another panel is queued.
        yield return null;

        float carried = pendingXP;
        pendingXP = 0f;

        if (carried > 0f)
        {
            AddXP(carried);
        }
        else if (xpLevel >= requiredXP)
        {
            // Overflow from the level just gained is already enough for the next.
            LevelUp();
        }
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