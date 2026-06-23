using UnityEngine;

[CreateAssetMenu(menuName = "Upgrades/Upgrade")]
public class UpgradeData : ScriptableObject
{
    public string upgradeName;

    [TextArea]
    public string description;

    public int weight = 100;

    public bool requiresUnlock;
    public string requiredUpgradeID;

    public string upgradeID;

    public Sprite icon;

    public int maxLevel = 5;

    public UpgradeType type;

    public float valueIncrease;

    public UpgradeRarity rarity;

    public bool oneTimeUpgrade = false;

    public enum UpgradeRarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
    }

    public enum UpgradeType
    {
        FireballDamage,
        FireballDuration,
        FireballCooldown,
        FireballQuantity,
        Wave,
        WaveDamage,
        WaveDuration,
        WaveCooldown,
        MaxHealth,
        Heal,
    }

    public enum UpgradeCategory
    {
        Weapon,
        Upgrade,
        Item
    }
}