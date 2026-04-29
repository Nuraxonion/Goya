using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public float maxHealth = 100f;
    public float currentHealth;

    public float damagePerSecond = 10f;

    private int enemiesTouching = 0;

    void Start()
    {
        currentHealth = maxHealth;
    }

    void Update()
    {
        if (enemiesTouching > 0)
        {
            currentHealth -= damagePerSecond * Time.deltaTime;

            if (currentHealth <= 0)
            {
                Die();
            }
        }
    }

    void Die()
    {
        Debug.Log("Player died");
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("Touched: " + other.name);

        if (other.CompareTag("Enemy"))
        {
            enemiesTouching++;
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            enemiesTouching--;
        }
    }
}