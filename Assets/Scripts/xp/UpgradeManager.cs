using System.Collections.Generic;
using UnityEngine;

public class UpgradeManager : MonoBehaviour
{
    public List<UpgradeData> allUpgrades;

    public PlayerStats playerStats;

    public UpgradeButton[] buttons;

    public GameObject upgradePanel;

    public void ShowUpgrades()
    {
        List<UpgradeData> available =
            GetAvailableUpgrades();

        for (int i = 0; i < buttons.Length; i++)
        {
            UpgradeData randomUpgrade =
                available[Random.Range(0, available.Count)];

            buttons[i].Setup(randomUpgrade, this);

            available.Remove(randomUpgrade);

            upgradePanel.SetActive(true);
        }
    }

    List<UpgradeData> GetAvailableUpgrades()
    {
        List<UpgradeData> list =
            new List<UpgradeData>();

        foreach (var upg in allUpgrades)
        {
            int currentLevel = 0;

            playerStats.upgrades.TryGetValue(
                upg,
                out currentLevel
            );

            if (currentLevel < upg.maxLevel)
            {
                list.Add(upg);
            }
        }

        return list;
    }

    public void SelectUpgrade(UpgradeData data)
    {
        playerStats.ApplyUpgrade(data);

        Time.timeScale = 1f;

        upgradePanel.SetActive(false);
    }
}