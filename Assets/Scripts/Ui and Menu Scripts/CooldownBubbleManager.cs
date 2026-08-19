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

    [Header("Pip Colors")]
    public Color pipRedColor = new Color(255f / 255f, 88f / 255f, 88f / 255f, 1f);
    public Color pipWhiteColor = Color.white;

    private Dictionary<string, CooldownState> cooldownStates =
        new Dictionary<string, CooldownState>();

    private const string FIREBALL_ID = "Fireball";
    private const string WAVE_ID = "WaveAttack";
    private const string SPIRAL_ID = "Spiral";

    // Cached because GameObject.Find cannot see inactive objects: once a locked
    // ability is hidden, only a reference taken while it was still reachable can
    // ever switch it back on.
    private GameObject fireballAbility;
    private GameObject fireballBubble;
    private GameObject waveAbility;
    private GameObject waveBubble;
    private GameObject spiralAbility;
    private GameObject spiralBubble;
    private GameObject container;

    void Start()
    {
        if (playerAttack == null)
            playerAttack = FindObjectOfType<PlayerAttack>();

        if (playerStats == null)
            playerStats = FindObjectOfType<PlayerStats>();

        container = GameObject.Find("BubbleContainer");
        if (container == null)
        {
            Debug.LogError("❌ BubbleContainer NOT found!");
            return;
        }

        // Find FireballAbility. Transform.Find traverses inactive children, unlike
        // GameObject.Find - and this must be cached before InitializeAbilities
        // hides the bubble for a locked fireball, or it can never be shown again.
        Transform fireballAbilityTransform = container.transform.Find("FireballAbility");
        if (fireballAbilityTransform != null)
        {
            fireballAbility = fireballAbilityTransform.gameObject;
            Debug.Log("✅ FireballAbility found!");

            Transform fireballBubbleTransform = fireballAbilityTransform.Find("FireballBubble");
            if (fireballBubbleTransform != null)
            {
                fireballBubble = fireballBubbleTransform.gameObject;

                // The child stays on; visibility is driven at the Ability level.
                fireballBubble.SetActive(true);
                Debug.Log("✅ FireballBubble found!");
            }
        }

        // Find WaveAttackAbility
        Transform waveAbilityTransform = container.transform.Find("WaveAttackAbility");
        if (waveAbilityTransform != null)
        {
            waveAbility = waveAbilityTransform.gameObject;
            waveAbility.SetActive(true);
            Debug.Log("✅ WaveAttackAbility found!");

            Transform waveBubbleTransform = waveAbilityTransform.Find("WaveAttackBubble");
            if (waveBubbleTransform != null)
            {
                waveBubble = waveBubbleTransform.gameObject;
                waveBubble.SetActive(true);
                Debug.Log("✅ WaveAttackBubble found!");
            }
        }

        // Find SpiralAbility
        Transform spiralAbilityTransform = container.transform.Find("SpiralAbility");
        if (spiralAbilityTransform != null)
        {
            spiralAbility = spiralAbilityTransform.gameObject;
            spiralAbility.SetActive(true);
            Debug.Log("✅ SpiralAbility found!");

            Transform spiralBubbleTransform = spiralAbilityTransform.Find("SpiralBubble");
            if (spiralBubbleTransform != null)
            {
                spiralBubble = spiralBubbleTransform.gameObject;
                spiralBubble.SetActive(true);
                Debug.Log("✅ SpiralBubble found!");
            }
        }

        InitializeAbilities();
        StartCoroutine(UpdateCooldowns());

        Debug.Log("CooldownBubbleManager started");
    }

    void InitializeAbilities()
    {
        cooldownStates.Clear();

        // Fireball
        CooldownState fireballState = new CooldownState
        {
            abilityId = FIREBALL_ID,
            abilityName = "Fireball",
            isOnCooldown = false,
            isUnlocked = playerStats.hasFireballAttack,
            currentLevel = playerStats.fireballLevel,
            maxLevel = 8,
            maxCooldown = playerStats.fireballAttackInterval,
            currentCooldown = 0f
        };
        cooldownStates.Add(FIREBALL_ID, fireballState);

        // Wave
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

        // Spiral - FIXED: Make sure it's tracked properly
        CooldownState spiralState = new CooldownState
        {
            abilityId = SPIRAL_ID,
            abilityName = "Spiral",
            isOnCooldown = false,
            isUnlocked = playerStats.hasSpiralAttack,
            currentLevel = 1, // Spiral has 1 level (unlocked/not unlocked)
            maxLevel = 1,
            maxCooldown = playerStats.spiralAttackInterval,
            currentCooldown = 0f
        };
        cooldownStates.Add(SPIRAL_ID, spiralState);

        UpdateBubbleVisibility(FIREBALL_ID, playerStats.hasFireballAttack);
        UpdateBubbleVisibility(WAVE_ID, playerStats.hasWaveAttack);
        UpdateBubbleVisibility(SPIRAL_ID, playerStats.hasSpiralAttack);

        UpdateAllBubbles();
    }

    IEnumerator UpdateCooldowns()
    {
        while (true)
        {
            yield return new WaitForSeconds(updateInterval);

            // Update all ability cooldowns
            if (playerAttack != null)
            {
                UpdateAbilityCooldown(FIREBALL_ID, playerAttack.fireballCooldown);
                UpdateAbilityCooldown(WAVE_ID, playerAttack.waveCooldown);
                UpdateAbilityCooldown(SPIRAL_ID, playerAttack.spiralCooldown);

                Debug.Log($"🔄 Spiral Cooldown: {playerAttack.spiralCooldown}");
            }

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

        // Update cooldown state
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

    GameObject ResolveBubble(CooldownState state)
    {
        if (state.bubble != null)
            return state.bubble;

        if (state.bubbleResolved)
            return null;

        state.bubbleResolved = true;

        // Use the references cached in Start(), which were taken while every
        // ability was still reachable.
        if (state.abilityId == FIREBALL_ID && fireballBubble != null)
        {
            state.bubble = fireballBubble;
            return state.bubble;
        }

        if (state.abilityId == WAVE_ID && waveBubble != null)
        {
            state.bubble = waveBubble;
            return state.bubble;
        }

        if (state.abilityId == SPIRAL_ID && spiralBubble != null)
        {
            state.bubble = spiralBubble;
            return state.bubble;
        }

        state.bubble = GameObject.Find(state.abilityId + "Bubble");

        if (state.bubble == null)
            Debug.LogWarning($"❌ {state.abilityId}Bubble not found!");

        return state.bubble;
    }

    void UpdateBubbleUI(CooldownState state)
    {
        GameObject bubble = ResolveBubble(state);

        if (bubble == null)
            return;

        // Update cooldown fill
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

                // DEBUG: Log Spiral cooldown fill
                if (state.abilityId == SPIRAL_ID)
                {
                    Debug.Log($"🌀 Spiral fill: {fillAmount} (cooldown: {state.currentCooldown}/{state.maxCooldown})");
                }
            }
        }

        // Update icon color
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

        // Update pips (if not Spiral)
        if (state.abilityId != SPIRAL_ID)
        {
            UpdatePipsDirect(bubble, state.currentLevel, state.maxLevel, state.abilityId);
        }
    }

    void UpdatePipsDirect(GameObject bubble, int currentLevel, int maxLevel, string abilityId)
    {
        int displayLevel = currentLevel;

        if (abilityId == "Fireball")
        {
            displayLevel = Mathf.Max(0, currentLevel - 1);
        }

        Transform pipsContainer = bubble.transform.Find("PipsContainer");
        Transform targetParent = pipsContainer != null ? pipsContainer : bubble.transform;

        for (int i = 1; i <= maxLevel; i++)
        {
            Transform pip = targetParent.Find("Pip" + i);
            if (pip == null) pip = targetParent.Find("Pip_" + i);
            if (pip == null) pip = targetParent.Find("pip" + i);

            if (pip == null) continue;

            Image pipImage = pip.GetComponent<Image>();
            if (pipImage == null) continue;

            if (i <= displayLevel)
            {
                pipImage.color = pipRedColor;
                pipImage.enabled = true;
            }
            else if (i <= maxLevel)
            {
                pipImage.color = pipWhiteColor;
                pipImage.enabled = true;
            }
            else
            {
                pipImage.enabled = false;
            }
        }
    }

    void UpdateBubbleVisibility(string abilityId, bool visible)
    {
        if (abilityId == FIREBALL_ID && fireballAbility != null)
        {
            fireballAbility.SetActive(visible);
            return;
        }

        if (abilityId == WAVE_ID && waveAbility != null)
        {
            waveAbility.SetActive(visible);
            return;
        }

        if (abilityId == SPIRAL_ID && spiralAbility != null)
        {
            spiralAbility.SetActive(visible);
            Debug.Log($"🌀 Spiral visibility set to: {visible}");
            return;
        }

        // Search the container first: GameObject.Find skips inactive objects, so on
        // its own it can hide an ability but never bring it back. Any ability
        // without a cached reference above would hit that trap.
        string abilityName = abilityId + "Ability";
        GameObject ability = null;

        if (container != null)
        {
            Transform found = container.transform.Find(abilityName);

            if (found != null)
                ability = found.gameObject;
        }

        if (ability == null)
            ability = GameObject.Find(abilityName);

        if (ability != null)
        {
            ability.SetActive(visible);
        }
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
        else if (abilityId == SPIRAL_ID)
        {
            state.currentLevel = 1;
            if (spiralAbility != null)
            {
                spiralAbility.SetActive(true);
                Debug.Log($"✅ SpiralAbility activated on unlock!");
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
        else if (abilityId == SPIRAL_ID)
        {
            // Spiral doesn't level up, just stays at 1
            state.currentLevel = 1;
        }

        UpdateAllBubbles();

        Debug.Log($"{state.abilityName} Level {state.currentLevel}");
    }

    public void RefreshAllBubbles()
    {
        // Update visibility
        if (waveAbility != null)
        {
            waveAbility.SetActive(playerStats.hasWaveAttack);
        }

        if (spiralAbility != null)
        {
            spiralAbility.SetActive(playerStats.hasSpiralAttack);
            Debug.Log($"🌀 Spiral active: {playerStats.hasSpiralAttack}");
        }

        // Update states
        if (cooldownStates.ContainsKey(FIREBALL_ID))
        {
            cooldownStates[FIREBALL_ID].isUnlocked = playerStats.hasFireballAttack;
            cooldownStates[FIREBALL_ID].currentLevel = playerStats.fireballLevel;
            cooldownStates[FIREBALL_ID].maxCooldown = playerStats.fireballAttackInterval;
        }

        if (cooldownStates.ContainsKey(WAVE_ID))
        {
            cooldownStates[WAVE_ID].isUnlocked = playerStats.hasWaveAttack;
            cooldownStates[WAVE_ID].currentLevel = playerStats.waveLevel;
            cooldownStates[WAVE_ID].maxCooldown = playerStats.waveAttackInterval;
        }

        if (cooldownStates.ContainsKey(SPIRAL_ID))
        {
            cooldownStates[SPIRAL_ID].isUnlocked = playerStats.hasSpiralAttack;
            cooldownStates[SPIRAL_ID].maxCooldown = playerStats.spiralAttackInterval;
            Debug.Log($"🌀 Spiral maxCooldown: {playerStats.spiralAttackInterval}");
        }

        UpdateBubbleVisibility(FIREBALL_ID, playerStats.hasFireballAttack);
        UpdateBubbleVisibility(WAVE_ID, playerStats.hasWaveAttack);
        UpdateBubbleVisibility(SPIRAL_ID, playerStats.hasSpiralAttack);

        UpdateAllBubbles();
    }
}

[System.Serializable]
public class CooldownState
{
    public string abilityId;
    public string abilityName;
    public GameObject bubble;
    public bool bubbleResolved;
    public bool isOnCooldown;
    public bool isUnlocked;
    public int currentLevel;
    public int maxLevel = 8;
    public float maxCooldown;
    public float currentCooldown;
}