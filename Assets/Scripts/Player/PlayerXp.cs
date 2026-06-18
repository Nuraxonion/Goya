using UnityEngine;

public class PlayerXP : MonoBehaviour
{

    public int playerLevel = 1;

    public float xpLevel = 0;
    public float xpTotal = 0;
    public float requiredXP = 10;

    public UpgradeManager upgradeManager;

    public void AddXP(float amount)
    {
        xpLevel += amount;
        xpTotal += amount;
        Debug.Log("Required XP for this level: " + requiredXP);
        Debug.Log("Player Level XP: " + xpLevel);
        Debug.Log("Total Player XP: " + xpTotal);

        while (xpLevel >= requiredXP)
        {
            LevelUp();
        }
    }

    void LevelUp()
    {
        xpLevel -= requiredXP;
        playerLevel++;

        requiredXP *= 1.25f;

        Debug.Log("Level Up to level " + playerLevel);

        upgradeManager.ShowUpgrades();

        Time.timeScale = 0f;
    }

    // Call this when the run ends (game over, level complete, etc.)
    // Adds 1/10th of total XP to meta XP
    public void EndRunAndAddMetaXP()
    {
        float metaXPGain = xpTotal * 0.1f; // 1/10th of xpTotal
        MetaXPManager.instance.AddMetaXP(metaXPGain);
        Debug.Log("Run ended. Meta XP gained: " + metaXPGain);
    }
}