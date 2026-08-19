using UnityEngine;

// Ground enemy: walks straight at the player.
// target / speed / stopDistance / Stun / the facing flip / GetKnockbackDirection
// all live on EnemyMovement now.
public class EnemyMoveScript : EnemyMovement
{
    protected override string WalkStateSuffix => "ManStick_Walk";

    public bool isTouchingPlayer = false;
    private float time = 1;
    private float timeToAct = 1f;

    public EnemyHealth enemyHealth;
    public PlayerHealth playerHealth;

    protected override void Move(float deltaTime)
    {
        float distance = Vector2.Distance(transform.position, target.position);

        // Only move if outside stop radius
        if (distance > stopDistance)
        {
            Vector3 direction = (target.position - transform.position).normalized;
            transform.position += direction * speed * deltaTime;
        }

        // Dead path: isTouchingPlayer is never set true (OnTriggerEnter2D's body is
        // commented out below), so this never runs. Real contact damage lives in
        // EnemyHealth.TryContactDamage. Left in place rather than removed as part of
        // an unrelated change.
        if (isTouchingPlayer)
        {
            if (time < timeToAct)
            {
                time = deltaTime;
            }
            else
            {
                time = 0;
                playerHealth.currentHealth -= enemyHealth.damage;
            }
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        //Debug.Log("Touched: " + other.name);

        if (other.CompareTag("Player"))
        {
            //isTouchingPlayer = true;
            //enemiesTouching++;
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isTouchingPlayer = false;
            //enemiesTouching--;
        }
    }
}
