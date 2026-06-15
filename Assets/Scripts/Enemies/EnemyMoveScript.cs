using UnityEngine;

public class EnemyMoveScript : MonoBehaviour
{
    public Transform target;
    public float speed = 3f;

    // Flipping sprite by X
    private Rigidbody2D body;
    private SpriteRenderer spriteRender;

    public bool isTouchingPlayer = false;
    private float time = 1;
    private float timeToAct = 1f;

    public float stopDistance = 0f;

    public EnemyHealth enemyHealth;
    public PlayerHealth playerHealth;

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

        

        if (isTouchingPlayer)
        {
            if (time < timeToAct)
            {
                time = Time.deltaTime;
            } else
            {
                time = 0;
                playerHealth.currentHealth -= enemyHealth.damage;

            }
        }
    }

    private void FixedUpdate()
    {
        spriteRender.flipX = body.position.x <= target.position.x;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        //Debug.Log("Touched: " + other.name);

        if (other.CompareTag("Player"))
        {
            isTouchingPlayer = true;
            Debug.Log($"Touching player: {isTouchingPlayer}");
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