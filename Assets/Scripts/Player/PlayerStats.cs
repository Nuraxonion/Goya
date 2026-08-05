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

    // INERT: PlayerAttack resets its timer from fireballAttackInterval, not this.
    // Kept only so existing serialized data doesn't break. Tune fireballAttackInterval instead.
    [HideInInspector] public float fireballCooldown = 1f;

    //Fireball weapon skill
    public float autoAimDamage = 1f;
    public int fireballPierce = 0;
    public int autoAimCount = 0;
    public float fireballAttackInterval = 1f;

    //Fireball Level
    public int fireballLevel = 0;

    //Wave
    public float waveDamage = 1f;

    // INERT: PlayerAttack resets its timer from waveAttackInterval, not this.
    [HideInInspector] public float waveCooldown = 1f;

    //Wave weapon skill
    public float waveAttackInterval = 4f;
    public float waveRadiusMultiplier = 1f;
    public bool waveHasPushback = false;
    public float wavePushbackDistance = 2f;
    public bool waveDoubleCast = false;
    public float waveSecondCastDelay = 0.69f;

    //Wave Level
    public int waveLevel = 0;

    //Lightning
    public float lightningDamage = 20f;
    public float lightningCooldown = 8f;
    public float lightningRange = 10f;

    //Lightning Level
    public int lightningLevel = 0;

    //Health
    public PlayerHealth playerHealth;

    //Has This Attack?
    public bool hasWaveAttack = false;
    public bool hasLightningAttack = false;

    // Single source of truth for "can this attack actually be cast right now".
    // GestureManager asks before accepting a gesture, so a locked attack reports
    // back to the player instead of showing a rank and then quietly doing nothing.
    public bool IsAttackAvailable(string attackId)
    {
        switch (attackId)
        {
            case AttackIds.Fireball:
                return true;

            case AttackIds.Wave:
                return hasWaveAttack;

            case AttackIds.Lightning:
                // Deliberately false regardless of hasLightningAttack: the branch in
                // PlayerAttack.Update only logs, and LightningAttack.Cast() has no
                // callers. Flip this once the attack is actually wired up.
                return false;

            default:
                return false;
        }
    }

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
            case UpgradeType.FireballLevel:
                fireballLevel++;
                Debug.Log($"🔥 Fireball level increased to: {fireballLevel}");
                ApplyFireballLevelBonuses(fireballLevel);
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
            case UpgradeType.WaveLevel:
                waveLevel++;
                Debug.Log($"🌊 Wave level increased to: {waveLevel}");
                ApplyWaveLevelBonuses(waveLevel);
                break;
            case UpgradeType.LightningLevel:
                lightningLevel++;
                ApplyLightningLevelBonuses(lightningLevel);
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

    void ApplyFireballWeaponLevel(int level)
    {
        fireballLevel = level;

        switch (level)
        {
            case 1: autoAimCount = 1; break;
            case 2: fireballDamage *= 2f; break;
            case 3: fireballPierce = 1; break;
            case 4: fireballAttackInterval *= 0.75f; break;
            case 5: autoAimCount = 2; break;
            case 6: fireballDamage *= 1.5f; autoAimDamage *= 2f; break;
            case 7: autoAimCount = 3; break;
            case 8: fireballPierce = 2; break;
        }
    }

    void ApplyWaveWeaponLevel(int level)
    {
        waveLevel = level;

        switch (level)
        {
            case 1: waveRadiusMultiplier *= 1.3f; break;
            case 2: waveAttackInterval = Mathf.Max(0.5f, waveAttackInterval - 1f); break;
            case 3: waveDamage *= 2f; break;
            case 4: waveHasPushback = true; break;
            case 5: waveRadiusMultiplier *= 1.3f; break;
            case 6: waveDoubleCast = true; break;
            case 7: waveDamage *= 1.5f; break;
            case 8: waveAttackInterval = Mathf.Max(0.5f, waveAttackInterval - 1f); break;
        }
    }

    void ApplyFireballLevelBonuses(int level)
    {
        switch (level)
        {
            case 1:
                break;
            case 2:
                fireballDamage *= 1.5f;
                break;
            case 3:
                fireballAttackInterval *= 0.8f;
                break;
            case 4:
                fireballDamage *= 1.5f;
                fireballPierce = 1;
                break;
            case 5:
                fireballSpeed *= 1.3f;
                break;
            case 6:
                fireballDamage *= 1.5f;
                fireballAttackInterval *= 0.8f;
                break;
            case 7:
                fireballPierce = 2;
                autoAimCount = 1;
                break;
            case 8:
                fireballDamage *= 2f;
                fireballAttackInterval *= 0.7f;
                fireballPierce = 3;
                autoAimCount = 2;
                break;
        }
    }

    void ApplyWaveLevelBonuses(int level)
    {
        switch (level)
        {
            case 1:
                break;
            case 2:
                waveDamage *= 1.5f;
                break;
            case 3:
                waveRadiusMultiplier *= 1.3f;
                break;
            case 4:
                waveDamage *= 1.5f;
                waveCooldown *= 0.8f;
                break;
            case 5:
                waveRadiusMultiplier *= 1.3f;
                break;
            case 6:
                waveDamage *= 1.5f;
                waveHasPushback = true;
                wavePushbackDistance = 2f;
                break;
            case 7:
                waveCooldown *= 0.8f;
                waveRadiusMultiplier *= 1.3f;
                break;
            case 8:
                waveDamage *= 2f;
                waveCooldown *= 0.7f;
                waveDoubleCast = true;
                waveSecondCastDelay = 0.3f;
                break;
        }
    }

    void ApplyLightningLevelBonuses(int level)
    {
        switch (level)
        {
            case 1:
                hasLightningAttack = true;
                lightningDamage = 20f;
                lightningCooldown = 8f;
                lightningRange = 10f;
                break;
            case 2:
                lightningDamage *= 1.5f;
                break;
            case 3:
                lightningCooldown *= 0.8f;
                break;
            case 4:
                lightningDamage *= 1.5f;
                lightningRange *= 1.3f;
                break;
            case 5:
                lightningCooldown *= 0.8f;
                break;
            case 6:
                lightningDamage *= 1.5f;
                lightningRange *= 1.3f;
                break;
            case 7:
                lightningCooldown *= 0.8f;
                break;
            case 8:
                lightningDamage *= 2f;
                lightningCooldown *= 0.7f;
                lightningRange *= 1.5f;
                break;
        }
    }
}