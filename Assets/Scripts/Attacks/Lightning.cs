using System.Collections.Generic;
using UnityEngine;

// One cast is one aimed blast where the gesture was drawn, plus lightningExtraStrikes
// scattered around it. The aimed blast is the only one that grows with upgrades, and the
// only one that feeds the chain: enemies it hits that were ALREADY stunned zap the nearest
// un-stunned enemy.
//
// Cast takes the position rather than reading input, because the caster holds it fixed for
// the attack's whole duration - the bolt does not chase the cursor.
public class LightningAttack : MonoBehaviour
{
    [Header("Visual Effect")]
    [SerializeField] private GameObject lightningEffectPrefab;

    [Header("Extra Strike Placement")]

    [Tooltip("Closest an extra strike can land to the aimed blast, in world units.")]
    [SerializeField] private float minStrikeDistance = 2.5f;

    [Tooltip("Furthest an extra strike can land from the aimed blast, in world units.")]
    [SerializeField] private float maxStrikeDistance = 5f;

    // Rejection sampling: a spot must be on screen, off the corners and clear of every
    // blast already placed this cast. Bounded so a cramped screen can never spin here.
    private const int PLACEMENT_ATTEMPTS = 20;

    private PlayerStats playerStats;
    private EnemySpawner enemySpawner;

    // Reused every cast so a cast allocates nothing beyond the physics query.
    private readonly List<Vector2> extraPositions = new List<Vector2>();
    private readonly List<EnemyHealth> preStunned = new List<EnemyHealth>();
    private readonly HashSet<EnemyHealth> hitThisStrike = new HashSet<EnemyHealth>();

    // Where the last blast actually landed, so the gizmo can show the real hit area
    // instead of a circle around the player.
    private Vector2 lastCastPosition;
    private float lastAimedRadius;
    private float lastExtraRadius;
    private bool hasCast;

    private void Awake()
    {
        playerStats = GetComponent<PlayerStats>();

        if (playerStats == null)
        {
            Debug.LogError("LightningAttack: PlayerStats not found!");
        }

        // Same source PlayerAttack.FindNearestEnemies reads - the spawner's live list,
        // rather than a scene-wide scan per chain hop.
        enemySpawner = FindObjectOfType<EnemySpawner>();
    }

    public void Cast(Vector2 castPosition, float damageMultiplier = 1f)
    {
        if (playerStats == null)
            return;

        float aimedRadius =
            playerStats.lightningRadius * playerStats.lightningAimedRadiusMultiplier;

        float extraRadius = playerStats.lightningRadius;

        lastCastPosition = castPosition;
        lastAimedRadius = aimedRadius;
        lastExtraRadius = extraRadius;
        hasCast = true;

        // Aimed blast first, recording who was already stunned when it landed.
        preStunned.Clear();
        StrikeAt(castPosition, aimedRadius, damageMultiplier, preStunned);

        // Then the scattered ones, which never grow and never feed the chain.
        PickStrikePositions(
            castPosition,
            aimedRadius,
            extraRadius,
            playerStats.lightningExtraStrikes
        );

        for (int i = 0; i < extraPositions.Count; i++)
        {
            StrikeAt(extraPositions[i], extraRadius, damageMultiplier, null);
        }

        if (playerStats.lightningChainFromStunned)
        {
            ChainFromStunned();
        }
    }

    // One blast. preStunnedOut collects the enemies that were stunned BEFORE this blast
    // stunned them; pass null for strikes that do not feed the chain.
    private void StrikeAt(
        Vector2 position,
        float radius,
        float damageMultiplier,
        List<EnemyHealth> preStunnedOut)
    {
        SpawnEffect(position, radius);

        // Find every collider inside the lightning radius
        Collider2D[] hits = Physics2D.OverlapCircleAll(position, radius);

        // An enemy with more than one collider used to be damaged once per collider.
        hitThisStrike.Clear();

        foreach (Collider2D hit in hits)
        {
            EnemyHealth enemyHealth =
                hit.GetComponentInParent<EnemyHealth>();

            if (enemyHealth == null)
                continue;

            if (!hitThisStrike.Add(enemyHealth))
                continue;

            // Stun. One lookup covers every movement type - this used to be an
            // if/else-if over the two concrete scripts, so any new enemy was
            // silently unstunnable.
            EnemyMovement movement =
                hit.GetComponentInParent<EnemyMovement>();

            // Read before the Stun below lands, or every enemy this blast touches
            // would look already-stunned to the chain.
            if (preStunnedOut != null && movement != null && movement.IsStunned)
                preStunnedOut.Add(enemyHealth);

            // Damage. Unupgraded lightning does none - it is a pure stun until the
            // Thunderclap upgrade lands - and TakeDamage always pops a number, flashes
            // the sprite and knocks the enemy back, so a zero hit has to be skipped
            // rather than dealt.
            float damage =
                playerStats.lightningDamage * damageMultiplier;

            if (damage > 0f)
                enemyHealth.TakeDamage(damage);

            if (movement != null)
            {
                movement.Stun(
                    playerStats.lightningStunDuration
                );
            }
        }
    }

    // Final upgrade: every already-stunned enemy the aimed blast hit arcs to the nearest
    // enemy that is still mobile. Flat damage - deliberately not scaled by the gesture
    // accuracy multiplier, since it is a fixed bonus rather than part of the blast.
    private void ChainFromStunned()
    {
        for (int i = 0; i < preStunned.Count; i++)
        {
            EnemyHealth source = preStunned[i];

            // Died to the blast that would have triggered the arc.
            if (source == null)
                continue;

            EnemyHealth victim = FindNearestUnstunned(
                source.transform.position,
                source
            );

            if (victim != null)
                victim.TakeDamage(playerStats.lightningChainDamage);
        }
    }

