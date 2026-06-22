using System.Collections.Generic;
using UnityEngine;

public class UpgradeManager : MonoBehaviour
{
    public List<UpgradeData> allUpgrades;

    public List<string> ownedUpgrades = new List<string>(); 

    public PlayerStats playerStats;

    public UpgradeButton[] buttons;

    public GameObject upgradePanel;

    public bool isGameRunning = true;

    public void ShowUpgrades()
    {
            
        upgradePanel.SetActive(true);
        isGameRunning = false;

            
        List<UpgradeData> available =
            GetAvailableUpgrades();

        for (int i = 0; i < buttons.Length; i++)
        {
            if (available.Count <= 0)
                break;

            UpgradeData randomUpgrade = GetWeightedRandomUpgrade(available);

            buttons[i].Setup(randomUpgrade, this);

            available.Remove(randomUpgrade);
        }
    }

    List<UpgradeData> GetAvailableUpgrades()
    {
        List<UpgradeData> list =
            new List<UpgradeData>();

        foreach (var upg in allUpgrades)
        {
            if (upg.oneTimeUpgrade &&
            playerStats.upgrades.ContainsKey(upg.upgradeID))
            {
                continue;
            }

            int currentLevel = 0;

            playerStats.upgrades.TryGetValue(
                upg.upgradeID,
                out currentLevel
            );

            if (currentLevel < upg.maxLevel &&
                !list.Contains(upg) &&
                CanAppear(upg))
            {
                list.Add(upg);
            }
        }

        return list;
    }

    // New added (20.06)
    bool CanAppear(UpgradeData upgrade)
    {
        if (!upgrade.requiresUnlock)
            return true;

        return ownedUpgrades.Contains(upgrade.requiredUpgradeID);
    }

    // New added (20.06)
    UpgradeData GetWeightedRandomUpgrade(List<UpgradeData> pool)
    {
        int totalWeight = 0;

        foreach (var upg in pool)
        {
            totalWeight += upg.weight;
        }

        int randomValue = Random.Range(0, totalWeight);

        foreach (var upg in pool)
        {
            randomValue -= upg.weight;

            if (randomValue < 0)
                return upg;
        }

        return pool[0];
    }

    public void SelectUpgrade(UpgradeData data)
    {
        playerStats.ApplyUpgrade(data);

        if (!ownedUpgrades.Contains(data.upgradeID))
        {
            ownedUpgrades.Add(data.upgradeID);
        }

        Time.timeScale = 1f;
        upgradePanel.SetActive(false);
    }
}