using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public float maxHealth = 100f;
    public float currentHealth;

    //public float damagePerSecond = 10f;

    //private int enemiesTouching = 0;
    private bool isDead = false;

    public GameOverManager gameOverManager;

    private const string HEALTH_UPGRADE_KEY = "HealthUpgradeCount";
    private const string MAX_HEALTH_KEY = "MaxHealth";

    void Start()
    {
        LoadHealthData();
        currentHealth = maxHealth;
    }

    private void LoadHealthData()
    {
        int upgradeCount = PlayerPrefs.GetInt(HEALTH_UPGRADE_KEY, 0);
        float savedMaxHealth = PlayerPrefs.GetFloat(MAX_HEALTH_KEY, 100f);

        if (upgradeCount == 0)
        {
            maxHealth = 100f;
        }
        else
        {
            maxHealth = savedMaxHealth;
        }

        maxHealth = Mathf.Max(maxHealth, 100f);
    }

    /*
    void Update()
    {
        if (isDead) return;

        if (enemiesTouching > 0)
        {
            currentHealth -= damagePerSecond * Time.deltaTime;

            if (currentHealth <= 0)
            {
                Die();
            }
        }
    }
    */

    void Die()
    {
        if (isDead) return;
        isDead = true;
        currentHealth = 0;

        Debug.Log("Player died");

        gameOverManager.ShowGameOver();
    }

    public void TakeDamage(float damage)
    {
        if (isDead) return;

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

    /*
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            enemiesTouching++;
        }
    }
    */

    /*
    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            enemiesTouching--;
        }
    }
    */

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