    private EnemyHealth FindNearestUnstunned(Vector2 origin, EnemyHealth exclude)
    {
        if (enemySpawner == null)
            return null;

        List<EnemyHealth> active = enemySpawner.activeEnemies;

        EnemyHealth nearest = null;
        float nearestSqr = float.MaxValue;

        for (int i = 0; i < active.Count; i++)
        {
            EnemyHealth candidate = active[i];

            // The list keeps entries for enemies destroyed this frame.
            if (candidate == null || candidate == exclude)
                continue;

            EnemyMovement movement = candidate.GetComponent<EnemyMovement>();

            if (movement != null && movement.IsStunned)
                continue;

            float sqr = ((Vector2)candidate.transform.position - origin).sqrMagnitude;

            if (sqr < nearestSqr)
            {
                nearestSqr = sqr;
                nearest = candidate;
            }
        }

        return nearest;
    }

    // Picks count spots on a ring around the aimed blast, so the whole cast reads as one
    // storm centred where the gesture was drawn instead of scattering across the screen.
    private void PickStrikePositions(
        Vector2 aimedPosition,
        float aimedRadius,
        float radius,
        int count)
    {
        extraPositions.Clear();

        if (count <= 0)
            return;

        GetPlayfieldBounds(out Vector2 center, out float halfWidth, out float halfHeight);

        // Inset by the blast radius so the whole circle stays on screen.
        float a = Mathf.Max(0.1f, halfWidth - radius);
        float b = Mathf.Max(0.1f, halfHeight - radius);

        Vector2 origin = aimedPosition;

        for (int i = 0; i < count; i++)
        {
            Vector2 candidate = origin;
            bool accepted = false;

            for (int attempt = 0; attempt < PLACEMENT_ATTEMPTS; attempt++)
            {
                float angle = Random.value * Mathf.PI * 2f;
                float distance = Random.Range(minStrikeDistance, maxStrikeDistance);

                candidate = origin + new Vector2(
                    Mathf.Cos(angle),
                    Mathf.Sin(angle)
                ) * distance;

                if (!IsOnPlayfield(candidate, center, a, b))
                    continue;

                if (Overlaps(candidate, radius, aimedPosition, aimedRadius))
                    continue;

                accepted = true;
                break;
            }

            // Out of attempts (screen full of blasts, or a very cramped view): keep the
            // strike rather than dropping it, but force it back on screen.
            if (!accepted)
            {
                candidate.x = center.x + Mathf.Clamp(candidate.x - center.x, -a, a);
                candidate.y = center.y + Mathf.Clamp(candidate.y - center.y, -b, b);
            }

            extraPositions.Add(candidate);
        }
    }

    // The inscribed ellipse rather than the screen rectangle: it is what keeps strikes off
    // the corners, since the corners are exactly the parts of the rectangle it excludes.
    private bool IsOnPlayfield(Vector2 point, Vector2 center, float a, float b)
    {
        float x = (point.x - center.x) / a;
        float y = (point.y - center.y) / b;

        return x * x + y * y <= 1f;
    }

    private bool Overlaps(
        Vector2 candidate,
        float radius,
        Vector2 aimedPosition,
        float aimedRadius)
    {
        if (Vector2.Distance(candidate, aimedPosition) < radius + aimedRadius)
            return true;

        for (int i = 0; i < extraPositions.Count; i++)
        {
            if (Vector2.Distance(candidate, extraPositions[i]) < radius * 2f)
                return true;
        }

        return false;
    }

    // Same camera maths EnemySpawner uses, with HairMoveScript.GetRoamBounds' fallback so
    // a missing main camera degrades to a sane play area instead of stacking every strike
    // on the origin.
    private void GetPlayfieldBounds(out Vector2 center, out float halfWidth, out float halfHeight)
    {
        Camera cam = Camera.main;

        if (cam == null)
        {
            center = Vector2.zero;
            halfWidth = 8f;
            halfHeight = 5f;
            return;
        }

        center = cam.transform.position;
        halfHeight = cam.orthographicSize;
        halfWidth = halfHeight * cam.aspect;
    }

    private void SpawnEffect(Vector2 position, float radius)
    {
        if (lightningEffectPrefab == null)
            return;

        GameObject effect = Instantiate(
            lightningEffectPrefab,
            position,
            Quaternion.identity
        );

        // The aimed blast can be much wider than the extra ones once upgraded, so the
        // visual has to say so. lightningRadius is the un-upgraded size - no upgrade
        // touches it, the aimed multiplier is applied on top of it.
        if (playerStats.lightningRadius > 0f)
            effect.transform.localScale *= radius / playerStats.lightningRadius;

        Destroy(effect, 1f);
    }

    // Yellow marks where the last aimed blast landed - the centre of the drawn gesture,
    // never the player - and magenta the extra strikes that scattered around it.
    private void OnDrawGizmosSelected()
    {
        // There used to be a cyan circle tracking the cursor here. The cursor no longer
        // aims anything, so it drew a hit area that could never be hit.
        if (hasCast)
        {
            Gizmos.color = Color.yellow;

            Gizmos.DrawWireSphere(
                lastCastPosition,
                lastAimedRadius
            );

            Gizmos.color = Color.magenta;

            for (int i = 0; i < extraPositions.Count; i++)
            {
                Gizmos.DrawWireSphere(
                    extraPositions[i],
                    lastExtraRadius
                );
            }
        }
    }
}
