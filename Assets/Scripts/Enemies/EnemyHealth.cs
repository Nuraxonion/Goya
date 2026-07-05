using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    public EnemySpawner spawner;
    public GameObject objectToSpawn;

    public float health = 3;
    public float damage = 1;

    public void TakeDamage(float damage)
    {
        health -= damage;

        DamagePopupManager.Instance.ShowDamage(
            damage,
            transform.position + Vector3.up * 0.8f
        );

        if (health <= 0)
            Die();
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