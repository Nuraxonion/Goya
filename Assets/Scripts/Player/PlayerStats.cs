using System.Collections.Generic;
using UnityEngine;
using static UpgradeData;

public class PlayerStats : MonoBehaviour
{
    public Dictionary<UpgradeData, int> upgrades =
        new Dictionary<UpgradeData, int>();

    //Fireball
    public float fireballDamage = 1f;
    public float fireballRate = 1f;
    public float fireballSpeed = 8f;

    //Wave
    public float waveDamage = 1f;

    //Health
    public float health = 100f;

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
                fireballDamage += data.valueIncrease;
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