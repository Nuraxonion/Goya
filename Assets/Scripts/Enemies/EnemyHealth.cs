using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    public EnemySpawner spawner;
    public GameObject objectToSpawn;

    public float health = 3;
    public float damage = 1;

    public Material whiteMaterial;

    private SpriteRenderer spriteRenderer;
    private Material originalMaterial;

    void Start()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        originalMaterial = spriteRenderer.material;
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

        Destroy(gameObject);
        Instantiate(objectToSpawn, transform.position, Quaternion.identity);
    }
}