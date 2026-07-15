using UnityEngine;

public class WaveAttack : MonoBehaviour
{
    public float maxSize = 8f;
    // Fixed time (seconds) for the wave to grow from its start size to maxSize,
    // regardless of radius upgrades. growSpeed is derived from this in Start().
    public float growDuration = 0.5f;
    private float growSpeed;
    public float damage = 1f;

    public GameObject waveAnimationPrefab;

    private WaveAnimation animationEffect;
    public bool isActive = false;

    private Vector3 startScale;

    // Set via Initialize from the wave weapon-skill stats.
    private bool hasPushback;
    private float pushbackDistance;

    void Start()
    {
        //Debug.Log("Wave Start");

        startScale = transform.localScale;

        // Derive the growth rate so the wave always reaches maxSize in growDuration
        // seconds, even after radius upgrades scale up both startScale and maxSize.
        growSpeed = growDuration > 0f
            ? (maxSize - startScale.x) / growDuration
            : maxSize;

        if (waveAnimationPrefab == null)
        {
            //Debug.LogError("Wave Animation Prefab is NOT assigned!");
            return;
        }

        GameObject anim = Instantiate(
            waveAnimationPrefab,
            transform.position,
            Quaternion.identity
        );

        //Debug.Log("Animation instantiated");

        animationEffect = anim.GetComponent<WaveAnimation>();

        ActivateWave();
    }

    void Update()
    {
        transform.localScale +=
            Vector3.one
            * growSpeed
            * Time.deltaTime;

        if (transform.localScale.x >= maxSize)
        {
            DeactivateWave();
            if (animationEffect != null)
            {
                Destroy(animationEffect.gameObject);
            }

            Destroy(gameObject);
        }
    }
    void ActivateWave()
    {
        isActive = true;

        if (animationEffect != null)
            animationEffect.Play();
    }

    void DeactivateWave()
    {
        isActive = false;

        if (animationEffect != null)
            animationEffect.Stop();
    }

    // Called by the spawner right after Instantiate (before Start captures startScale),
    // so the radius multiplier applied here is inherited by startScale and growth.
    public void Initialize(PlayerStats stats)
    {
        damage = stats.waveDamage;
        hasPushback = stats.waveHasPushback;
        pushbackDistance = stats.wavePushbackDistance;

        transform.localScale *= stats.waveRadiusMultiplier;
        maxSize *= stats.waveRadiusMultiplier;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Enemy"))
        {
            EnemyHealth enemy =
                collision.GetComponent<EnemyHealth>();

            if (enemy != null)
            {
                enemy.TakeDamage(damage);

                if (hasPushback)
                {
                    Vector2 dir =
                        (collision.transform.position - transform.position)
                        .normalized;

                    collision.transform.position +=
                        (Vector3)(dir * pushbackDistance);
                }
            }
        }
    }
}