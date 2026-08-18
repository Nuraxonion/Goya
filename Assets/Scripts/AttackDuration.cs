using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Owns which attacks are currently active and how long each has left.
//
// Without the Multi-Tasking upgrade only one attack is ever registered at a
// time (casting a new one clears the previous), which matches the original
// single-attack behaviour. With Multi-Tasking each cast attack keeps ticking
// down its own timer, so several can run at once.
public class AttackDuration : MonoBehaviour
{
    public PlayerStats playerStats;
    public GestureManager gestureManager;

    private CursorManager cursorManager;

    public Slider durationSlider;
    public GameObject sliderPanel;

    // attackId -> seconds left / full duration / multiplier captured at cast time.
    private readonly Dictionary<string, float> remaining = new Dictionary<string, float>();
    private readonly Dictionary<string, float> maxima = new Dictionary<string, float>();
    private readonly Dictionary<string, float> multipliers = new Dictionary<string, float>();

    // Where the gesture that cast each attack was drawn, in world space. Lightning
    // lands here for its whole duration instead of chasing the cursor.
    private readonly Dictionary<string, Vector2> castPositions = new Dictionary<string, Vector2>();

    // The single duration bar follows whichever attack was cast most recently.
    private string mostRecentAttack = AttackIds.None;

    // Reused each frame so ticking doesn't allocate.
    private readonly List<string> activeIds = new List<string>();
    private readonly List<string> expiredIds = new List<string>();

    // Separate from activeIds, which is owned by Update().
    private readonly List<string> refreshIds = new List<string>();

    private void Start()
    {
        cursorManager = FindFirstObjectByType<CursorManager>();
    }

    void Update()
    {
        if (remaining.Count == 0)
            return;

        activeIds.Clear();
        activeIds.AddRange(remaining.Keys);

        expiredIds.Clear();

        for (int i = 0; i < activeIds.Count; i++)
        {
            string id = activeIds[i];
            float left = remaining[id] - Time.deltaTime;

            if (left <= 0f)
                expiredIds.Add(id);
            else
                remaining[id] = left;
        }

        for (int i = 0; i < expiredIds.Count; i++)
        {
            Debug.Log($"⏱️ Duration ended for {expiredIds[i]} - attack cleared!");
            Deactivate(expiredIds[i]);
        }

        UpdateSlider();

        if (remaining.Count == 0)
            ClearAll();
    }

    // Registers an attack as active (or refreshes it if it already is).
    // multiplier is the gesture-accuracy bonus from the stroke that cast it, so
    // each attack keeps its own instead of inheriting the newest gesture's -
    // except under OP Multi-Tasking, where a refresh restamps them all.
    public void StartAttackTimer(string attackId, float multiplier, Vector2 castPosition)
    {
        if (string.IsNullOrEmpty(attackId))
            return;

        cursorManager.ShowDurationCursor();

        Debug.Log($"🎯 StartAttackTimer called for: {attackId}");

        // Without Multi-Tasking a new cast replaces whatever was running.
        if (playerStats == null || !playerStats.hasMultiTasking)
        {
            remaining.Clear();
            maxima.Clear();
            multipliers.Clear();
            castPositions.Clear();
        }

        // OP Multi-Tasking: any successful cast tops every already-running attack
        // back up to full, so alternating gestures keeps them all alive. Only
        // recognised, available attacks reach this method, so a miss refreshes
        // nothing. Refreshed attacks adopt this gesture's accuracy multiplier.
        if (playerStats != null && playerStats.hasOpMultiTasking)
        {
            refreshIds.Clear();
            refreshIds.AddRange(remaining.Keys);

            for (int i = 0; i < refreshIds.Count; i++)
            {
                string id = refreshIds[i];

                remaining[id] = maxima[id];
                multipliers[id] = multiplier;
            }
        }

        float maxTime;

        switch (attackId)
        {
            case AttackIds.Fireball:
                maxTime = playerStats.fireballDuration;
                break;

            case AttackIds.Wave:
                maxTime = playerStats.waveDuration;
                break;

            case AttackIds.Lightning:
                maxTime = playerStats.lightningDuration;
                break;

            default:
                maxTime = playerStats.fireballDuration;
                break;
        }

        remaining[attackId] = maxTime;
        maxima[attackId] = maxTime;
        multipliers[attackId] = multiplier;
        castPositions[attackId] = castPosition;
        mostRecentAttack = attackId;

        if (sliderPanel != null)
            sliderPanel.SetActive(true);

        if (durationSlider != null)
            durationSlider.maxValue = 1f;

        UpdateSlider();
    }

    public bool IsActive(string attackId)
    {
        return !string.IsNullOrEmpty(attackId) && remaining.ContainsKey(attackId);
    }

    public float GetMultiplier(string attackId)
    {
        if (!string.IsNullOrEmpty(attackId) && multipliers.TryGetValue(attackId, out float value))
            return value;

        return 1f;
    }

    // Where the gesture that started this attack was drawn. Try-shaped rather than
    // returning a default the way GetMultiplier does: there is no harmless fallback
    // position, since Vector2.zero is a real spot in the world, so the caller has to
    // pick its own.
    public bool TryGetCastPosition(string attackId, out Vector2 position)
    {
        if (!string.IsNullOrEmpty(attackId))
            return castPositions.TryGetValue(attackId, out position);

        position = Vector2.zero;
        return false;
    }

    private void Deactivate(string attackId)
    {
        cursorManager.ShowNormalCursor();
        remaining.Remove(attackId);
        maxima.Remove(attackId);
        multipliers.Remove(attackId);
        castPositions.Remove(attackId);

        // Hand the bar over to whatever is still running.
        if (mostRecentAttack == attackId)
        {
            mostRecentAttack = AttackIds.None;

            foreach (string id in remaining.Keys)
            {
                mostRecentAttack = id;
                break;
            }
        }
    }

    private void UpdateSlider()
    {
        if (durationSlider == null)
            return;

        if (!string.IsNullOrEmpty(mostRecentAttack)
            && remaining.TryGetValue(mostRecentAttack, out float left)
            && maxima.TryGetValue(mostRecentAttack, out float max)
            && max > 0f)
        {
            durationSlider.value = Mathf.Clamp01(left / max);
        }
        else
        {
            durationSlider.value = 0f;
        }
    }

    private void ClearAll()
    {
        remaining.Clear();
        maxima.Clear();
        multipliers.Clear();
        castPositions.Clear();
        mostRecentAttack = AttackIds.None;

        if (durationSlider != null)
            durationSlider.value = 0f;

        // Mirrors the SetActive(true) in StartAttackTimer. Without this the bar
        // stayed on screen permanently after the first cast.
        if (sliderPanel != null)
            sliderPanel.SetActive(false);

        if (gestureManager != null)
            gestureManager.currentAttack = AttackIds.None;
    }
}
