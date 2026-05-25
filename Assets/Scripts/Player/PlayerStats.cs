using System.Collections.Generic;
using UnityEngine;
using static UpgradeData;

public class PlayerStats : MonoBehaviour
{
    public Dictionary<UpgradeData, int> upgrades =
        new Dictionary<UpgradeData, int>();

    public float damage = 1;
    public float health = 100;

    public bool hasWaveAttack = false;



    public void ApplyUpgrade(UpgradeData data)
    {
        if (!upgrades.ContainsKey(data))
            upgrades[data] = 0;

        upgrades[data]++;

        switch (data.type)
        {
            case UpgradeType.MaxHealth:
                health += data.valueIncrease;
                break;
            case UpgradeType.Wave:
                hasWaveAttack = true;

                Debug.Log("Wave unlocked");
                break;
            case UpgradeType.Damage:
                damage += data.valueIncrease;
                break;
        }

        Debug.Log("Applied: " + data.upgradeName);
        if (data.upgradeName == "Wave")
        {
            Debug.Log("Wave is possible");
            //isWaveAvailable = true;
        }
    }
}