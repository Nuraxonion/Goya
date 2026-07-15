using System.Collections.Generic;
using UnityEngine;
using static UpgradeData;

public class PlayerStats : MonoBehaviour
{
    public Dictionary<string, int> upgrades =
        new Dictionary<string, int>();

    //Attack Duration Stats
    public float fireballDuration = 5f;
    public float waveDuration = 5f;

    //Fireball
    public float fireballDamage = 1f;
    public float fireballRate = 1f;
    public float fireballSpeed = 8f;
    public float fireballCooldown = 1f;

    //Fireball weapon skill
    public float autoAimDamage = 1f;          // damage of auto-aimed projectiles
    public int   fireballPierce = 0;          // enemies a projectile passes through
    public int   autoAimCount = 0;            // extra auto-aimed projectiles per shot
    public float fireballAttackInterval = 1f; // seconds between attacks (base)

    //Wave
    public float waveDamage = 1f;
    public float waveCooldown = 1f;

    //Wave weapon skill
    public float waveAttackInterval = 4f;    // seconds between wave casts (base)
    public float waveRadiusMultiplier = 1f;  // scales wave size / reach
    public bool  waveHasPushback = false;
    public float wavePushbackDistance = 2f;  // units enemies are shoved outward
    public bool  waveDoubleCast = false;     // fire a 2nd wave after a short delay
    public float waveSecondCastDelay = 0.69f;

    //Health
    //public float health = 100f;
    public PlayerHealth playerHealth;

    //Has This Attack?
    public bool hasWaveAttack = false;
    public bool hasLightningAttack = true;



    public void ApplyUpgrade(UpgradeData data)
    {
        if (!upgrades.ContainsKey(data.upgradeID))
            upgrades[data.upgradeID] = 0;

        upgrades[data.upgradeID]++;

        switch (data.type)
        {
            case UpgradeType.MaxHealth:
                playerHealth.IncreaseMaxHealth(data.valueIncrease);
                break;
            case UpgradeType.Heal:
                playerHealth.Heal(data.valueIncrease);
                break;
            case UpgradeType.FireballDamage:
                fireballDamage += data.valueIncrease;
                break;
            case UpgradeType.FireballDuration:
                fireballDuration += data.valueIncrease;
                break;
            case UpgradeType.FireballCooldown:
                fireballCooldown -= data.valueIncrease;
                fireballCooldown = Mathf.Max(0.1f, fireballCooldown);
                break;
            case UpgradeType.Wave:
                hasWaveAttack = true;
                Debug.Log("Wave unlocked");
                break;
            case UpgradeType.WaveDuration:
                waveDuration += data.valueIncrease;
                break;
            case UpgradeType.WaveCooldown:
                waveCooldown -= data.valueIncrease;
                waveCooldown = Mathf.Max(0.1f, waveCooldown);
                break;
            case UpgradeType.WaveDamage:
                waveDamage += data.valueIncrease;
                break;
            case UpgradeType.FireballWeapon:
                ApplyFireballWeaponLevel((int)data.valueIncrease);
                break;
            case UpgradeType.WaveWeapon:
                ApplyWaveWeaponLevel((int)data.valueIncrease);
                break;


        }

        Debug.Log("Applied: " + data.upgradeName);
    }

    // Applies the bespoke effect for a given fireball weapon-skill level (1-8).
    // The level number is carried in the asset's valueIncrease.
    void ApplyFireballWeaponLevel(int level)
    {
        switch (level)
        {
            case 1: autoAimCount = 1; break;                          // +1 auto-aimed projectile
            case 2: fireballDamage *= 2f; break;                      // regular damage +100%
            case 3: fireballPierce = 1; break;                        // pierce through 1 enemy
            case 4: fireballAttackInterval *= 0.75f; break;           // attack interval -25%
            case 5: autoAimCount = 2; break;                          // +1 auto-aimed projectile
            case 6: fireballDamage *= 1.5f; autoAimDamage *= 2f; break; // regular +50%, auto-aim +100%
            case 7: autoAimCount = 3; break;                          // +1 auto-aimed projectile
            case 8: fireballPierce = 2; break;                        // pierce through 2 enemies
        }
    }

    // Applies the bespoke effect for a given wave weapon-skill level (1-8).
    // The level number is carried in the asset's valueIncrease.
    void ApplyWaveWeaponLevel(int level)
    {
        switch (level)
        {
            case 1: waveRadiusMultiplier *= 1.3f; break;                              // radius +30%
            case 2: waveAttackInterval = Mathf.Max(0.5f, waveAttackInterval - 1f); break; // cooldown -1s
            case 3: waveDamage *= 2f; break;                                          // damage +100%
            case 4: waveHasPushback = true; break;                                    // pushback
            case 5: waveRadiusMultiplier *= 1.3f; break;                              // radius +30%
            case 6: waveDoubleCast = true; break;                                     // second wave
            case 7: waveDamage *= 1.5f; break;                                        // damage +50%
            case 8: waveAttackInterval = Mathf.Max(0.5f, waveAttackInterval - 1f); break; // cooldown -1s
        }
    }
}