using UnityEngine;
using System.Collections;

public class EnemySpawner : MonoBehaviour
{
    public GameObject enemyPrefab;
    public GameObject enemyBatPrefab;
    public Transform target;

    [Header("Wave Settings")]
    public int waveNumber = 1;
    public int waveEnemy = 2;
    public int enemiesPerWave = 5;
    public float spawnInterval = 1f;
    public float timeBetweenWaves = 3f;

    [Header("Difficulty Scaling")]
    public float enemySpeed = 3f;
    public float speedIncreasePerWave = 0.2f;
    public int extraEnemiesPerWave = 2;

    private int enemiesAlive = 0;
    private int randomNumber = 0;

    void Start()
    {
        StartCoroutine(WaveLoop());

    }

    IEnumerator WaveLoop()
    {
        while (true)
        {
            yield return StartCoroutine(SpawnWave());

            // Wait until all enemies are gone
            while (enemiesAlive > 0)
            {
                yield return null;
                randomNumber = Random.Range(0, 2);
                Debug.Log($"Random number for the wave is {randomNumber}");
            } 
            

            // Short break between waves
            yield return new WaitForSeconds(timeBetweenWaves);

            // Increase difficulty
            waveNumber++;
            enemiesPerWave += extraEnemiesPerWave;
            enemySpeed += speedIncreasePerWave;
        }
    }

    IEnumerator SpawnWave()
    {
        if (randomNumber == 0)
        {
            for (int i = 0; i < enemiesPerWave; i++)
            {
                int randomEnemy = 0;
                randomEnemy = Random.Range(0, 10);
                Debug.Log($"Random number for enemy is: {randomEnemy}");
                if (randomEnemy <= 3)
                {
                    SpawnEnemyBat();
                } else if (randomEnemy > 3)
                {
                    SpawnEnemy();
                }
                yield return new WaitForSeconds(spawnInterval);
            }
        } else if (randomNumber == 1)
        {
            for (int i = 0; i < enemiesPerWave; i++)
            {
                int randomEnemy = 0;
                randomEnemy = Random.Range(0, 10);
                Debug.Log($"Random number for enemy is: {randomEnemy}");
                if (randomEnemy <= 3)
                {
                    SpawnEnemy();
                }
                else if (randomEnemy > 3)
                {
                    SpawnEnemyBat();
                }
                yield return new WaitForSeconds(spawnInterval);
            }
        }
    }

    void SpawnEnemy()
    {
        Vector3 spawnPos = GetRandomEdgePosition();
        GameObject enemy = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);

        enemiesAlive++;

        EnemyMoveScript move = enemy.GetComponent<EnemyMoveScript>();
        if (move != null)
        {
            move.target = target;
            move.speed = enemySpeed;
        }

        EnemyHealth health = enemy.GetComponent<EnemyHealth>();
        if (health != null)
        {
            health.spawner = this;
        }
    }

    void SpawnEnemyBat()
    {
        Vector3 spawnPos = GetRandomEdgePosition();
        GameObject enemy = Instantiate(enemyBatPrefab, spawnPos, Quaternion.identity);

        enemiesAlive++;

        BatMoveScript move = enemy.GetComponent<BatMoveScript>();
        if (move != null)
        {
            move.target = target;
            move.speed = enemySpeed;
        }

        EnemyHealth health = enemy.GetComponent<EnemyHealth>();
        if (health != null)
        {
            health.spawner = this;
        }
    }

    public void OnEnemyKilled()
    {
        enemiesAlive--;
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
            case 0: x = Random.Range(-width / 2, width / 2); y = height / 2; break;
            case 1: x = Random.Range(-width / 2, width / 2); y = -height / 2; break;
            case 2: x = -width / 2; y = Random.Range(-height / 2, height / 2); break;
            case 3: x = width / 2; y = Random.Range(-height / 2, height / 2); break;
        }

        return new Vector3(x, y, 0);
    }
}