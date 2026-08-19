using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Tooltip("Health before any Vitality levels are applied. The Art Shop adds to this on Start - do not bake bought health into it.")]
    public float baseMaxHealth = 100f;

    // Base plus whatever Vitality the player has bought. Set in Start.
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

    
    [Header("Sound")]
    public AudioClip[] hitSounds;
    private AudioSource audioSource;

    [Header("Health-band idle")]
    [Tooltip("Animator states are named 1/2/3 + this suffix, matching the Painter controller.")]
    public string idleStateSuffix = "Character_Idle";

    private Animator animator;
    private int currentHealthBand = -1;   // -1 forces the first update
    
    void Start()
    {
        LoadHealthData();
        currentHealth = maxHealth;
        
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        originalMaterial = spriteRenderer.material;
        
        originalPos = transform.position;
        audioSource = GetComponent<AudioSource>();
        animator = GetComponent<Animator>();

        UpdateHealthAnimation();
    }

    // Derived from the purchased level every run rather than read back from a saved
    // total, so the shop and the player can never disagree about how much health a
    // level is worth.
    private void LoadHealthData()
    {
        maxHealth =
            baseMaxHealth + MetaUpgrades.GetTotalValue(MetaUpgradeIds.Vitality);

        maxHealth = Mathf.Max(maxHealth, baseMaxHealth);
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
            PlayHitSound();
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

        UpdateHealthAnimation();
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

        UpdateHealthAnimation();
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

        UpdateHealthAnimation();
    }

    /// <summary>
    /// 1 = healthy (> 2/3), 2 = hurt (1/3 .. 2/3), 3 = critical (<= 1/3).
    /// The single definition of the thresholds: the player's idle, every enemy's
    /// walk cycle and the soundtrack all band off the player's health, so their
    /// artwork flips on the same hit.
    /// </summary>
    public int CurrentHealthBand
    {
        get
        {
            if (maxHealth <= 0f) return 1;

            float ratio = currentHealth / maxHealth;

            if (ratio > 2f / 3f)      return 1;
            else if (ratio > 1f / 3f) return 2;
            else                      return 3;
        }
    }

    //   band 1 -> 1Character_Idle, band 2 -> 2Character_Idle, band 3 -> 3Character_Idle
    private void UpdateHealthAnimation()
    {
        if (animator == null) return;

        int band = CurrentHealthBand;

        if (band == currentHealthBand) return;
        currentHealthBand = band;

        // All three idle clips share a length, so carrying the playback position
        // over swaps only the artwork - the idle never hitches back to frame 0.
        float t = animator.GetCurrentAnimatorStateInfo(0).normalizedTime % 1f;
        animator.Play(band + idleStateSuffix, 0, t);
    }
    private void PlayHitSound()
    {
        if (hitSounds == null || hitSounds.Length == 0 || audioSource == null) return;
        AudioClip clip = hitSounds[Random.Range(0, hitSounds.Length)];
        audioSource.pitch = Random.Range(0.85f, 1.15f);
        audioSource.PlayOneShot(clip);
    }
}