using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CooldownBubbleManager : MonoBehaviour
{
    [Header("References")]
    public PlayerAttack playerAttack;
    public PlayerStats playerStats;

    [Header("Cooldown Settings")]
    public float updateInterval = 0.1f;

    private Dictionary<string, CooldownState> cooldownStates =
        new Dictionary<string, CooldownState>();

    private const string FIREBALL_ID = "Fireball";
    private const string WAVE_ID = "WaveAttack";

    void Start()
    {
        if (playerAttack == null)
            playerAttack = FindObjectOfType<PlayerAttack>();

        if (playerStats == null)
            playerStats = FindObjectOfType<PlayerStats>();

        InitializeAbilities();
        StartCoroutine(UpdateCooldowns());

        Debug.Log("CooldownBubbleManager started");
    }

    void InitializeAbilities()
    {
        cooldownStates.Clear();

        CooldownState fireballState = new CooldownState
        {
            abilityId = FIREBALL_ID,
            abilityName = "Fireball",
            isOnCooldown = false,
            isUnlocked = true,
            currentLevel = playerStats.fireballLevel,
            maxLevel = 8,
            maxCooldown = playerStats.fireballAttackInterval,
            currentCooldown = 0f
        };

        cooldownStates.Add(FIREBALL_ID, fireballState);

        CooldownState waveState = new CooldownState
        {
            abilityId = WAVE_ID,
            abilityName = "WaveAttack",
            isOnCooldown = false,
            isUnlocked = playerStats.hasWaveAttack,
            currentLevel = playerStats.waveLevel,
            maxLevel = 8,
            maxCooldown = playerStats.waveAttackInterval,
            currentCooldown = 0f
        };

        cooldownStates.Add(WAVE_ID, waveState);

        UpdateBubbleVisibility(FIREBALL_ID, true);
        UpdateBubbleVisibility(WAVE_ID, playerStats.hasWaveAttack);

        UpdateAllBubbles();
    }

    IEnumerator UpdateCooldowns()
    {
        while (true)
        {
            yield return new WaitForSeconds(updateInterval);

            UpdateAbilityCooldown(FIREBALL_ID, playerAttack.fireballCooldown);
            UpdateAbilityCooldown(WAVE_ID, playerAttack.waveCooldown);

            RefreshAllBubbles();
        }
    }

    void UpdateAbilityCooldown(string abilityId, float cooldownValue)
    {
        if (!cooldownStates.ContainsKey(abilityId))
            return;

        CooldownState state = cooldownStates[abilityId];

        if (!state.isUnlocked)
            return;

        if (cooldownValue > 0f)
        {
            state.isOnCooldown = true;
            state.currentCooldown = cooldownValue;
        }
        else
        {
            state.isOnCooldown = false;
            state.currentCooldown = 0f;
        }
    }

    void UpdateAllBubbles()
    {
        foreach (CooldownState state in cooldownStates.Values)
        {
            UpdateBubbleUI(state);
        }
    }

    void UpdateBubbleUI(CooldownState state)
    {
        GameObject bubble =
            GameObject.Find(state.abilityId + "Bubble");

        if (bubble == null)
        {
            Debug.LogWarning(state.abilityId + "Bubble not found!");
            return;
        }

        // Update Cooldown Fill (RED)
        Transform fill =
            bubble.transform.Find("CooldownFill");

        if (fill != null)
        {
            Image fillImage = fill.GetComponent<Image>();

            if (fillImage != null)
            {
                float fillAmount = 0f;

                if (state.isOnCooldown && state.maxCooldown > 0f)
                {
                    fillAmount = Mathf.Clamp01(state.currentCooldown / state.maxCooldown);
                }

                fillImage.fillAmount = fillAmount;
                fillImage.gameObject.SetActive(state.isOnCooldown);
            }
        }

        // Update Icon
        Transform icon =
            bubble.transform.Find("AbilityIcon");

        if (icon != null)
        {
            Image iconImage = icon.GetComponent<Image>();

            if (iconImage != null)
            {
                iconImage.color = state.isUnlocked
                    ? Color.white
                    : new Color(0.35f, 0.35f, 0.35f, 0.5f);
            }
        }

        // Update Pips
        UpdatePipsDirect(bubble, state.currentLevel, state.maxLevel);
    }

    void UpdatePipsDirect(GameObject bubble, int currentLevel, int maxLevel)
    {
        Debug.Log($"🎯 Updating pips for {bubble.name}: Level={currentLevel}/{maxLevel}");

        for (int i = 1; i <= maxLevel; i++)
        {
            Transform pip = bubble.transform.Find("Pip" + i);

            if (pip == null)
            {
                Debug.LogWarning($"⚠️ Pip{i} not found in {bubble.name}!");
                continue;
            }

            Image pipImage = pip.GetComponent<Image>();

            if (pipImage == null)
                continue;

            if (i <= currentLevel)
            {
                pipImage.color = Color.red;
                Debug.Log($"✅ {bubble.name} Pip{i} turned RED");
            }
            else
            {
                pipImage.color = Color.white;
                Debug.Log($"⬜ {bubble.name} Pip{i} is WHITE");
            }
        }
    }

    void UpdateBubbleVisibility(string abilityId, bool visible)
    {
        GameObject ability =
            GameObject.Find(abilityId + "Ability");

        if (ability == null)
        {
            Debug.LogWarning(abilityId + "Ability not found!");
            return;
        }

        ability.SetActive(visible);
    }

    public void UnlockAbility(string abilityId)
    {
        if (!cooldownStates.ContainsKey(abilityId))
        {
            Debug.LogWarning("Ability not found: " + abilityId);
            return;
        }

        CooldownState state = cooldownStates[abilityId];

        state.isUnlocked = true;

        if (abilityId == FIREBALL_ID)
        {
            state.currentLevel = playerStats.fireballLevel;
        }
        else if (abilityId == WAVE_ID)
        {
            state.currentLevel = Mathf.Max(1, playerStats.waveLevel);
        }

        UpdateBubbleVisibility(abilityId, true);
        UpdateAllBubbles();

        Debug.Log("Unlocked " + abilityId);
    }

    public void LevelUpAbility(string abilityId)
    {
        Debug.Log($"⬆️ LevelUpAbility called for: {abilityId}");

        if (!cooldownStates.ContainsKey(abilityId))
        {
            Debug.LogWarning($"❌ {abilityId} not found in cooldownStates!");
            return;
        }

        CooldownState state = cooldownStates[abilityId];

        if (!state.isUnlocked)
        {
            Debug.LogWarning($"⚠️ {abilityId} is not unlocked!");
            return;
        }

        // Get the level from PlayerStats
        if (abilityId == FIREBALL_ID)
        {
            state.currentLevel = playerStats.fireballLevel;
            Debug.Log($"🔥 Fireball level set to: {state.currentLevel}");
        }
        else if (abilityId == WAVE_ID)
        {
            state.currentLevel = playerStats.waveLevel;
            Debug.Log($"🌊 WaveAttack level set to: {state.currentLevel}");
        }

        UpdateAllBubbles();

        Debug.Log($"✅ {state.abilityName} Level {state.currentLevel}");
    }

    public void RefreshAllBubbles()
    {
        Debug.Log("🔄 RefreshAllBubbles called!");

        if (cooldownStates.ContainsKey(FIREBALL_ID))
        {
            cooldownStates[FIREBALL_ID].currentLevel =
                playerStats.fireballLevel;

            cooldownStates[FIREBALL_ID].maxCooldown =
                playerStats.fireballAttackInterval;
        }

        if (cooldownStates.ContainsKey(WAVE_ID))
        {
            cooldownStates[WAVE_ID].isUnlocked =
                playerStats.hasWaveAttack;

            cooldownStates[WAVE_ID].currentLevel =
                playerStats.waveLevel;

            cooldownStates[WAVE_ID].maxCooldown =
                playerStats.waveAttackInterval;
        }

        UpdateBubbleVisibility(
            FIREBALL_ID,
            true);

        UpdateBubbleVisibility(
            WAVE_ID,
            playerStats.hasWaveAttack);

        UpdateAllBubbles();
    }
}

[System.Serializable]
public class CooldownState
{
    public string abilityId;
    public string abilityName;

    public bool isOnCooldown;

    public bool isUnlocked;

    public int currentLevel;

    public int maxLevel = 8;

    public float maxCooldown;

    public float currentCooldown;
}