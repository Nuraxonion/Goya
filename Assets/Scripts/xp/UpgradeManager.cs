using System.Collections.Generic;
using UnityEngine;

public class UpgradeManager : MonoBehaviour
{
    public List<UpgradeData> allUpgrades;

    public List<string> ownedUpgrades = new List<string>();

    public PlayerStats playerStats;

    public UpgradeButton[] buttons;

    public GameObject upgradePanel;

    public bool isGameRunning = true;

    public CooldownBubbleManager cooldownBubbleManager;

    public InkXPUI inkXPUI;

    void Start()
    {
        if (cooldownBubbleManager == null)
            cooldownBubbleManager = FindObjectOfType<CooldownBubbleManager>();

        if (inkXPUI == null)
            inkXPUI = FindObjectOfType<InkXPUI>();
    }

    public void ShowUpgrades()
    {
        if (inkXPUI != null)
            inkXPUI.HideBottle();

        upgradePanel.SetActive(true);
        isGameRunning = false;

        List<UpgradeData> available = GetAvailableUpgrades();

        for (int i = 0; i < buttons.Length; i++)
        {
            if (available.Count <= 0)
                break;

            UpgradeData randomUpgrade = GetWeightedRandomUpgrade(available);

            buttons[i].Setup(randomUpgrade, this);

            available.Remove(randomUpgrade);
        }
    }

    List<UpgradeData> GetAvailableUpgrades()
    {
        List<UpgradeData> list = new List<UpgradeData>();

        List<UpgradeData> fireballUpgrades = new List<UpgradeData>();
        List<UpgradeData> waveUpgrades = new List<UpgradeData>();
        List<UpgradeData> otherUpgrades = new List<UpgradeData>();

        foreach (var upg in allUpgrades)
        {
            if (upg.oneTimeUpgrade && playerStats.upgrades.ContainsKey(upg.upgradeID))
            {
                continue;
            }

            int currentLevel = 0;
            playerStats.upgrades.TryGetValue(upg.upgradeID, out currentLevel);

            if (currentLevel >= upg.maxLevel)
                continue;

            if (!CanAppear(upg))
                continue;

            if (upg.type == UpgradeData.UpgradeType.FireballLevel ||
                upg.type == UpgradeData.UpgradeType.FireballDamage ||
                upg.type == UpgradeData.UpgradeType.FireballCooldown ||
                upg.type == UpgradeData.UpgradeType.FireballDuration ||
                upg.type == UpgradeData.UpgradeType.FireballQuantity ||
                upg.type == UpgradeData.UpgradeType.FireballWeapon)
            {
                fireballUpgrades.Add(upg);
            }
            else if (upg.type == UpgradeData.UpgradeType.Wave ||
                     upg.type == UpgradeData.UpgradeType.WaveLevel ||
                     upg.type == UpgradeData.UpgradeType.WaveDamage ||
                     upg.type == UpgradeData.UpgradeType.WaveCooldown ||
                     upg.type == UpgradeData.UpgradeType.WaveDuration ||
                     upg.type == UpgradeData.UpgradeType.WaveWeapon)
            {
                waveUpgrades.Add(upg);
            }
            else
            {
                otherUpgrades.Add(upg);
            }
        }

        while (fireballUpgrades.Count > 0 || waveUpgrades.Count > 0 || otherUpgrades.Count > 0)
        {
            if (fireballUpgrades.Count > 0)
            {
                UpgradeData upg = GetWeightedRandomUpgrade(fireballUpgrades);
                list.Add(upg);
                fireballUpgrades.Remove(upg);
            }

            if (waveUpgrades.Count > 0)
            {
                UpgradeData upg = GetWeightedRandomUpgrade(waveUpgrades);
                list.Add(upg);
                waveUpgrades.Remove(upg);
            }

            if (otherUpgrades.Count > 0)
            {
                UpgradeData upg = GetWeightedRandomUpgrade(otherUpgrades);
                list.Add(upg);
                otherUpgrades.Remove(upg);
            }
        }

        return list;
    }

    bool CanAppear(UpgradeData upgrade)
    {
        if (upgrade.type == UpgradeData.UpgradeType.Heal ||
            upgrade.type == UpgradeData.UpgradeType.MaxHealth)
        {
            return true;
        }

        if (!upgrade.requiresUnlock)
            return true;

        return ownedUpgrades.Contains(upgrade.requiredUpgradeID);
    }

    UpgradeData GetWeightedRandomUpgrade(List<UpgradeData> pool)
    {
        int totalWeight = 0;

        foreach (var upg in pool)
        {
            totalWeight += upg.weight;
        }

        int randomValue = Random.Range(0, totalWeight);

        foreach (var upg in pool)
        {
            randomValue -= upg.weight;

            if (randomValue < 0)
                return upg;
        }

        return pool[0];
    }

    public void SelectUpgrade(UpgradeData data)
    {
        playerStats.ApplyUpgrade(data);

        if (!ownedUpgrades.Contains(data.upgradeID))
        {
            ownedUpgrades.Add(data.upgradeID);
        }

        UpdateCooldownBubbles(data);

        PlayerXP playerXP = FindObjectOfType<PlayerXP>();

        if (playerXP != null)
        {
            // FIX: Reset XP and check for more levels
            playerXP.ResetXP();
        }
    }

    public void CloseUpgradePanel()
    {
        Time.timeScale = 1f;
        upgradePanel.SetActive(false);
        isGameRunning = true;

        if (inkXPUI != null)
            inkXPUI.OnUpgradeSelected();
    }

    void UpdateCooldownBubbles(UpgradeData data)
    {
        if (cooldownBubbleManager == null)
            return;

        switch (data.type)
        {
            case UpgradeData.UpgradeType.FireballLevel:
            case UpgradeData.UpgradeType.FireballDamage:
            case UpgradeData.UpgradeType.FireballCooldown:
            case UpgradeData.UpgradeType.FireballDuration:
            case UpgradeData.UpgradeType.FireballQuantity:
            case UpgradeData.UpgradeType.FireballWeapon:
                cooldownBubbleManager.LevelUpAbility("Fireball");
                break;

            case UpgradeData.UpgradeType.WaveWeapon:
            case UpgradeData.UpgradeType.WaveLevel:
            case UpgradeData.UpgradeType.WaveDamage:
            case UpgradeData.UpgradeType.WaveCooldown:
            case UpgradeData.UpgradeType.WaveDuration:
                cooldownBubbleManager.LevelUpAbility("WaveAttack");
                break;

            case UpgradeData.UpgradeType.Wave:
                cooldownBubbleManager.UnlockAbility("WaveAttack");
                break;
        }

        cooldownBubbleManager.RefreshAllBubbles();
    }
}