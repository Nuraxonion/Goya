using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public float maxHealth = 100f;
    public float currentHealth;

    //public float damagePerSecond = 10f;

    //private int enemiesTouching = 0;
    private bool isDead = false;

    public GameOverManager gameOverManager;

    public Material flashMaterial;
    private SpriteRenderer spriteRenderer;
    private Material originalMaterial;
    
    private Vector3 originalPos;
    private Coroutine knockbackCoroutine;
    private Coroutine flashCoroutine;

    [Tooltip("Minimum seconds between hit flash / knockback reactions, so per-frame DPS doesn't thrash them.")]
    public float damageFeedbackCooldown = 0.15f;
    private float nextFeedbackTime = 0f;

    private const string HEALTH_UPGRADE_KEY = "HealthUpgradeCount";
    private const string MAX_HEALTH_KEY = "MaxHealth";

    void Start()
    {
        LoadHealthData();
        currentHealth = maxHealth;
        
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        originalMaterial = spriteRenderer.material;
        
        originalPos = transform.position;
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

    /*public void TakeDamage(float damage)
    {
        if (isDead) return;

        currentHealth -= damage;

        StartCoroutine(FlashRed());
        
        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }
    }*/
    public void TakeDamage(float damage, Vector2 sourcePosition)
    {
        if (isDead) return;
        currentHealth -= damage;

        // Continuous damage (bats draining) calls this every frame; only play the
        // reaction on a cooldown so the flash and knockback aren't restarted forever.
        if (Time.time >= nextFeedbackTime)
        {
            nextFeedbackTime = Time.time + damageFeedbackCooldown;

            Vector2 knockbackDir = ((Vector2)originalPos - sourcePosition).normalized;

            if (flashCoroutine != null) StopCoroutine(flashCoroutine);
            if (knockbackCoroutine != null) StopCoroutine(knockbackCoroutine);

            flashCoroutine = StartCoroutine(FlashRed());
            knockbackCoroutine = StartCoroutine(Knockback(knockbackDir));
        }

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }
    }

    private System.Collections.IEnumerator Knockback(Vector2 direction)
    {
        float elapsed = 0f;
        float duration = 0.03f;
        float distance = 0.15f;

        Vector3 targetPos = originalPos + (Vector3)direction * distance;
        
        while (elapsed < duration)
        {
            transform.position = Vector3.Lerp(originalPos, targetPos, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        elapsed = 0f;

        while (elapsed < duration)
        {
            transform.position = Vector3.Lerp(targetPos, originalPos, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = originalPos;
        knockbackCoroutine = null;
    }
    private System.Collections.IEnumerator FlashRed()
    {
        spriteRenderer.material = flashMaterial;
        flashMaterial.SetFloat("_FlashAmount", 1f);
        yield return new WaitForSeconds(0.1f);
        spriteRenderer.material = originalMaterial;
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