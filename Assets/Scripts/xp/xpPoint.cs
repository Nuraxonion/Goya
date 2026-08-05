    using UnityEngine;

    public class xpPoint : MonoBehaviour
    {
        public Transform target;
        public float speed = 5f;

        private bool isMouseOver = false;
        public float stopDistance = 0.5f;

        public float xpValue = 10f;

        // Set by the Spiral attack - see PlayerAttack.TryCastSpiral().
        private bool isCollecting = false;


        void OnMouseOver()
        {
            isMouseOver = true;
        }

        // Sends this orb flying at the player. The target is passed in because the
        // prefab's serialized target points at a prefab asset, not the live player.
        public void AttractTo(Transform player, float collectSpeed)
        {
            if (player == null)
                return;

            target = player;
            speed = collectSpeed;
            isCollecting = true;
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
            if (isMouseOver || isCollecting)
            {

                if (target == null) return;

                // Collecting runs on unscaled time: a big collect can trigger the
                // level up, which sets timeScale to 0 and would otherwise strand
                // the remaining orbs mid-flight until the upgrade panel closes.
                float delta = isCollecting ? Time.unscaledDeltaTime : Time.deltaTime;

                Vector3 direction = (target.position - transform.position).normalized;
                transform.position += direction * speed * delta;
            }
        }
    }
