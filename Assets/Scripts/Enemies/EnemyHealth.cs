using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    public EnemySpawner spawner;
    public GameObject objectToSpawn;

    public float health = 3;
    private float maxHealth;
    public float damage = 100;

    public Material whiteMaterial;

    private SpriteRenderer spriteRenderer;
    private Material originalMaterial;

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
        
        EnemyMoveScript moveScript = GetComponent<EnemyMoveScript>();
        BatMoveScript batMoveScript = GetComponent<BatMoveScript>();

        Vector2 knockbackDir = Vector2.right;

        if (moveScript != null)
            knockbackDir = moveScript.GetKnockbackDirection();
        else if (batMoveScript != null)
            knockbackDir = batMoveScript.GetKnockbackDirection();

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
    void Die()
    {
        if (spawner != null)
        {
            spawner.activeEnemies.Remove(this);
            spawner.OnEnemyKilled();
        }

        Instantiate(objectToSpawn, transform.position, Quaternion.identity);
        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        PlayerHealth player = other.GetComponent<PlayerHealth>();

        if (player == null)
            return;

        float healthPercentage = health / maxHealth;
        float finalDamage = damage * healthPercentage;

        Debug.Log($"Enemy hit! Damage = {finalDamage}");
        player.TakeDamage(finalDamage, transform.position);
        Die();
        //player.TakeDamage(finalDamage);
        //Debug.Log($"Enemy dealt {finalDamage} damage to player. Initial damage: {damage}");

        Die();
    }
}