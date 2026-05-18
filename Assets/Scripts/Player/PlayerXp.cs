using UnityEngine;

public class PlayerXP : MonoBehaviour
{

    public int level = 1;

    public float xpTotal = 0;
    public float requiredXP = 10;

    public UpgradeManager upgradeManager;

    public void AddXP(float amount)
    {
        xpTotal += amount;
        Debug.Log("Total XP: " + xpTotal);

        while (xpTotal >= requiredXP)
        {
            LevelUp();
        }
    }

    void LevelUp()
    {
        xpTotal -= requiredXP;
        level++;

        requiredXP *= 1.25f;

        Debug.Log("LEVEL UP");

        upgradeManager.ShowUpgrades();

        Time.timeScale = 0f;
    }
}
