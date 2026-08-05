using UnityEngine;

public class FireballSmokeStamper : MonoBehaviour
{
    [Header("Спавн")]
    [SerializeField] private GameObject smokeStampPrefab;
    [SerializeField] private float spacing = 0.08f;
    [SerializeField] private float randomOffset = 0.02f;

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
        GameObject stamp = Instantiate(smokeStampPrefab, position + offset, randomRotation);

        stamp.transform.localScale = stampScale;

        SpriteRenderer sr = stamp.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            Color c = sr.color;
            c.a = opacity;
            sr.color = c;
            sr.sortingOrder = sortingOrder;
        }
    }
}