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

    private GameObject waveAbility;
    private GameObject waveBubble;

    void Start()
    {
        if (playerAttack == null)
            playerAttack = FindObjectOfType<PlayerAttack>();

        if (playerStats == null)
            playerStats = FindObjectOfType<PlayerStats>();

        // Find both abilities through the container
        GameObject container = GameObject.Find("BubbleContainer");
        if (container != null)
        {
            // Find Fireball (should always work)
            Transform fireballTransform = container.transform.Find("FireballAbility");
            if (fireballTransform != null)
            {
                Debug.Log("✅ FireballAbility found!");
            }

            // Find WaveAttackAbility
            Transform waveAbilityTransform = container.transform.Find("WaveAttackAbility");
            if (waveAbilityTransform != null)
            {
                waveAbility = waveAbilityTransform.gameObject;
                // Keep it active so we can find it later
                waveAbility.SetActive(true);
                Debug.Log("✅ WaveAttackAbility found through container!");

                // Find WaveAttackBubble
                Transform waveBubbleTransform = waveAbilityTransform.Find("WaveAttackBubble");
                if (waveBubbleTransform != null)
                {
                    waveBubble = waveBubbleTransform.gameObject;
                    waveBubble.SetActive(true);
                    Debug.Log("✅ WaveAttackBubble found!");
                }
            }
            else
            {
                Debug.LogError("❌ WaveAttackAbility NOT found in BubbleContainer!");
            }
        }
        else
        {
            Debug.LogError("❌ BubbleContainer NOT found!");
        }

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
        GameObject bubble = GameObject.Find(state.abilityId + "Bubble");

        if (bubble == null)
        {
            Debug.LogWarning(state.abilityId + "Bubble not found!");
            return;
        }

        Transform fill = bubble.transform.Find("CooldownFill");

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

        Transform icon = bubble.transform.Find("AbilityIcon");

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

        UpdatePipsDirect(bubble, state.currentLevel, state.maxLevel);
    }

    void UpdatePipsDirect(GameObject bubble, int currentLevel, int maxLevel)
    {
        int displayLevel = Mathf.Max(0, currentLevel - 1);

        for (int i = 1; i <= maxLevel; i++)
        {
            Transform pip = bubble.transform.Find("Pip" + i);

            if (pip == null)
                continue;

            Image pipImage = pip.GetComponent<Image>();

            if (pipImage == null)
                continue;

            if (i <= displayLevel)
            {
                pipImage.color = Color.red;
            }
            else
            {
                pipImage.color = Color.white;
            }
        }
    }

    void UpdateBubbleVisibility(string abilityId, bool visible)
    {
        // Use the stored reference for Wave
        if (abilityId == WAVE_ID && waveAbility != null)
        {
            waveAbility.SetActive(visible);
            Debug.Log($"✅ {waveAbility.name} SetActive({visible})");
            return;
        }

        string abilityName = abilityId + "Ability";
        GameObject ability = GameObject.Find(abilityName);

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

            if (waveAbility != null)
            {
                waveAbility.SetActive(true);
                Debug.Log($"✅ {waveAbility.name} activated on unlock!");
            }
            else
            {
                Debug.LogError("❌ WaveAbility reference is null! Make sure it's a child of BubbleContainer.");
            }
        }

        UpdateBubbleVisibility(abilityId, true);
        UpdateAllBubbles();

        Debug.Log("Unlocked " + abilityId);
    }

    public void LevelUpAbility(string abilityId)
    {
        if (!cooldownStates.ContainsKey(abilityId))
            return;

        CooldownState state = cooldownStates[abilityId];

        if (!state.isUnlocked)
            return;

        if (abilityId == FIREBALL_ID)
        {
            state.currentLevel = playerStats.fireballLevel;
        }
        else if (abilityId == WAVE_ID)
        {
            state.currentLevel = playerStats.waveLevel;
        }

        UpdateAllBubbles();

        Debug.Log(state.abilityName + " Level " + state.currentLevel);
    }

    public void RefreshAllBubbles()
    {
        if (waveAbility != null)
        {
            waveAbility.SetActive(playerStats.hasWaveAttack);
            Debug.Log($"🔄 {waveAbility.name} set to: {playerStats.hasWaveAttack}");
        }

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

        UpdateBubbleVisibility(FIREBALL_ID, true);
        UpdateBubbleVisibility(WAVE_ID, playerStats.hasWaveAttack);

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