using UnityEngine;

public class WaveAttack : MonoBehaviour
{
    public float maxSize = 8f;
    public float growSpeed = 8f;
    public float damage = 1f;

    public GameObject waveAnimationPrefab;

    private WaveAnimation animationEffect;
    public bool isActive = false;

    private Vector3 startScale;

    void Start()
    {
        //Debug.Log("Wave Start");

        startScale = transform.localScale;

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

    public void Initialize(PlayerStats stats)
    {
        damage = stats.waveDamage;

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
            }
        }
    }
}