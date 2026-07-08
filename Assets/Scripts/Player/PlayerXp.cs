using UnityEngine;

public class PlayerXP : MonoBehaviour
{

    public int playerLevel = 1;

    public float xpLevel = 0;
    public float xpTotal = 0;
    public float requiredXP = 10;

    public UpgradeManager upgradeManager;

    // Coins earned at run end = xpTotal * coinsPerXP (floored).
    // Tune this in the Inspector to balance how fast coins accumulate.
    public float coinsPerXP = 0.1f;

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
    // Converts the run's total XP into persistent coins and banks them.
    // Returns the number of coins earned this run.
    public int EndRunAndAddCoins()
    {
        int coinsEarned = Mathf.FloorToInt(xpTotal * coinsPerXP);
        CoinBank.AddCoins(coinsEarned);
        Debug.Log("Run ended. Coins earned: " + coinsEarned + ". Total coins: " + CoinBank.GetCoins());
        return coinsEarned;
    }
}