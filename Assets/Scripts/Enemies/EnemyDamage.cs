using UnityEngine;

public class EnemyDamage : MonoBehaviour
{
    public float damagePerSecond = 10f;

    private void OnTriggerStay2D(Collider2D other)
    {
        PlayerHealth player = other.GetComponent<PlayerHealth>();

        if (player != null)
        {
            player.TakeDamage(damagePerSecond * Time.deltaTime, transform.position);
        }
    }
}