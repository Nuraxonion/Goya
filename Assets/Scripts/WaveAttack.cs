using UnityEngine;

public class WaveAttack : MonoBehaviour
{
    public float maxSize = 8f;
    public float growSpeed = 8f;
    public float damage = 20f;

    private Vector3 startScale;

    void Start()
    {
        startScale = transform.localScale;
    }

    void Update()
    {
        transform.localScale +=
            Vector3.one
            * growSpeed
            * Time.deltaTime;

        if (transform.localScale.x >= maxSize)
        {
            Destroy(gameObject);
        }
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