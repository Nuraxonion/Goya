using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public GameObject enemyPrefab;
    public float spawnInterval = 2f;

    void Start()
    {
        InvokeRepeating(nameof(SpawnEnemy), 1f, spawnInterval);
    }

    public Transform target; // assign center object in Inspector

    void SpawnEnemy()
    {
        Vector3 spawnPos = GetRandomEdgePosition();
        GameObject enemy = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);

        EnemyMoveScript moveScript = enemy.GetComponent<EnemyMoveScript>();
        if (moveScript != null)
        {
            moveScript.target = target;
        }
    }

    Vector3 GetRandomEdgePosition()
    {
        Camera cam = Camera.main;

        float height = 2f * cam.orthographicSize;
        float width = height * cam.aspect;

        float x = 0;
        float y = 0;

        int side = Random.Range(0, 4);

        switch (side)
        {
            case 0: // Top
                x = Random.Range(-width / 2, width / 2);
                y = height / 2;
                break;
            case 1: // Bottom
                x = Random.Range(-width / 2, width / 2);
                y = -height / 2;
                break;
            case 2: // Left
                x = -width / 2;
                y = Random.Range(-height / 2, height / 2);
                break;
            case 3: // Right
                x = width / 2;
                y = Random.Range(-height / 2, height / 2);
                break;
        }

        return new Vector3(x, y, 0);
    }
}