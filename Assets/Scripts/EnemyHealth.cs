using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    public EnemySpawner spawner;

    public int health = 3;

    void OnMouseDown()
    {
        TakeDamage(1);
    }

    public void TakeDamage(int amount)
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