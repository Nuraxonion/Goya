using UnityEngine;

public class FireballSmokeStamper : MonoBehaviour
{
    [Header("Спавн")]
    [SerializeField] private GameObject smokeStampPrefab;
    [SerializeField] private float spacing = 0.08f;
    [SerializeField] private float randomOffset = 0.02f;

    // Hard cap on how many stamps exist at once, shared by every fireball.
    // Past this the oldest stamp is recycled into the newest position.
    [SerializeField] private int maxStamps = 2000;

    [Header("Внешний вид")]
    [SerializeField] private Vector3 stampScale = new Vector3(0.008f, 0.008f, 1f);
    [SerializeField][Range(0f, 1f)] private float opacity = 0.6f;
    [SerializeField] private int sortingOrder = 1;

    private Vector3 lastStampPosition;

    private void Start()
    {
        lastStampPosition = transform.position;
        SpawnStamp(transform.position);
    }

    private void Update()
    {
        if (Vector3.Distance(transform.position, lastStampPosition) >= spacing)
        {
            SpawnStamp(transform.position);
            lastStampPosition = transform.position;
        }
    }

    private void SpawnStamp(Vector3 position)
    {
        if (smokeStampPrefab == null) return;

        Vector3 offset = new Vector3(
            Random.Range(-randomOffset, randomOffset),
            Random.Range(-randomOffset, randomOffset),
            0f
        );

        Quaternion randomRotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));

        // The pool owns the stamp objects - see SmokeStampPool for why they are
        // recycled instead of instantiated per stamp.
        SmokeStampPool.Instance.Stamp(
            smokeStampPrefab,
            position + offset,
            randomRotation,
            stampScale,
            opacity,
            sortingOrder,
            maxStamps
        );
    }
}