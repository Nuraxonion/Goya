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

    protected override string WalkStateSuffix => "HeadFly_Walk";

    protected override void Move(float deltaTime)
    {
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
}
