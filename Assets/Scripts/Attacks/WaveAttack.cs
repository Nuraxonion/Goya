using UnityEngine;

public class WaveAttack : MonoBehaviour
{
    public float maxSize = 5f;
    // Fixed time (seconds) for the wave to grow from its start size to maxSize,
    // regardless of radius upgrades. growSpeed is derived from this in Start().
    // Kept equal to the length of WaveAnimation.anim (0.4166667s) so the hitbox
    // stops growing on the animation's last frame - retune both together.
    public float growDuration = 0.4166667f;
    private float growSpeed;
    public float damage = 1f;

    public GameObject waveAnimationPrefab;

    private WaveAnimation animationEffect;
    public bool isActive = false;

    private Vector3 startScale;

    // Set via Initialize from the wave weapon-skill stats.
    private bool hasPushback;
    private float pushbackDistance;
    private float radiusMultiplier = 1f;

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

        // The animation prefab's authored scale is calibrated so the drawn ring's
        // width lands on the collider edge at the base maxSize. Radius upgrades
        // scale maxSize, so the visual has to be scaled by the same factor.
        // Deliberately NOT parented to this transform: this object's localScale
        // grows every frame, and a child would inherit that growth on top of the
        // expansion the sprite frames already draw.
        anim.transform.localScale *= radiusMultiplier;

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
    public void Initialize(PlayerStats stats, float damageMultiplier)
    {
        damage = stats.waveDamage * damageMultiplier;
        hasPushback = stats.waveHasPushback;
        pushbackDistance = stats.wavePushbackDistance;

        radiusMultiplier = stats.waveRadiusMultiplier;

        transform.localScale *= radiusMultiplier;
        maxSize *= radiusMultiplier;
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
                PlayHitSound();

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
    public AudioClip[] hitSounds;

    private void PlayHitSound()
    {
        if (hitSounds == null || hitSounds.Length == 0) return;

        AudioClip clip = hitSounds[Random.Range(0, hitSounds.Length)];
        AudioSource.PlayClipAtPoint(clip, transform.position);
    }
}