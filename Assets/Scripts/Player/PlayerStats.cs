using System.Collections.Generic;
using UnityEngine;
using static UpgradeData;

public class PlayerStats : MonoBehaviour
{
    public Dictionary<UpgradeData, int> upgrades =
        new Dictionary<UpgradeData, int>();

    public float damage = 1;
    public float moveSpeed = 5;

    public void ApplyUpgrade(UpgradeData data)
    {
        if (!upgrades.ContainsKey(data))
            upgrades[data] = 0;

        upgrades[data]++;

        switch (data.type)
        {
            case UpgradeType.Damage:
                damage += data.valueIncrease;
                break;

            case UpgradeType.Speed:
                moveSpeed += data.valueIncrease;
                break;
        }

        Debug.Log("Applied: " + data.upgradeName);
    }
}