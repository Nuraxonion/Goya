using UnityEngine;

public class LightningAttack : MonoBehaviour
{
    [Header("Visual Effect")]
    [SerializeField] private GameObject lightningEffectPrefab;

    private PlayerStats playerStats;

    private void Awake()
    {
        playerStats = GetComponent<PlayerStats>();

        if (playerStats == null)
        {
            Debug.LogError("LightningAttack: PlayerStats not found!");
        }
    }

    public void Cast(Vector2 castPosition, float damageMultiplier = 1f)
    {
        if (playerStats == null)
            return;

        // Spawn visual effect
        if (lightningEffectPrefab != null)
        {
            GameObject effect = Instantiate(
                lightningEffectPrefab,
                castPosition,
                Quaternion.identity
            );

            Destroy(effect, 1f);
        }

        // Find every collider inside the lightning radius
        Collider2D[] hits = Physics2D.OverlapCircleAll(
            castPosition,
            playerStats.lightningRadius
        );

        foreach (Collider2D hit in hits)
        {
            EnemyHealth enemyHealth =
                hit.GetComponentInParent<EnemyHealth>();

            if (enemyHealth == null)
                continue;

            // Damage
            float damage =
                playerStats.lightningDamage * damageMultiplier;

            enemyHealth.TakeDamage(damage);

            // Stun. One lookup covers every movement type - this used to be an
            // if/else-if over the two concrete scripts, so any new enemy was
            // silently unstunnable.
            EnemyMovement movement =
                hit.GetComponentInParent<EnemyMovement>();

            if (movement != null)
            {
                movement.Stun(
                    playerStats.lightningStunDuration
                );
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        PlayerStats stats = GetComponent<PlayerStats>();

        if (stats == null)
            return;

        Gizmos.DrawWireSphere(
            transform.position,
            stats.lightningRadius
        );
    }
}