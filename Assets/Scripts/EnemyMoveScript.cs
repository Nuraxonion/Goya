using UnityEngine;

public class EnemyMoveScript : MonoBehaviour
{
    public Transform target;
    public float speed = 3f;

    public float stopDistance = 0.5f;

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
}