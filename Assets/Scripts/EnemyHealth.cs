using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    public EnemySpawner spawner;

    public float health = 3;

    void OnMouseDown()
    {
        TakeDamage(1);
    }

    public void TakeDamage(float amount)
    {
        health -= amount;

        if (health <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        if (spawner != null)
        {
            spawner.OnEnemyKilled();
        }

        Destroy(gameObject);
    }
}