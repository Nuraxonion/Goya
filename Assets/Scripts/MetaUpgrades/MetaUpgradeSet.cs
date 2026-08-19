using UnityEngine;

/// <summary>
/// One meta upgrade the player buys with coins in the Art Shop. This is the
/// authored definition only - the purchased level lives in PlayerPrefs, and the
/// scene objects that draw it live on ArtShopManager.
/// </summary>
[System.Serializable]
public class MetaUpgrade
{
    [Header("Identity")]

    [Tooltip("Stable key gameplay code looks this upgrade up by. Renaming it orphans the upgrade - use MetaUpgradeIds for the ones the game reads.")]
    public string id = "";

    [Tooltip("Shown above the icon in the shop.")]
    public string upgradeName = "New Upgrade";

    [TextArea(2, 4)]
    public string description = "";

    [Tooltip("The effect line, e.g. \"+10% XP Gain\". Written as authored - the shop adds the level counter itself.")]
    public string statsText = "";

    [TextArea(2, 4)]
    [Tooltip("Description shown once every level is bought. Falls back to the normal description if left empty.")]
    public string maxedDescription = "";

    public Sprite icon;


    [Header("Levels")]

    [Tooltip("Coin cost of each level in order. The array length IS the maximum level.")]
    public int[] costs = { 100, 200, 300, 400, 500, 600 };

    [Tooltip("What one level is worth. The unit is whatever the gameplay script reading it expects - health for Vitality, a 0-1 fraction for XP Gain.")]
    public float valuePerLevel = 1f;


    [Header("Saving")]

    [Tooltip("PlayerPrefs key holding the purchased level. Leave empty to use MetaUpgrade_<id>. Set explicitly only to keep an older save key working.")]
    public string prefKeyOverride = "";


    public int MaxLevel
    {
        get { return costs != null ? costs.Length : 0; }
    }

    public string PrefKey
    {
        get
        {
            return string.IsNullOrEmpty(prefKeyOverride)
                ? "MetaUpgrade_" + id
                : prefKeyOverride;
        }
    }

    /// <summary>Coin cost of the next level, or 0 once the upgrade is maxed.</summary>
    public int CostForLevel(int level)
    {
        if (costs == null || level < 0 || level >= costs.Length)
            return 0;

        return costs[level];
    }

    /// <summary>Total effect at the given level, e.g. 0.30 of XP gain at level 3.</summary>
    public float TotalValueAt(int level)
    {
        return valuePerLevel * level;
    }

    /// <summary>Coins spent to reach the given level - the sum of every cost paid so far.</summary>
    public int TotalSpentAt(int level)
    {
        if (costs == null)
            return 0;

        int spent = 0;

        for (int i = 0; i < level && i < costs.Length; i++)
            spent += costs[i];

        return spent;
    }
}

/// <summary>
/// The ids gameplay code reads. Kept as constants so a typo is a compile error
/// rather than an upgrade that silently does nothing.
/// </summary>
public static class MetaUpgradeIds
{
    public const string Vitality = "Vitality";
    public const string XpGain = "XpGain";

    // One per attack: each adds flat seconds to how long that attack stays active.
    public const string FireballDuration = "FireballDuration";
    public const string WaveDuration = "WaveDuration";
    public const string LightningDuration = "LightningDuration";

    // One per attack: each adds flat base damage, kept out of the multiplied damage
    // fields - see PlayerStats.ApplyMetaDamageUpgrades.
    public const string FireballDamage = "FireballDamage";
    public const string WaveDamage = "WaveDamage";
    public const string LightningDamage = "LightningDamage";

    // Difficulty trade: tougher, faster enemies in exchange for more XP.
    public const string Hardcore = "Hardcore";
}

/// <summary>
/// Every meta upgrade in one asset, so the Art Shop and the gameplay scenes read
/// the same numbers instead of each holding their own copy.
///
/// Lives at Assets/Resources/MetaUpgradeSet.asset - MetaUpgrades loads it from
/// there, which is what lets scripts in the gameplay scene reach it with no
/// inspector wiring. Same arrangement as LightningConfig.
/// </summary>
[CreateAssetMenu(fileName = "MetaUpgradeSet", menuName = "Goya/Meta Upgrade Set")]
public class MetaUpgradeSet : ScriptableObject
{
    public MetaUpgrade[] upgrades = new MetaUpgrade[0];

    public MetaUpgrade Find(string id)
    {
        if (upgrades == null || string.IsNullOrEmpty(id))
            return null;

        for (int i = 0; i < upgrades.Length; i++)
        {
            if (upgrades[i] != null && upgrades[i].id == id)
                return upgrades[i];
        }

        return null;
    }
}
