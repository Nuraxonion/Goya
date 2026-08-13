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
    private const string SPIRAL_ID = "Spiral";

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

        // Find the container
        container = GameObject.Find("BubbleContainer");
        if (container == null)
        {
            Debug.LogError("❌ BubbleContainer NOT found!");
            return;
        }

        // Find WaveAttackAbility
        Transform waveAbilityTransform = container.transform.Find("WaveAttackAbility");
        if (waveAbilityTransform != null)
        {
            waveAbility = waveAbilityTransform.gameObject;
            waveAbility.SetActive(true);
            Debug.Log("✅ WaveAttackAbility found!");

            // Find WaveAttackBubble (child of WaveAttackAbility)
            Transform waveBubbleTransform = waveAbilityTransform.Find("WaveAttackBubble");
            if (waveBubbleTransform != null)
            {
                waveBubble = waveBubbleTransform.gameObject;
                waveBubble.SetActive(true);
                Debug.Log("✅ WaveAttackBubble found!");

                // DEBUG: Log all children of the bubble
                Debug.Log("=== WaveAttackBubble Children ===");
                foreach (Transform child in waveBubbleTransform)
                {
                    Debug.Log("Child: " + child.name);
                }
                Debug.Log("=== End WaveAttackBubble Children ===");
            }
            else
            {
                Debug.LogError("❌ WaveAttackBubble NOT found under WaveAttackAbility!");
            }
        }
        else
        {
            Debug.LogError("❌ WaveAttackAbility NOT found in BubbleContainer!");
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
        else
        {
            Debug.LogWarning("⚠️ SpiralAbility NOT found in BubbleContainer!");
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

        CooldownState spiralState = new CooldownState
        {
            abilityId = SPIRAL_ID,
            abilityName = "Spiral",
            isOnCooldown = false,
            isUnlocked = playerStats.hasSpiralAttack,
            currentLevel = 0,
            maxLevel = 1,
            maxCooldown = playerStats.spiralAttackInterval,
            currentCooldown = 0f
        };

        cooldownStates.Add(SPIRAL_ID, spiralState);

        UpdateBubbleVisibility(FIREBALL_ID, true);
        UpdateBubbleVisibility(WAVE_ID, playerStats.hasWaveAttack);
        UpdateBubbleVisibility(SPIRAL_ID, playerStats.hasSpiralAttack);

        UpdateAllBubbles();
    }

    IEnumerator UpdateCooldowns()
    {
        while (true)
        {
            yield return new WaitForSeconds(updateInterval);

            UpdateAbilityCooldown(FIREBALL_ID, playerAttack.fireballCooldown);
            UpdateAbilityCooldown(WAVE_ID, playerAttack.waveCooldown);
            UpdateAbilityCooldown(SPIRAL_ID, playerAttack.spiralCooldown);

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

    GameObject ResolveBubble(CooldownState state)
    {
        // If we already have a cached reference, use it
        if (state.bubble != null)
            return state.bubble;

        if (state.bubbleResolved)
            return null;

        state.bubbleResolved = true;

        // FIX: Use the stored waveBubble reference for Wave
        if (state.abilityId == WAVE_ID && waveBubble != null)
        {
            state.bubble = waveBubble;
            Debug.Log($"✅ Using cached WaveAttackBubble: {state.bubble.name}");
            return state.bubble;
        }

        // For other abilities, try to find them
        if (state.abilityId == FIREBALL_ID)
        {
            // Try to find FireballBubble under FireballAbility
            if (container != null)
            {
                Transform fireballAbility = container.transform.Find("FireballAbility");
                if (fireballAbility != null)
                {
                    Transform fireballBubble = fireballAbility.Find("FireballBubble");
                    if (fireballBubble != null)
                    {
                        state.bubble = fireballBubble.gameObject;
                        Debug.Log($"✅ Found FireballBubble: {state.bubble.name}");
                        return state.bubble;
                    }
                }
            }
        }

        // Fallback: try GameObject.Find
        state.bubble = GameObject.Find(state.abilityId + "Bubble");

        if (state.bubble == null)
            Debug.LogWarning($"❌ {state.abilityId}Bubble not found!");

        return state.bubble;
    }

    void UpdateBubbleUI(CooldownState state)
    {
        GameObject bubble = ResolveBubble(state);

        if (bubble == null)
        {
            Debug.LogWarning($"❌ Bubble is null for {state.abilityId}");
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

        // Only update pips if ability has them (Fireball and Wave)
        if (state.abilityId != SPIRAL_ID)
        {
            UpdatePipsDirect(bubble, state.currentLevel, state.maxLevel, state.abilityId);
        }
    }

    void UpdatePipsDirect(GameObject bubble, int currentLevel, int maxLevel, string abilityId)
    {
        // For Wave, displayLevel = currentLevel (NO subtraction)
        // For Fireball, subtract 1 to hide the starting level
        int displayLevel = currentLevel;

        if (abilityId == "Fireball")
        {
            displayLevel = Mathf.Max(0, currentLevel - 1);
        }

        Debug.Log($"=== Updating pips for {abilityId} ===");
        Debug.Log($"Current Level: {currentLevel}, Display Level: {displayLevel}, Max: {maxLevel}");
        Debug.Log($"Bubble name: {bubble.name}");

        // Try to find pips in PipsContainer first, then direct children
        Transform pipsContainer = bubble.transform.Find("PipsContainer");
        Transform targetParent = pipsContainer != null ? pipsContainer : bubble.transform;

        Debug.Log($"Using parent: {targetParent.name} (PipsContainer found: {pipsContainer != null})");

        int foundPips = 0;
        int pipsFoundAndSet = 0;

        for (int i = 1; i <= maxLevel; i++)
        {
            // Try multiple naming conventions
            Transform pip = targetParent.Find("Pip" + i);
            if (pip == null) pip = targetParent.Find("Pip_" + i);
            if (pip == null) pip = targetParent.Find("Pip " + i);
            if (pip == null) pip = targetParent.Find("pip" + i);
            if (pip == null) pip = targetParent.Find("Pips" + i);

            if (pip == null)
            {
                continue;
            }

            foundPips++;
            Debug.Log($"✅ Found Pip{i}");

            Image pipImage = pip.GetComponent<Image>();

            if (pipImage == null)
            {
                Debug.Log($"❌ Pip{i} has no Image component!");
                continue;
            }

            // Set the color based on level
            if (i <= displayLevel)
            {
                pipImage.color = Color.red;
                pipImage.enabled = true;
                pipsFoundAndSet++;
                Debug.Log($"🔴 Pip{i} set to RED (i={i} <= displayLevel={displayLevel})");
            }
            else if (i <= maxLevel)
            {
                pipImage.color = Color.white;
                pipImage.enabled = true;
                Debug.Log($"⚪ Pip{i} set to WHITE (i={i} > displayLevel={displayLevel})");
            }
            else
            {
                pipImage.enabled = false;
                Debug.Log($"🚫 Pip{i} disabled (i={i} > maxLevel={maxLevel})");
            }
        }

        Debug.Log($"Found {foundPips} pips, set {pipsFoundAndSet} to RED");
        Debug.Log($"=== End pips update for {abilityId} ===");

        if (foundPips == 0)
        {
            Debug.LogWarning($"⚠️ NO PIPS FOUND for {abilityId}! Check hierarchy.");
            Debug.Log($"Bubble children: {GetAllChildNames(bubble.transform)}");
        }
    }

    string GetAllChildNames(Transform parent)
    {
        List<string> names = new List<string>();
        foreach (Transform child in parent)
        {
            names.Add(child.name);
        }
        return string.Join(", ", names);
    }

    void UpdateBubbleVisibility(string abilityId, bool visible)
    {
        if (abilityId == WAVE_ID && waveAbility != null)
        {
            if (waveAbility.activeSelf != visible)
                waveAbility.SetActive(visible);
            return;
        }

        if (abilityId == SPIRAL_ID && spiralAbility != null)
        {
            if (spiralAbility.activeSelf != visible)
                spiralAbility.SetActive(visible);
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
        else if (abilityId == SPIRAL_ID)
        {
            if (spiralAbility != null)
            {
                spiralAbility.SetActive(true);
                Debug.Log($"✅ {spiralAbility.name} activated on unlock!");
            }
            else
            {
                Debug.LogError("❌ SpiralAbility reference is null! Make sure it's a child of BubbleContainer.");
            }
        }

        UpdateBubbleVisibility(abilityId, true);
        UpdateAllBubbles();

        Debug.Log("Unlocked " + abilityId);
    }

    public void LevelUpAbility(string abilityId)
    {
        Debug.Log($"=== LevelUpAbility called for {abilityId} ===");

        if (!cooldownStates.ContainsKey(abilityId))
        {
            Debug.LogWarning($"❌ Ability {abilityId} not found in cooldownStates!");
            return;
        }

        CooldownState state = cooldownStates[abilityId];

        if (!state.isUnlocked)
        {
            Debug.LogWarning($"❌ Ability {abilityId} is not unlocked!");
            return;
        }

        // Update the level from playerStats
        if (abilityId == FIREBALL_ID)
        {
            state.currentLevel = playerStats.fireballLevel;
            Debug.Log($"🔄 Fireball Level updated to: {state.currentLevel}");
        }
        else if (abilityId == WAVE_ID)
        {
            state.currentLevel = playerStats.waveLevel;
            Debug.Log($"🔄 Wave Level updated to: {state.currentLevel}");
        }

        UpdateAllBubbles();

        Debug.Log($"✅ {state.abilityName} Level {state.currentLevel} - Pips updated!");
        Debug.Log($"=== End LevelUpAbility for {abilityId} ===");
    }

    public void RefreshAllBubbles()
    {
        if (waveAbility != null && waveAbility.activeSelf != playerStats.hasWaveAttack)
        {
            waveAbility.SetActive(playerStats.hasWaveAttack);
        }

        if (spiralAbility != null && spiralAbility.activeSelf != playerStats.hasSpiralAttack)
        {
            spiralAbility.SetActive(playerStats.hasSpiralAttack);
        }

        if (cooldownStates.ContainsKey(FIREBALL_ID))
        {
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
        }

        UpdateBubbleVisibility(FIREBALL_ID, true);
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