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

    // Flat damage bought in the Art Shop. Seeded in Awake from ApplyMetaDamageUpgrades.
    //
    // Held apart from fireballDamage/waveDamage/lightningDamage on purpose: the weapon
    // level chains multiply those in place (fireballDamage *= 2f and friends), so folding
    // the meta bonus in would let a 6.75x chain compound a +3 upgrade into +20. These are
    // added in at the cast sites instead, just before the gesture accuracy multiplier, so
    // the bonus still scales with accuracy like any other damage.
    public float fireballBonusDamage = 0f;
    public float waveBonusDamage = 0f;
    public float lightningBonusDamage = 0f;

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

    // Lightning
    [Header("Lightning Attack")]

    // Baseline lives in Assets/Resources/LightningConfig.asset - tune it there. Leave this
    // slot empty and Awake loads that asset by name; assign a different config to override.
    [SerializeField] private LightningConfig lightningConfig;

    // Runtime values. Seeded from lightningConfig in Awake, then mutated in place by
    // ApplyLightningWeaponLevel as the player levels the attack up.
    public float lightningDamage = 50f;
    public float lightningRadius = 1f;
    public float lightningCastSpeed = 1f;
    public float lightningStunDuration = 2f;
    public float lightningDuration = 5f;

    // Grows only the cursor-aimed blast. The extra strikes below deliberately stay at
    // the un-upgraded lightningRadius, so this is a separate multiplier rather than a
    // change to lightningRadius itself. Upgrade-driven only - LightningConfig does not
    // seed it, same as waveRadiusMultiplier.
    public float lightningAimedRadiusMultiplier = 1f;

    // Extra blasts dropped at random spots near the player on every cast, on top of the
    // aimed one. Full damage and full stun, at base radius.
    public int lightningExtraStrikes = 0;

    // Final upgrade: every enemy the aimed blast hits that was ALREADY stunned zaps the
    // nearest un-stunned enemy for lightningChainDamage.
    public bool lightningChainFromStunned = false;
    public float lightningChainDamage = 1f;

    //Lightning Level
    public int lightningLevel = 0;

    //Spiral - pulls every XP orb in the level to the player.
    // Named *AttackInterval like the fireball/wave cadence fields, since the
    // *Cooldown fields above are inert and drive nothing.
    public float spiralAttackInterval = 5f;
    public float spiralCollectSpeed = 12f;

    //Health
    public PlayerHealth playerHealth;

    //Has This Attack?
    // Every attack is gated behind an unlock upgrade, the fireball included - it
    // used to be the hardcoded baseline.
    public bool hasFireballAttack = false;
    public bool hasWaveAttack = false;
    public bool hasLightningAttack = false;
    public bool hasSpiralAttack = false;

    // Multi-Tasking: casting a new attack no longer cancels the one already
    // running - each stays active until its own duration timer expires.
    public bool hasMultiTasking = false;

    // OP Multi-Tasking: casting any attack also refreshes every attack already
    // running back to full duration, so alternating gestures keeps them alive.
    public bool hasOpMultiTasking = false;

    // Seeds the lightning stats from the config asset before anything else runs - upgrades
    // apply later and multiply on top of these, so the baseline has to land first.
    void Awake()
    {
        if (lightningConfig == null)
            lightningConfig = Resources.Load<LightningConfig>("LightningConfig");

        if (lightningConfig != null)
            lightningConfig.ApplyTo(this);

        ApplyMetaDurationUpgrades();
        ApplyMetaDamageUpgrades();
    }

    // Flat seconds bought in the Art Shop, added on top of the authored baselines.
    //
    // Has to run after ApplyTo above: that assigns lightningDuration outright, so a bonus
    // written any earlier would be wiped. The in-run duration upgrades are flat += on these
    // same fields and land later still, so meta and in-run bonuses simply sum.
    void ApplyMetaDurationUpgrades()
    {
        fireballDuration += MetaUpgrades.GetTotalValue(MetaUpgradeIds.FireballDuration);
        waveDuration += MetaUpgrades.GetTotalValue(MetaUpgradeIds.WaveDuration);
        lightningDuration += MetaUpgrades.GetTotalValue(MetaUpgradeIds.LightningDuration);
    }

    // Assignment rather than +=, so a stale serialized value in the scene cannot
    // accumulate across edits. Nothing else writes these three fields.
    void ApplyMetaDamageUpgrades()
    {
        fireballBonusDamage = MetaUpgrades.GetTotalValue(MetaUpgradeIds.FireballDamage);
        waveBonusDamage = MetaUpgrades.GetTotalValue(MetaUpgradeIds.WaveDamage);
        lightningBonusDamage = MetaUpgrades.GetTotalValue(MetaUpgradeIds.LightningDamage);
    }

    // Single source of truth for "can this attack actually be cast right now".
    // GestureManager asks before accepting a gesture, so a locked attack reports
    // back to the player instead of showing a rank and then quietly doing nothing.
    public bool IsAttackAvailable(string attackId)
    {
        switch (attackId)
        {
            case AttackIds.Fireball:
                return hasFireballAttack;

            case AttackIds.Wave:
                return hasWaveAttack;

            case AttackIds.Spiral:
                return hasSpiralAttack;

            case AttackIds.Lightning:
                return hasLightningAttack;

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
            case UpgradeType.Fireball:
                hasFireballAttack = true;

                // The bubble UI shows fireball pips as (fireballLevel - 1), a
                // leftover from when the fireball was implicitly already level 1.
                // Setting it here makes that subtraction literally correct.
                fireballLevel = 1;

                Debug.Log("Fireball unlocked");
                break;
            case UpgradeType.Wave:
                hasWaveAttack = true;
                Debug.Log("Wave unlocked");
                break;
            case UpgradeType.Spiral:
                hasSpiralAttack = true;
                Debug.Log("Spiral unlocked");
                break;
            case UpgradeType.MultiTasking:
                hasMultiTasking = true;
                Debug.Log("Multi-Tasking unlocked");
                break;
            case UpgradeType.OpMultiTasking:
                hasOpMultiTasking = true;
                Debug.Log("OP Multi-Tasking unlocked");
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
            // Older scaffolding, kept working because the enum value cannot be removed
            // without repointing every serialized upgrade. No live asset uses it.
            case UpgradeType.LightningLevel:
                ApplyLightningWeaponLevel(lightningLevel + 1);
                break;
            case UpgradeType.Lightning:
                hasLightningAttack = true;

                // Same convention as the fireball unlock: the attack is level 1 the
                // moment it is unlocked, so the skill chain reads 2..8 on the pips.
                lightningLevel = 1;

                Debug.Log("Lightning unlocked");
                break;
            case UpgradeType.LightningWeapon:
                ApplyLightningWeaponLevel((int)data.valueIncrease);
                break;
            case UpgradeType.FireballWeapon:
                ApplyFireballWeaponLevel((int)data.valueIncrease);
                break;
            case UpgradeType.WaveWeapon:
                ApplyWaveWeaponLevel((int)data.valueIncrease);
                break;
            // Banked immediately rather than at run end, so these coins survive
            // even if the player dies before finishing the run.
            case UpgradeType.MetaXP:
                CoinBank.AddCoins((int)data.valueIncrease);
                break;
        }

        Debug.Log("Applied: " + data.upgradeName);
    }

    void ApplyFireballWeaponLevel(int level)
    {
        // Increment instead of set, so Fireball_Skill_1 turns Pip1 red
        fireballLevel++;

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
        // Increment instead of set
        waveLevel++;

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

    // The lightning chain. The unlock (UpgradeType.Lightning) is step 0, so the seven
    // Lightning_Skill_N assets pass 1..7 here via valueIncrease - matching
    // ApplyWaveWeaponLevel, where the asset carries the step number rather than a delta.
    void ApplyLightningWeaponLevel(int level)
    {
        // Incremented rather than assigned, so a HUD pip display can read the count
        // directly the way the fireball and wave bubbles do.
        lightningLevel++;

        switch (level)
        {
            // Lightning unlocks as a pure stun (LightningConfig.baseDamage is 0), so the
            // first two steps are what put a damage number on it at all: 0 -> 1 -> 2.
            case 1:
                lightningDamage += 1f;
                break;

            case 2:
                lightningStunDuration += 1.5f;
                break;

            case 3:
                lightningExtraStrikes = 1;
                break;

            // Only the aimed blast grows - the extra strikes stay at base radius.
            case 4:
                lightningAimedRadiusMultiplier *= 1.3f;
                break;

            case 5:
                lightningCastSpeed *= 0.8f;
                break;

            case 6:
                lightningDamage += 1f;
                break;

            case 7:
                lightningExtraStrikes = 4;
                break;

            case 8:
                lightningChainFromStunned = true;
                break;
        }

        Debug.Log($"Lightning level increased to: {lightningLevel}");
    }
}
