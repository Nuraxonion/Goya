using UnityEngine;

public class xpPoint : MonoBehaviour
{
    public Transform target;
    public float speed = 5f;

    private bool isMouseOver = false;
    public float stopDistance = 0.5f;

    public float xpValue = 10f;


    void OnMouseOver()
    {
        isMouseOver = true;
        Debug.Log("Mouse entered");
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerXP playerXP = other.GetComponent<PlayerXP>();
            if (playerXP != null)
            {
                playerXP.AddXP(xpValue);
            }

            Destroy(gameObject);
        }
    }



    // Update is called once per frame
    void Update()
    {
        if (isMouseOver)
        {
            Debug.Log("Mouse is on object");

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
}
