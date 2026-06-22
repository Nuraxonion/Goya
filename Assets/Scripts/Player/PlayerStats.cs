using System.Collections.Generic;
using UnityEngine;
using static UpgradeData;

public class PlayerStats : MonoBehaviour
{
    public Dictionary<string, int> upgrades =
        new Dictionary<string, int>(); 

    

    //Fireball
    public float fireballDamage = 1f;
    public float fireballRate = 1f;
    public float fireballSpeed = 8f;
    public float fireballCooldown = 1f;

    //Wave
    public float waveDamage = 1f;
    public float waveCooldown = 1f;

    //Health
    //public float health = 100f;
    public PlayerHealth playerHealth;

    //Has This Attack?
    public bool hasWaveAttack = false;
    public bool hasLightningAttack = false;



    public void ApplyUpgrade(UpgradeData data)
    {
        if (!upgrades.ContainsKey(data.upgradeID))
            upgrades[data.upgradeID] = 0;

        upgrades[data.upgradeID]++;

        switch (data.type)
        {
            case UpgradeType.MaxHealth:
                //health += data.valueIncrease;
                playerHealth.IncreaseMaxHealth(data.valueIncrease);
                break;
            case UpgradeType.Wave:
                hasWaveAttack = true;

                Debug.Log("Wave unlocked");
                break;
            case UpgradeType.FireballDamage:
                fireballDamage += data.valueIncrease;
                break;
            case UpgradeType.FireballCooldown:
                fireballCooldown -= data.valueIncrease;
                fireballCooldown = Mathf.Max(0.1f, fireballCooldown);
                break;
            case UpgradeType.WaveCooldown:
                waveCooldown -= data.valueIncrease;
                waveCooldown = Mathf.Max(0.1f, waveCooldown);
                break;
            case UpgradeType.WaveDamage:
                waveDamage += data.valueIncrease;
                break;

        }

        Debug.Log("Applied: " + data.upgradeName);
    }
}