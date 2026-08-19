using UnityEngine;

/// <summary>
/// Read/write access to the player's purchased meta upgrades, from any scene.
///
/// Static on purpose, like CoinBank: the gameplay scenes need the levels without
/// a reference to the Art Shop, and the levels are plain PlayerPrefs ints. The
/// per-level values come from Assets/Resources/MetaUpgradeSet.asset, so there is
/// exactly one place to tune them.
/// </summary>
public static class MetaUpgrades
{
    const string ResourcePath = "MetaUpgradeSet";

    static MetaUpgradeSet cachedSet;

    public static MetaUpgradeSet Set
    {
        get
        {
            if (cachedSet == null)
            {
                cachedSet = Resources.Load<MetaUpgradeSet>(ResourcePath);

                if (cachedSet == null)
                {
                    Debug.LogWarning(
                        "No MetaUpgradeSet found at Assets/Resources/" + ResourcePath +
                        ".asset - every meta upgrade will read as level 0.");
                }
            }

            return cachedSet;
        }
    }

    public static MetaUpgrade Find(string id)
    {
        MetaUpgradeSet set = Set;

        return set != null ? set.Find(id) : null;
    }

    /// <summary>Purchased level, clamped to what the definition actually allows.</summary>
    public static int GetLevel(string id)
    {
        MetaUpgrade upgrade = Find(id);

        return upgrade != null ? GetLevel(upgrade) : 0;
    }

    public static int GetLevel(MetaUpgrade upgrade)
    {
        if (upgrade == null)
            return 0;

        int stored = PlayerPrefs.GetInt(upgrade.PrefKey, 0);

        return Mathf.Clamp(stored, 0, upgrade.MaxLevel);
    }

    public static void SetLevel(MetaUpgrade upgrade, int level)
    {
        if (upgrade == null)
            return;

        PlayerPrefs.SetInt(
            upgrade.PrefKey,
            Mathf.Clamp(level, 0, upgrade.MaxLevel));

        PlayerPrefs.Save();
    }

    /// <summary>
    /// Total effect the player has bought, e.g. 0.30 for three levels of a 0.10
    /// per level upgrade. Returns 0 for an unknown id, so a missing definition
    /// degrades to "no upgrade" rather than throwing mid-run.
    /// </summary>
    public static float GetTotalValue(string id)
    {
        MetaUpgrade upgrade = Find(id);

        if (upgrade == null)
            return 0f;

        return upgrade.TotalValueAt(GetLevel(upgrade));
    }
}
