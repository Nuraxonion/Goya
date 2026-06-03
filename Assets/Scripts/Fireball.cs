using UnityEngine;

public class Fireball : MonoBehaviour
{
    //Imports
    public PlayerStats playerStats;

    public float lifeTime = 3f;

    public float fireballSpeed;
    public float fireballRate;
    public float fireballDamage;

    //temporary variables
    //public float speed = 4f;
    //public float damage = 1f;


    private Vector2 direction;

    void Start()
    {
        Destroy(gameObject, lifeTime);
        //fireballRate = playerStats.fireballRate;
    }

    public void SetDirection(Vector2 dir)
    {
        direction = dir.normalized;
    }

    void Update()
    {
        transform.position +=
            (Vector3)direction *
            fireballSpeed *
            Time.deltaTime;
    }

    public void Initialize(PlayerStats stats)
    {
        fireballDamage = stats.fireballDamage;
        fireballRate = stats.fireballRate;
        fireballSpeed = stats.fireballSpeed;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        EnemyHealth enemy = collision.GetComponent<EnemyHealth>();

        if (enemy != null)
        {
            enemy.TakeDamage(fireballDamage);
            Destroy(gameObject);
        }
    }
}