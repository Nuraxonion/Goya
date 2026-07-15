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

    // Health-driven animation
    private Animator animator;
    private PlayerHealth playerHealth;
    private int currentHealthBand = -1;   // 1, 2, or 3; -1 forces first update

    void Start()
    {
        body = GetComponent<Rigidbody2D>();
        spriteRender = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();

        if (target != null)
            playerHealth = target.GetComponent<PlayerHealth>();
    }

    void Update()
    {
        if (target == null) return;

        UpdateHealthAnimation();

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

    private void UpdateHealthAnimation()
    {
        if (playerHealth == null || animator == null) return;
        if (playerHealth.maxHealth <= 0f) return;

        float ratio = playerHealth.currentHealth / playerHealth.maxHealth;

        // > 2/3        -> band 1 -> 1HeadFly_Walk
        // 1/3 .. 2/3   -> band 2 -> 2HeadFly_Walk
        // <= 1/3       -> band 3 -> 3HeadFly_Walk
        int band;
        if (ratio > 2f / 3f)      band = 1;
        else if (ratio > 1f / 3f) band = 2;
        else                      band = 3;

        if (band != currentHealthBand)
        {
            currentHealthBand = band;
            animator.Play(band + "HeadFly_Walk");
        }
    }

    private void FixedUpdate()
    {
        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * (target.position.x > transform.position.x ? -1 : 1);
        transform.localScale = scale;
        //spriteRender.flipX = body.position.x <= target.position.x;
    }
}