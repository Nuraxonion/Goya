using System.Collections.Generic;
using UnityEngine;

public class Fireball : MonoBehaviour
{
    //Imports
    public PlayerStats playerStats;

    public float lifeTime = 3f;

    public float fireballSpeed;
    public float fireballDamage;

    // Number of enemies this projectile can pass through before being destroyed.
    // 0 = destroyed on first hit (default behaviour).
    public int pierceRemaining = 0;

    public GameObject hitEffectPrefab;
    
    // Prevents a single trigger overlap from damaging the same enemy twice.
    private HashSet<EnemyHealth> hitEnemies = new HashSet<EnemyHealth>();

    public AudioClip hitSound;
    private AudioSource audioSource;

    //temporary variables
    //public float speed = 4f;
    //public float damage = 1f;


    private Vector2 direction;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        Destroy(gameObject, lifeTime);
    }

    // Expects a direction vector (target - firePosition), not an absolute position.
    public void SetDirection(Vector2 dir)
    {
        direction = dir.normalized;
        
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    void Update()
    {
        transform.position +=
            (Vector3)direction *
            fireballSpeed *
            Time.deltaTime;
    }

    public void Initialize(float damage, float speed, int pierce)
    {
        fireballDamage = damage;
        fireballSpeed = speed;
        pierceRemaining = pierce;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        EnemyHealth enemy = collision.GetComponent<EnemyHealth>();

        if (enemy != null && !hitEnemies.Contains(enemy))
        {
            hitEnemies.Add(enemy);
            enemy.TakeDamage(fireballDamage);
            PlayHitSound();

            if (pierceRemaining <= 0)
            {
                SpawnHitEffect();
                Destroy(gameObject);
            }
            else
            {
                pierceRemaining--;
            }
        }
    }
    
    private void SpawnHitEffect()
    {
        if (hitEffectPrefab == null) return;
        
        GameObject effect = Instantiate(
            hitEffectPrefab,
            transform.position,
            transform.rotation  
        );
        
        Destroy(effect, 0.5f);
    }
    public AudioClip[] hitSounds;

    private void PlayHitSound()
    {
        if (hitSounds == null || hitSounds.Length == 0) return;

        AudioClip clip = hitSounds[Random.Range(0, hitSounds.Length)];
        AudioSource.PlayClipAtPoint(clip, transform.position);
    }
}