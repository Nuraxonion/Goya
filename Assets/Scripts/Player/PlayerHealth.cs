using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public float maxHealth = 100f;
    public float currentHealth;

    public float damagePerSecond = 10f;

    private int enemiesTouching = 0;

    public GameOverManager gameOverManager;

    void Start()
    {
        currentHealth = maxHealth;
    }

    void Update()
    {
        if (enemiesTouching > 0)
        {
            //Debug.Log(Time.deltaTime);
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

        gameOverManager.ShowGameOver();
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }
    }

    public void Heal(float amount)
    {
        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        //Debug.Log("Touched: " + other.name);

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

    public void IncreaseMaxHealth(float amount)
    {
        maxHealth += amount;

        currentHealth += amount;

        currentHealth =
            Mathf.Clamp(
                currentHealth,
                0,
                maxHealth);
    }
}