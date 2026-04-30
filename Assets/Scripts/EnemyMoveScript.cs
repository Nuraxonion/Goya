using UnityEngine;

public class EnemyMoveScript : MonoBehaviour
{
    public Transform target;
    public float speed = 3f;

    // Flipping sprite by X
    private Rigidbody2D body;
    private SpriteRenderer spriteRender;

    public float stopDistance = 0.5f;

    void Start()
    {
        body = GetComponent<Rigidbody2D>();
        spriteRender = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        if (target == null) return;

        float distance = Vector2.Distance(transform.position, target.position);

        // Only move if outside stop radius
        if (distance > stopDistance)
        {
            Vector3 direction = (target.position - transform.position).normalized;
            transform.position += direction * speed * Time.deltaTime;
        }
    }

    private void FixedUpdate()
    {
        spriteRender.flipX = body.position.x <= target.position.x;
    }
}