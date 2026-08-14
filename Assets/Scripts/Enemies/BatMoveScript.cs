using UnityEngine;

// Flying enemy: closes on the player while weaving side to side.
// target / speed / stopDistance / Stun / the facing flip / GetKnockbackDirection
// all live on EnemyMovement now.
public class BatMoveScript : EnemyMovement
{
    // INERT: replaced by zigzagSpeedRatio below, which keeps the weave consistent
    // as speed ramps across the run. Kept so existing serialized data doesn't break.
    [HideInInspector] public float zigzagAmount = 4f;

    [Tooltip("Sideways swerve as a fraction of forward speed. Scaling with speed keeps the weave looking the same whether the bat is crawling on wave 1 or racing on wave 50.")]
    public float zigzagSpeedRatio = 0.6f;

    public float zigzagFrequency = 6f;

    // Health-driven animation
    private Animator animator;
    private PlayerHealth playerHealth;
    private int currentHealthBand = -1;   // 1, 2, or 3; -1 forces first update

    void Start()
    {
        animator = GetComponent<Animator>();

        if (target != null)
            playerHealth = target.GetComponent<PlayerHealth>();
    }

    protected override void Move(float deltaTime)
    {
        UpdateHealthAnimation();

        float distance = Vector2.Distance(transform.position, target.position);

        // Only move if outside stop radius
        if (distance > stopDistance)
        {
            Vector2 directionZ = (target.position - transform.position).normalized;

            Vector2 perpendicular = new Vector2(-directionZ.y, directionZ.x);
            float zigzag = Mathf.Sin(Time.time * zigzagFrequency) * zigzagSpeedRatio * speed;

            Vector2 finalDirection = directionZ * speed + perpendicular * zigzag;

            transform.position += (Vector3)finalDirection * deltaTime;
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
}
