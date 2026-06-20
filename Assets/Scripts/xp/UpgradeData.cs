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

    public enum UpgradeType
    {
        FireballDamage,
        FireballRate,
        FireballCooldown,
        FireballQuantity,
        Wave,
        WaveDamage,
        WaveCooldown,
        MaxHealth,
    }
}