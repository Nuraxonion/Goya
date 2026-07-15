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
        player.TakeDamage(finalDamage);
        Die();
        //player.TakeDamage(finalDamage);
        //Debug.Log($"Enemy dealt {finalDamage} damage to player. Initial damage: {damage}");

        Die();
    }
}