using System.Collections;
using UnityEngine;

public class PlayerXP : MonoBehaviour
{
    public int playerLevel = 1;

    public float xpLevel = 0;
    public float xpTotal = 0;
    public float requiredXP = 20;

    public UpgradeManager upgradeManager;

    public float coinsPerXP = 0.1f;

    [Header("Level Curve")]
    [Tooltip("X = current level, Y = XP needed to reach the next level. Drag the graph to reshape levelling pace.")]
    public AnimationCurve xpRequiredPerLevel;

    [Tooltip("Scales the whole curve. Tune this first if levelling feels globally too fast or slow - it shifts pacing without reshaping the graph. Raise it if players collect more XP orbs than the curve assumes.")]
    public float xpRequiredMultiplier = 1f;

    [Tooltip("Compounding per level past the last authored key, so levelling keeps costing more in endless mode instead of flatlining at the last value.")]
    public float endlessGrowthPerLevel = 0.15f;

    [Tooltip("XP granted at spawn. With level 1 costing 1 XP this makes the first upgrade immediately available, which is how the player unlocks their first attack.")]
    public float startingXP = 1f;

    [Tooltip("Cost of the very first level up, overriding the curve. Keep this at or below Starting XP - the tutorial soft-locks if the player can't afford the first upgrade, because the run clock and enemy spawning only start once the fireball is taken.")]
    public float firstLevelXP = 1f;

    private bool isLevelingUp = false;
    private bool upgradeReady = false;

    // Set from the XP Gain meta upgrade bought in the Art Shop.
    private float xpGainMultiplier = 1f;

    public InkXPUI inkXPUI;

    void Awake()
    {
        EnsureCurves();
        LoadXpGainMultiplier();
    }

    // Additive rather than compounding: six levels of 0.10 give 1.60x. Both the
    // per level value and the level itself come from the shared upgrade set, so
    // there is no second copy of the number here to fall out of sync.
    //
    // Hardcore stacks on top: it is the reward half of that upgrade, paying for the
    // tougher, faster enemies it gives the DifficultyDirector. Both maxed gives 2.20x.
    void LoadXpGainMultiplier()
    {
        xpGainMultiplier =
            1f + MetaUpgrades.GetTotalValue(MetaUpgradeIds.XpGain)
               + MetaUpgrades.GetTotalValue(MetaUpgradeIds.Hardcore);
    }

    void Start()
    {
        if (inkXPUI == null)
            inkXPUI = FindObjectOfType<InkXPUI>();

        // Seed from the curve so the serialized requiredXP can't fight it.
        requiredXP = XPRequiredForLevel(playerLevel);

        // Level 1 costs 1 XP and the player starts with exactly that, so the bottle
        // is ready straight away and the first thing they do is take the fireball
        // unlock. Until they do, the run clock and enemy spawning stay paused.
        // Deliberately unscaled by the XP gain upgrade: startingXP and firstLevelXP are
        // pinned to each other to stop the tutorial soft-locking, and the meta upgrade
        // has no business shifting that.
        if (startingXP > 0f)
            AddXP(startingXP, false);
    }

    /// <summary>XP needed to get from the given level to the next one.</summary>
    public float XPRequiredForLevel(int level)
    {
        EnsureCurves();

        // The tutorial level is not curve driven. The scene's curve is authored in
        // the inspector and has drifted out of sync before - when it did, level 1
        // cost more than startingXP, the bottle never armed and the whole run
        // soft-locked before the clock started. Pin it here so that can't recur.
        if (level <= 1)
            return Mathf.Max(1f, firstLevelXP);

        float value = xpRequiredPerLevel.Evaluate(level) * xpRequiredMultiplier;

        // AnimationCurve clamps past its last key, which would make every further
        // level cost the same. Keep it climbing for endless mode.
        int lastLevel = Mathf.RoundToInt(xpRequiredPerLevel[xpRequiredPerLevel.length - 1].time);

        if (level > lastLevel)
            value *= Mathf.Pow(1f + endlessGrowthPerLevel, level - lastLevel);

        // Never zero or negative: InkXPUI divides by requiredXP unguarded every
        // frame (XPBarUIManager.cs:133 and :370), so a curve dipping to 0 would
        // push NaN into the bar fill and wedge the readiness check.
        return Mathf.Max(1f, Mathf.Round(value));
    }

    void Reset()
    {
        EnsureCurves();
    }

    void OnValidate()
    {
        EnsureCurves();
    }

