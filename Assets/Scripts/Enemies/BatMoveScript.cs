using UnityEngine;

public class BatMoveScript : MonoBehaviour
{
    public Transform target;
    public float speed = 3f;

    // Flipping sprite by X
    private Rigidbody2D body;
    private SpriteRenderer spriteRender;

    public float zigzagAmount = 4f;
    public float zigzagFrequency = 6f;

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
            //Vector3 direction = (target.position - transform.position).normalized;
            Vector2 directionZ = (target.position - transform.position).normalized;

            Vector2 perpendicular = new Vector2(-directionZ.y, directionZ.x);
            float zigzag = Mathf.Sin(Time.time * zigzagFrequency) * zigzagAmount;

            Vector2 finalDirection = directionZ * speed + perpendicular * zigzag;

            transform.position += (Vector3)finalDirection * Time.deltaTime;
            //transform.position += direction * speed * Time.deltaTime;
        }
    }

    private void FixedUpdate()
    {
        spriteRender.flipX = body.position.x <= target.position.x;
    }
}