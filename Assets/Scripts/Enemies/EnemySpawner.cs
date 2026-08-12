using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    // Per-type stat block. The spawner writes these onto every enemy it creates,
    // so the prefab's own values are irrelevant - this is the single source of truth.
    [System.Serializable]
    public class EnemyArchetype
    {
        public float baseHealth = 3f;
        public float speedMultiplier = 1f;
        public float contactDamage = 10f;

        [Tooltip("True = kamikaze, lands one hit and dies on contact. False = survives and can hit again after its contact cooldown.")]
        public bool dieOnContact = true;
    }

    public GameObject enemyPrefab;
    public GameObject enemyBatPrefab;
    public GameObject enemyDihPrefab;
    public Transform target;

    [Header("Wave Settings")]
    public int waveNumber = 1;

    [Header("Difficulty Curve - Pacing")]
    [Tooltip("Seconds between wave starts on wave 1. Waves overlap: a new one begins whether or not the last is dead.")]
    public float waveIntervalStart = 14f;
    public float waveIntervalMin = 8f;
    public float waveIntervalDecay = 0.2f;

    [Tooltip("Fraction of a wave's interval used to spread out its spawns.")]
    [Range(0.1f, 1f)]
    public float spawnSpreadFraction = 0.7f;

    [Header("Difficulty Curve - Volume")]
    public float enemiesPerWaveBase = 8f;
    public float enemiesPerWaveGrowth = 2f;

    [Header("Difficulty Curve - Enemy Power")]
    [Tooltip("Compounding rate: enemy health is multiplied by (1 + this)^(wave - 1). This is the main knob for run length - player damage multiplies, so linear health growth always falls behind.")]
    public float healthGrowthPerWave = 0.12f;

    [Tooltip("Enemy contact damage is multiplied by (1 + this * (wave - 1)).")]
    public float damageGrowthPerWave = 0.03f;

    [Tooltip("Flat across the whole run - speed is not a difficulty axis. Tuned so a ground enemy averages ~18s and a bat ~12s to cross the screen from spawn. Count, health and wave interval carry the difficulty curve instead.")]
    public float speedBase = 0.525f;

    [Tooltip("Left at 0 deliberately. Raising it re-enables a per-wave speed ramp, which double-counts the pressure already coming from enemy count and health.")]
    public float speedGrowthPerWave = 0f;

    [Header("Difficulty Curve - Reward")]
    [Tooltip("Halved against pass 1 to offset the doubled enemy count, keeping total run XP (and end-of-run level) steady.")]
    public float xpValueBase = 5f;
    public float xpValueGrowthPerWave = 0.15f;

    [Header("Enemy Archetypes")]
    public EnemyArchetype groundStats = new EnemyArchetype
    {
        baseHealth = 2f,
        speedMultiplier = 0.68f,
        contactDamage = 10f,
        dieOnContact = true
    };

    public EnemyArchetype batStats = new EnemyArchetype
    {
        baseHealth = 1.5f,
        speedMultiplier = 1.25f,
        contactDamage = 6f,
        dieOnContact = true
    };

    [Header("Debug")]
    public bool logWaveStats = false;

    private int enemiesAlive = 0;
    private int randomNumber = 0;

    public List<EnemyHealth> activeEnemies = new List<EnemyHealth>();

    void Start()
    {
        StartCoroutine(WaveLoop());
    }

    // --- Difficulty curve (n = waves elapsed since wave 1) ---

    public float CurrentWaveInterval()
    {
        return Mathf.Max(waveIntervalMin, waveIntervalStart - waveIntervalDecay * (waveNumber - 1));
    }

    public int CurrentEnemyCount()
    {
        return Mathf.Max(1, Mathf.RoundToInt(enemiesPerWaveBase + enemiesPerWaveGrowth * (waveNumber - 1)));
    }

    public float CurrentSpawnInterval()
    {
        return CurrentWaveInterval() * spawnSpreadFraction / CurrentEnemyCount();
    }

    // Compounding, not linear: the fireball skill tree multiplies player damage on
    // four axes at once, so linear enemy health always loses the race. n-1 keeps
    // wave 1 at exactly 1.0.
    public float HealthMultiplier()
    {
        return Mathf.Pow(1f + healthGrowthPerWave, waveNumber - 1);
    }

    public float DamageMultiplier()
    {
        return 1f + damageGrowthPerWave * (waveNumber - 1);
    }

    public float CurrentSpeed()
    {
        return speedBase + speedGrowthPerWave * (waveNumber - 1);
    }

    public float CurrentXPValue()
    {
        return xpValueBase + xpValueGrowthPerWave * (waveNumber - 1);
    }

    IEnumerator WaveLoop()
    {
        while (true)
        {
            // Rolled at the start of every wave so wave 1's mix isn't fixed.
            randomNumber = Random.Range(0, 2);

            if (logWaveStats)
            {
                Debug.Log(
                    $"[Wave {waveNumber}] interval={CurrentWaveInterval():F1}s " +
                    $"count={CurrentEnemyCount()} hpX={HealthMultiplier():F2} " +
                    $"dmgX={DamageMultiplier():F2} speed={CurrentSpeed():F2} " +
                    $"xp={CurrentXPValue():F1} | alive at start={enemiesAlive}"
                );
            }

            // Not yielded: waves overlap, so falling behind on kills accumulates pressure.
            StartCoroutine(SpawnWave());

            yield return new WaitForSeconds(CurrentWaveInterval());

            waveNumber++;
        }
    }

    // Wave stats are captured up-front so an in-flight wave keeps its own numbers
    // even after waveNumber has advanced.
    IEnumerator SpawnWave()
    {
        int count = CurrentEnemyCount();
        float spacing = CurrentSpawnInterval();
        float healthMult = HealthMultiplier();
        float damageMult = DamageMultiplier();
        float speed = CurrentSpeed();
        float xpValue = CurrentXPValue();
        int mix = randomNumber;

        for (int i = 0; i < count; i++)
        {
            bool spawnBat = (Random.Range(0, 10) <= 3) == (mix == 0);

            SpawnOne(
                spawnBat ? enemyBatPrefab : enemyPrefab,
                spawnBat ? batStats : groundStats,
                healthMult, damageMult, speed, xpValue
            );

            yield return new WaitForSeconds(spacing);
        }
    }

    void SpawnOne(GameObject prefab, EnemyArchetype stats, float healthMult, float damageMult, float speed, float xpValue)
    {
        if (prefab == null) return;

        Vector3 spawnPos = GetRandomEdgePosition();
        GameObject enemy = Instantiate(prefab, spawnPos, Quaternion.identity);

        enemiesAlive++;

        float finalSpeed = speed * stats.speedMultiplier;

        EnemyMoveScript move = enemy.GetComponent<EnemyMoveScript>();
        if (move != null)
        {
            move.target = target;
            move.speed = finalSpeed;
        }

        BatMoveScript batMove = enemy.GetComponent<BatMoveScript>();
        if (batMove != null)
        {
            batMove.target = target;
            batMove.speed = finalSpeed;
        }

        EnemyHealth health = enemy.GetComponent<EnemyHealth>();
        if (health != null)
        {
            health.spawner = this;
            health.health = stats.baseHealth * healthMult;
            health.damage = stats.contactDamage * damageMult;
            health.dieOnContact = stats.dieOnContact;
            health.xpValue = xpValue;

            activeEnemies.Add(health);
        }
    }

    public void OnEnemyKilled()
    {
        enemiesAlive = Mathf.Max(0, enemiesAlive - 1);
    }

    public int EnemiesAlive()
    {
        return enemiesAlive;
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
