using UnityEngine;

// Authored base values for the lightning attack, kept out of the scene so they can be
// tuned from one asset instead of by hunting through the Player's PlayerStats component.
//
// PlayerStats copies these onto its runtime lightningX fields in Awake, before any level
// upgrade can apply its multipliers. That keeps a clean split: this asset is the baseline,
// PlayerStats holds the live, upgraded values.
[CreateAssetMenu(fileName = "LightningConfig", menuName = "Goya/Lightning Config")]
public class LightningConfig : ScriptableObject
{
    [Header("Lightning Base Values")]

    [Tooltip("Area of effect - radius in world units of the blast around the cast point.")]
    [Min(0f)] public float areaOfEffect = 1f;

    [Tooltip("Base damage dealt to every enemy inside the area of effect, before the gesture-accuracy multiplier. Zero by default: unupgraded lightning only stuns, and the Thunderclap upgrade is what gives it a damage number.")]
    [Min(0f)] public float baseDamage = 0f;

    [Tooltip("Attack cooldown - seconds between casts while lightning is active.")]
    [Min(0f)] public float attackCooldown = 1f;

    [Tooltip("How long a hit enemy stays stunned, in seconds.")]
    [Min(0f)] public float stunDuration = 2f;

    [Tooltip("How long one gesture keeps lightning firing, in seconds.")]
    [Min(0f)] public float activeDuration = 5f;

    // Seeds the runtime stats. Note the rename: PlayerStats.lightningCastSpeed is really a
    // cooldown in seconds (PlayerAttack resets its timer from it), so it is exposed here
    // under the name it actually behaves as.
    public void ApplyTo(PlayerStats stats)
    {
        if (stats == null)
            return;

        stats.lightningRadius = areaOfEffect;
        stats.lightningDamage = baseDamage;
        stats.lightningCastSpeed = attackCooldown;
        stats.lightningStunDuration = stunDuration;
        stats.lightningDuration = activeDuration;
    }

#if UNITY_EDITOR
    // Live tuning: editing a value in the inspector during Play mode retunes lightning
    // immediately instead of needing a restart. This re-seeds from the baseline, so it also
    // clears any level-up multipliers earned so far in that run - acceptable for a tuning knob.
    private void OnValidate()
    {
        if (!Application.isPlaying)
            return;

        ApplyTo(FindFirstObjectByType<PlayerStats>());
    }
#endif
}
