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

    //private CursorManager cursorManager;
    public CursorController cursorController;

    //public Slider durationSlider;
    //public GameObject sliderPanel;

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

    // Attack ids in cast order, most recent first. The dictionaries above are unordered,
    // so this is the only thing that can answer "which attack was cast before this one" -
    // which is exactly what OP Multi-Tasking needs.
    private readonly List<string> castOrder = new List<string>();

    private void Start()
    {
        cursorController = FindFirstObjectByType<CursorController>();
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
    // except under OP Multi-Tasking, where the one refreshed attack adopts it.
    public void StartAttackTimer(string attackId, float multiplier, Vector2 castPosition)
    {
        if (string.IsNullOrEmpty(attackId))
            return;

        // Guarded because no scene currently contains a CursorManager, so this is null.
        // Unguarded it threw here - before the attack was ever registered below - which
        // silently stopped every duration-based attack from firing at all. A cosmetic
        // cursor swap must never be able to block a cast.
        /*if (cursorController != null)
            cursorController.ShowDuration(1f);
        */

        Debug.Log($"🎯 StartAttackTimer called for: {attackId}");

        // Without Multi-Tasking a new cast replaces whatever was running.
        if (playerStats == null || !playerStats.hasMultiTasking)
        {
            remaining.Clear();
            maxima.Clear();
            multipliers.Clear();
            castPositions.Clear();
            castOrder.Clear();
        }

        // OP Multi-Tasking: a successful cast tops up the attack cast before it, so
        // alternating two gestures keeps that pair alive. Only the previous one - the
        // attack being cast gets its own full duration below anyway, and together they
        // are the "last 2". Anything older keeps ticking down, so at most two attacks
        // are ever sustained. Only recognised, available attacks reach this method, so
        // a miss refreshes nothing. The refreshed attack adopts this gesture's accuracy.
        //
        // Runs before the new cast is pushed onto castOrder below, or it would find
        // itself as its own "previous".
        if (playerStats != null && playerStats.hasOpMultiTasking)
        {
            for (int i = 0; i < castOrder.Count; i++)
            {
                string id = castOrder[i];

                if (id == attackId || !remaining.ContainsKey(id))
                    continue;

                remaining[id] = maxima[id];
                multipliers[id] = multiplier;
                break;
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

        if (cursorController != null)
            cursorController.StartDuration();

        remaining[attackId] = maxTime;
        maxima[attackId] = maxTime;
        multipliers[attackId] = multiplier;
        castPositions[attackId] = castPosition;
        mostRecentAttack = attackId;

        // Remove-then-insert rather than a plain insert, so recasting a running attack
        // moves it to the front instead of leaving a stale duplicate further down.
        castOrder.Remove(attackId);
        castOrder.Insert(0, attackId);

        /*
        if (sliderPanel != null)
            sliderPanel.SetActive(true);

        if (durationSlider != null)
            durationSlider.maxValue = 1f;

        */
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
        // Same guard as in StartAttackTimer - see the note there.

        remaining.Remove(attackId);
        maxima.Remove(attackId);
        multipliers.Remove(attackId);
        castPositions.Remove(attackId);
        castOrder.Remove(attackId);

        // Hand the bar over to whatever is still running. castOrder is most-recent-first,
        // so this picks the genuinely newest survivor - the dictionary walk this replaced
        // returned an arbitrary one, which the bar is documented not to do.
        if (mostRecentAttack == attackId)
        {
            mostRecentAttack = AttackIds.None;

            for (int i = 0; i < castOrder.Count; i++)
            {
                if (remaining.ContainsKey(castOrder[i]))
                {
                    mostRecentAttack = castOrder[i];
                    break;
                }
            }
        }

        UpdateSlider();

        if (remaining.Count == 0 && cursorController != null)
            cursorController.EndDuration();
    }

    private void UpdateSlider()
    {
        float normalizedDuration = 0f;

        if (!string.IsNullOrEmpty(mostRecentAttack)
            && remaining.TryGetValue(mostRecentAttack, out float left)
            && maxima.TryGetValue(mostRecentAttack, out float max)
            && max > 0f)
        {
            normalizedDuration = Mathf.Clamp01(left / max);
        }

        // Existing duration slider
        /*
        if (durationSlider != null)
        {
            durationSlider.value = normalizedDuration;
        }
        */

        // NEW: radial cursor duration
        if (cursorController != null)
        {
            cursorController.SetDuration(normalizedDuration);
        }
    }

    private void ClearAll()
    {
        remaining.Clear();
        maxima.Clear();
        multipliers.Clear();
        castPositions.Clear();
        castOrder.Clear();
        mostRecentAttack = AttackIds.None;

        /*
        if (durationSlider != null)
            durationSlider.value = 0f;

        // Mirrors the SetActive(true) in StartAttackTimer. Without this the bar
        // stayed on screen permanently after the first cast.
        if (sliderPanel != null)
            sliderPanel.SetActive(false);
        */

        if (cursorController != null)
            cursorController.EndDuration();

        if (gestureManager != null)
            gestureManager.currentAttack = AttackIds.None;

    }
}
