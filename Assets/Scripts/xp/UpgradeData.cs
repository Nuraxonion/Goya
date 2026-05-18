using UnityEngine;

[CreateAssetMenu(menuName = "Upgrade")]
public class UpgradeData : ScriptableObject
{
    public string upgradeName;

    [TextArea]
    public string description;

    public Sprite icon;

    public int maxLevel = 5;

    public UpgradeType type;

    public float valueIncrease;

    public enum UpgradeType
    {
        Damage,
        Speed,
        FireRate,
        MaxHealth
    }
}