using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    public EnemySpawner spawner;
    public GameObject objectToSpawn;

    public float health = 3;
    private float maxHealth;
    public float damage = 100;

    [Tooltip("True = kamikaze, the enemy lands one hit and dies the moment it touches the player. False = it survives and can hit again after contactCooldown.")]
    public bool dieOnContact = true;

    [Tooltip("Minimum seconds between contact hits for enemies that don't die on contact.")]
    public float contactCooldown = 0.5f;

    [Header("Reward")]
    public float xpValue = 10f;

    [Tooltip("Travel speed given to the dropped XP orb. Overrides the prefab value.")]
    public float xpOrbSpeed = 6f;

    public Material whiteMaterial;

    private SpriteRenderer spriteRenderer;
    private Material originalMaterial;
    private float nextContactTime = 0f;

    void Start()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        originalMaterial = spriteRenderer.material;
        maxHealth = health;
    }

    public void TakeDamage(float damage)
    {
        health -= damage;

        DamagePopupManager.Instance.ShowDamage(
            damage,
            transform.position + Vector3.up * 0.8f
        );

        StartCoroutine(FlashWhite());
        
        EnemyMovement movement = GetComponent<EnemyMovement>();

        Vector2 knockbackDir = Vector2.right;

        if (movement != null)
            knockbackDir = movement.GetKnockbackDirection();

        StartCoroutine(Knockback(knockbackDir));

        if (health <= 0)
            Die();

        
    }

    private System.Collections.IEnumerator FlashWhite()
    {
        spriteRenderer.material = whiteMaterial;
        whiteMaterial.SetFloat("_FlashAmount", 1f);
        yield return new WaitForSeconds(0.1f);
        spriteRenderer.material = originalMaterial;
    }

    private System.Collections.IEnumerator Knockback(Vector2 direction)
    {
        float elapsed = 0f;
        float duration = 0.03f;
        float distance = 0.2f;

        Vector3 startPos = transform.position;
        Vector3 targetPos = startPos + (Vector3)direction * distance;

        while (elapsed < duration)
        {
            transform.position = Vector3.Lerp(startPos, targetPos, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        elapsed = 0f;

        while (elapsed < duration)
        {
            transform.position = Vector3.Lerp(targetPos, startPos, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = startPos;
    }
    // dropXp is false when the enemy despawns because it hit the player - getting
    // hit shouldn't reward you. Only enemies the player actually kills drop an orb.
    [Header("Death Effect")]
    public GameObject deathEffectPrefab;
    public float deathEffectDuration = 0.1f;
    
    void Die(bool dropXp = true)
    {
        if (spawner != null)
        {
            spawner.activeEnemies.Remove(this);
            spawner.OnEnemyKilled();
        }

        if (dropXp && objectToSpawn != null)
        {
            GameObject orb = Instantiate(objectToSpawn, transform.position, Quaternion.identity);

            xpPoint point = orb.GetComponent<xpPoint>();
            if (point != null)
            {
                point.xpValue = xpValue;
                point.speed = xpOrbSpeed;
            }
        }
        if (deathEffectPrefab != null)
        {
            GameObject effect = Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);
            
            // localScale.x < 0 means facing right - the convention EnemyMovement
            // maintains. Any movement type qualifies, so a new enemy's death effect
            // mirrors correctly instead of always pointing left.
            bool facingRight = GetComponent<EnemyMovement>() != null
                && transform.localScale.x < 0;

            if (facingRight)
            {
                Vector3 scale = effect.transform.localScale;
                scale.x *= -1;
                effect.transform.localScale = scale;
            }
            
            Destroy(effect, deathEffectDuration);
        }
        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryContactDamage(other);
    }

    // One hit on arrival, never a per-frame drain: damage only lands on Enter.
    // For lingering enemies contactCooldown guards against re-entry spam from
    // knockback jitter; kamikaze enemies are gone before it matters.
    private void TryContactDamage(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (Time.time < nextContactTime)
            return;

        PlayerHealth player = other.GetComponent<PlayerHealth>();

        if (player == null)
            return;

        // Chipped enemies hit softer, so partial damage is never wasted.
        float healthPercentage = maxHealth > 0f ? health / maxHealth : 1f;
        float finalDamage = damage * healthPercentage;

        nextContactTime = Time.time + contactCooldown;

        player.TakeDamage(finalDamage, transform.position);

        if (dieOnContact)
            Die(false);
    }
}