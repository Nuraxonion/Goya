using UnityEngine;

public class FireballSmokeStamper : MonoBehaviour
{
    public GameObject smokeStampPrefab;

    public float spacing = 0.035f;
    public float randomOffset = 0.002f;

    private Vector3 lastSpawnPosition;

    private void Start()
    {
        lastSpawnPosition = transform.position;
    }

    private void Update()
    {
        if (smokeStampPrefab == null)
            return;

        float distance = Vector3.Distance(transform.position, lastSpawnPosition);

        if (distance < spacing)
            return;

        Vector3 direction = (transform.position - lastSpawnPosition).normalized;

        while (distance >= spacing)
        {
            lastSpawnPosition += direction * spacing;
            SpawnStamp(lastSpawnPosition);
            distance = Vector3.Distance(transform.position, lastSpawnPosition);
        }
    }

    private void SpawnStamp(Vector3 position)
    {
        Vector3 offset = new Vector3(
            Random.Range(-randomOffset, randomOffset),
            Random.Range(-randomOffset, randomOffset),
            0f
        );

        GameObject stamp = Instantiate(
            smokeStampPrefab,
            position + offset,
            Quaternion.identity
        );

        // ЖЁСТКИЙ РАЗМЕР. Больше Inspector не решает размер PNG.
        stamp.transform.localScale = new Vector3(0.008f, 0.008f, 1f);

        SpriteRenderer sr = stamp.GetComponent<SpriteRenderer>();

        if (sr != null)
        {
            Color c = sr.color;
            c.a = 0.1f; // временно 100%, чтобы точно увидеть PNG
            sr.color = c;

            sr.sortingOrder = 9; // под Fireball, но выше карты
        }
    }
}