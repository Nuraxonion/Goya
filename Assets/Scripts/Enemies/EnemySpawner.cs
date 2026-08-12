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

    [Header("Difficulty Curve - Downtime")]
    [Tooltip("Seconds from the start of one lull to the start of the next. The lull occupies the END of each period.")]
    public float lullPeriod = 75f;

    [Tooltip("How long each lull lasts, in seconds.")]
    public float lullDuration = 15f;

    [Tooltip("Fraction of scheduled spawns that still happen during a lull. 0 stops spawning entirely.")]
    [Range(0f, 1f)]
    public float lullSpawnFraction = 0.25f;

    // A scripted set-piece: normal spawning stops and a full ring of base enemies
    // closes in from every side at once.
    [System.Serializable]
    public class RingEvent
    {
        [Tooltip("Seconds after run start when this event fires.")]
        public float timeSeconds = 150f;

        [Tooltip("Enemies in the ring, spaced evenly around the perimeter.")]
        public int enemyCount = 50;

        [Tooltip("Ring radius as a multiple of the screen's half-diagonal. 1 puts the circle exactly through the four corners - the smallest circle that encloses the view, so nothing spawns on-screen. Below 1 and part of the ring becomes visible.")]
        public float screenScale = 1f;

        [Tooltip("Superellipse exponent. 2 is a true circle. Higher values square the ring off towards the screen corners, pushing the diagonals further out.")]
        public float cornerSharpness = 2f;
    }

    [Header("Encirclement Events")]
    [Tooltip("Must be in ascending time order - entries are processed in sequence.")]
    public RingEvent[] ringEvents =
    {
        new RingEvent { timeSeconds = 110f, enemyCount = 50, screenScale = 1f, cornerSharpness = 2f },
        new RingEvent { timeSeconds = 210f, enemyCount = 50, screenScale = 1f, cornerSharpness = 2f },
        new RingEvent { timeSeconds = 300f, enemyCount = 50, screenScale = 1f, cornerSharpness = 2f },
        new RingEvent { timeSeconds = 390f, enemyCount = 50, screenScale = 1f, cornerSharpness = 2f },
        new RingEvent { timeSeconds = 480f, enemyCount = 50, screenScale = 1f, cornerSharpness = 2f },
    };

    [Tooltip("Normal spawning resumes after this long even if ring enemies are still alive, so a straggler can never stall the run. Must comfortably exceed the walk-in time - side enemies need ~35s to cross from off-screen.")]
    public float ringEventTimeout = 60f;

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

    private float runStartTime;
    private bool wasInLull;

    private bool eventActive;
    private readonly List<EnemyHealth> ringEnemies = new List<EnemyHealth>();

    public List<EnemyHealth> activeEnemies = new List<EnemyHealth>();

    void Start()
    {
        runStartTime = Time.time;

        StartCoroutine(WaveLoop());
        StartCoroutine(EventLoop());
    }

    // Purely diagnostic - early-returns to nothing when logging is off.
    void Update()
    {
        if (!logWaveStats)
            return;

        bool inLull = IsInLull();

        if (inLull != wasInLull)
        {
            wasInLull = inLull;

            Debug.Log($"[Lull] {(inLull ? "started" : "ended")} at {Time.time - runStartTime:F1}s");
        }
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

    // The lull sits at the end of every period: with the defaults, seconds 60-75
    // of each 75-second cycle. Time.time is scaled, so this clock stops while the
    // upgrade panel or pause menu holds timeScale at 0 - a lull is never silently
    // spent while the player is reading an upgrade.
    public bool IsInLull()
    {
        if (lullPeriod <= 0f || lullDuration <= 0f)
            return false;

        float duration = Mathf.Min(lullDuration, lullPeriod);
        float phase = (Time.time - runStartTime) % lullPeriod;

        return phase >= lullPeriod - duration;
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
            // Also freezes waveNumber, so the difficulty clock doesn't tick through
            // an event during which nothing spawned.
            while (eventActive)
                yield return null;

            // Rolled at the start of every wave so wave 1's mix isn't fixed.
            randomNumber = Random.Range(0, 2);

            if (logWaveStats)
            {
                Debug.Log(
                    $"[Wave {waveNumber}] interval={CurrentWaveInterval():F1}s " +
                    $"count={CurrentEnemyCount()} lull={IsInLull()} " +
                    $"hpX={HealthMultiplier():F2} " +
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

    // Walks the schedule in order. Time.time is scaled, so - like the lull clock -
    // this pauses while the upgrade panel or pause menu holds timeScale at 0, and
    // an event can never be consumed while the player is reading an upgrade.
    IEnumerator EventLoop()
    {
        if (ringEvents == null)
            yield break;

        for (int i = 0; i < ringEvents.Length; i++)
        {
            RingEvent ev = ringEvents[i];

            if (ev == null)
                continue;

            while (Time.time - runStartTime < ev.timeSeconds)
                yield return null;

            yield return StartCoroutine(RunRingEvent(ev));
        }
    }

    IEnumerator RunRingEvent(RingEvent ev)
    {
        Camera cam = Camera.main;

        if (target == null || enemyPrefab == null || ev.enemyCount <= 0 || cam == null)
            yield break;

        // Same camera maths GetRandomEdgePosition uses, so the ring and the normal
        // edge spawns stay consistent at any aspect ratio.
        float halfHeight = cam.orthographicSize;
        float halfWidth = halfHeight * cam.aspect;

        // A circle through the screen's corners - the smallest circle that fully
        // encloses the view, so every enemy starts off-screen and all of them are
        // equidistant from the player.
        float radius = Mathf.Sqrt(halfWidth * halfWidth + halfHeight * halfHeight) * ev.screenScale;

        float a = radius;
        float b = radius;

        eventActive = true;

        float healthMult = HealthMultiplier();
        float damageMult = DamageMultiplier();
        float speed = CurrentSpeed();
        float xpValue = CurrentXPValue();

        ringEnemies.Clear();

        // Even ANGLE steps around a superellipse do NOT give even spacing - the
        // point speed varies around the curve, bunching positions near the corners.
        // Distribute by arc length instead.
        const int SAMPLES = 256;

        float[] arc = new float[SAMPLES + 1];
        Vector2 prev = SuperellipsePoint(0f, a, b, ev.cornerSharpness);

        for (int i = 1; i <= SAMPLES; i++)
        {
            float t = (i / (float)SAMPLES) * Mathf.PI * 2f;
            Vector2 p = SuperellipsePoint(t, a, b, ev.cornerSharpness);

            arc[i] = arc[i - 1] + Vector2.Distance(prev, p);
            prev = p;
        }

        float perimeter = arc[SAMPLES];

        // All on one frame: the oval appearing at once is the effect. Target arc
        // lengths only increase, so one forward walk resolves every position.
        int seg = 1;

        for (int i = 0; i < ev.enemyCount; i++)
        {
            float targetArc = (i / (float)ev.enemyCount) * perimeter;

            while (seg < SAMPLES && arc[seg] < targetArc)
                seg++;

            float f = Mathf.InverseLerp(arc[seg - 1], arc[seg], targetArc);
            float t0 = (seg - 1) / (float)SAMPLES * Mathf.PI * 2f;
            float t1 = seg / (float)SAMPLES * Mathf.PI * 2f;
            float t = Mathf.Lerp(t0, t1, f);

            Vector3 offset = SuperellipsePoint(t, a, b, ev.cornerSharpness);

            EnemyHealth spawned = SpawnOne(
                enemyPrefab, groundStats, target.position + offset,
                healthMult, damageMult, speed, xpValue
            );

            if (spawned != null)
                ringEnemies.Add(spawned);
        }

        if (logWaveStats)
        {
            Debug.Log(
                $"[Ring] started at {Time.time - runStartTime:F1}s " +
                $"count={ev.enemyCount} scale={ev.screenScale:F2} sharpness={ev.cornerSharpness:F0} " +
                $"radius={radius:F1} perimeter={perimeter:F1} hpX={healthMult:F2}"
            );
        }

        // Held until the ring is cleared so this reads as a "deal with this"
        // moment, with a timeout so an unreachable straggler can never stall
        // the run.
        float deadline = Time.time + ringEventTimeout;

        while (Time.time < deadline && AnyRingEnemyAlive())
            yield return null;

        if (logWaveStats)
        {
            Debug.Log(
                $"[Ring] ended at {Time.time - runStartTime:F1}s " +
                $"({(AnyRingEnemyAlive() ? "timed out" : "cleared")})"
            );
        }

        ringEnemies.Clear();
        eventActive = false;
    }

    // |x/a|^n + |y/b|^n = 1. n=2 is an ellipse; raising n squares it off towards
    // the screen's rectangle, which is what lets the ring sit just outside the
    // view without having to be scaled far away from it.
    static Vector2 SuperellipsePoint(float t, float a, float b, float exponent)
    {
        float cos = Mathf.Cos(t);
        float sin = Mathf.Sin(t);

        // Guarded: an exponent of 0 would divide by zero, and below 2 the shape
        // caves inwards and would no longer enclose the screen.
        float e = 2f / Mathf.Max(2f, exponent);

        return new Vector2(
            a * Mathf.Sign(cos) * Mathf.Pow(Mathf.Abs(cos), e),
            b * Mathf.Sign(sin) * Mathf.Pow(Mathf.Abs(sin), e)
        );
    }

    // Destroy leaves a Unity-null reference behind, so a null slot means that
    // enemy is gone however it died.
    bool AnyRingEnemyAlive()
    {
        for (int i = 0; i < ringEnemies.Count; i++)
        {
            if (ringEnemies[i] != null)
                return true;
        }

        return false;
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
            // Re-checked per spawn: waves overlap, so a lull or an event starting
            // mid-wave has to stop every coroutine already in flight, not just the
            // next wave. Skipped spawns are dropped rather than deferred -
            // deferring would push the backlog into the quiet and defeat it.
            if (!eventActive && (!IsInLull() || Random.value < lullSpawnFraction))
            {
                bool spawnBat = (Random.Range(0, 10) <= 3) == (mix == 0);

                SpawnOne(
                    spawnBat ? enemyBatPrefab : enemyPrefab,
                    spawnBat ? batStats : groundStats,
                    GetRandomEdgePosition(),
                    healthMult, damageMult, speed, xpValue
                );
            }

            yield return new WaitForSeconds(spacing);
        }
    }

    // Returns the spawned enemy so the ring event can track what it created.
    EnemyHealth SpawnOne(GameObject prefab, EnemyArchetype stats, Vector3 spawnPos, float healthMult, float damageMult, float speed, float xpValue)
    {
        if (prefab == null) return null;

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

        return health;
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
