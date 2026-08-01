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

                if (target == null) return;

                Vector3 direction = (target.position - transform.position).normalized;
                transform.position += direction * speed * Time.deltaTime;
            }
        }
    }