    // Built in code rather than authored into the scene YAML: an AnimationCurve
    // that deserialises empty evaluates to 0, which here would mean every level
    // costs nothing and the player levels up infinitely on the first orb.
    void EnsureCurves()
    {
        if (!CurveUtil.IsEmpty(xpRequiredPerLevel))
            return;

        // Only a fallback: the scene serialises its own curve and that is the
        // source of truth. Keep the two in sync - this must mirror what the
        // inspector holds, or a scene without an authored curve will pace
        // differently from one with it.
        //
        // Level 1 costs 1 XP - that is the tutorial level, where the player takes
        // the fireball unlock. The (1, 1) key is mirrored by firstLevelXP, which
        // overrides it in XPRequiredForLevel either way. Every key after it is the
        // previously tuned curve shifted up one level, so the real pacing is
        // unchanged: level 2 costs 35, which used to be level 1's cost.
        //
        // Paced against DifficultyDirector's XP income so that, at roughly 60% orb
        // collection, the fireball tree is maxed around 160s and both the fireball
        // and wave trees are done by ~340s, inside the 6 minute target. Carries on
        // to ~29 level-ups by the 600s run end, leaving room for Lightning and
        // further attacks.
        xpRequiredPerLevel = CurveUtil.LinearCurve(
            1f, 1f, 2f, 35f, 4f, 75f, 6f, 130f, 8f, 200f, 10f, 290f, 12f, 390f,
            14f, 500f, 16f, 620f, 18f, 750f, 20f, 900f, 23f, 1250f, 26f, 1700f,
            29f, 2250f, 32f, 2900f
        );
    }

    public void AddXP(float amount, bool applyUpgradeMultiplier = true)
    {
        if (isLevelingUp)
            return;

        if (applyUpgradeMultiplier)
            amount *= xpGainMultiplier;

        xpLevel += amount;
        xpTotal += amount;

        Debug.Log($"XP: {xpLevel} / Required: {requiredXP}");

        if (xpLevel >= requiredXP && !upgradeReady)
        {
            upgradeReady = true;
            Debug.Log("✅ Upgrade ready! Click the bottle!");
        }
    }

    // Returns true only if the level up actually went ahead, so the caller knows
    // whether it is safe to hide the bottle - a refused level up must leave it
    // visible and clickable.
    public bool TriggerLevelUp()
    {
        if (!upgradeReady || isLevelingUp)
            return false;

        if (xpLevel < requiredXP)
            return false;

        Debug.Log("🔥 Triggering level up!");
        LevelUp();

        return true;
    }

    void LevelUp()
    {
        isLevelingUp = true;
        upgradeReady = false;

        float overflowXP = xpLevel - requiredXP;

        playerLevel++;
        requiredXP = XPRequiredForLevel(playerLevel);
        xpLevel = overflowXP;

        if (xpLevel < 0)
            xpLevel = 0;

        Debug.Log($"⬆️ Level {playerLevel}! Next needs {requiredXP} XP. Overflow: {xpLevel}");

        if (inkXPUI != null)
            inkXPUI.OnLevelUp();

        Time.timeScale = 0f;
        StartCoroutine(LevelUpSequence());
    }

    IEnumerator LevelUpSequence()
    {
        yield return new WaitForSecondsRealtime(0.4f);
        upgradeManager.ShowUpgrades();
    }

    // Called after ONE upgrade is selected
    public void CompleteLevelUp()
    {
        isLevelingUp = false;

        // Check if we have enough XP for ANOTHER level
        if (xpLevel >= requiredXP)
        {
            upgradeReady = true;
            Debug.Log($"🔄 Another level available! ({xpLevel} >= {requiredXP})");

            // Trigger the next level up (panel will refresh with new choices)
            LevelUp();
        }
        else
        {
            upgradeReady = false;
            Debug.Log($"❌ No more levels. ({xpLevel} < {requiredXP}) - Closing panel.");

            upgradeManager.CloseUpgradePanel();
        }
    }

    public bool IsLevelingUp()
    {
        return isLevelingUp;
    }

    public bool IsUpgradeReady()
    {
        return upgradeReady;
    }

    public int EndRunAndAddCoins()
    {
        int coinsEarned = Mathf.FloorToInt(xpTotal * coinsPerXP);
        CoinBank.AddCoins(coinsEarned);
        Debug.Log($"Run ended. Coins earned: {coinsEarned}");
        return coinsEarned;
    }
}