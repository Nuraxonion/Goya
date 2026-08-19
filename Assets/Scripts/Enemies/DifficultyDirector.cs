using System;
using UnityEngine;

// Owns the run's difficulty curve. Everything is a pure function of elapsed run
// time (RunTimer), never of an abstract wave counter - so nothing can freeze the
// difficulty clock, and every value is authorable as a curve in the inspector
// instead of being derived from a formula.
//
// EnemySpawner is a dumb spawn service underneath this: the director decides what
// pressure to apply, the spawner decides where the bodies go.
public class DifficultyDirector : MonoBehaviour
{
    public static DifficultyDirector Instance;

    // Per-type stat block. The spawner writes these onto every enemy it creates,
    // so the prefab's own values are irrelevant - this is the single source of truth.
    [System.Serializable]
    public class EnemyArchetype
    {
        public string id = "Ground";
        public GameObject prefab;

        public float baseHealth = 2f;
        public float speedMultiplier = 1f;
        public float contactDamage = 10f;

        [Tooltip("True = kamikaze, lands one hit and dies on contact. False = survives and can hit again after its contact cooldown.")]
        public bool dieOnContact = true;

        [Tooltip("Threat spent to spawn one of these. The director spends a threat budget, not a headcount, so a heavier archetype can be introduced later without inflating enemy health. Both current types cost 1.")]
        [Min(0.01f)]
        public float threatCost = 1f;

        [Tooltip("Relative share of the spawn mix over the run, sampled at the current run time. Flat 1 on both types gives the 50/50 split the old per-wave mix roll averaged out to.")]
        public AnimationCurve weightOverRun;
    }

    // A scripted set-piece: a full ring of enemies closes in from every side at
    // once. Unlike the old implementation this does NOT stall the run - it damps
    // normal spawning for a few seconds and the difficulty clock keeps running.
    [System.Serializable]
    public class RingEvent
    {
        [Tooltip("Seconds after run start when this event fires.")]
        public float timeSeconds = 110f;

        [Tooltip("Enemies in the ring, spaced evenly around the perimeter.")]
        public int enemyCount = 50;

        [Tooltip("Which archetype fills the ring. Matched against EnemyArchetype.id; falls back to the first archetype.")]
        public string archetypeId = "Ground";

        [Tooltip("Ring radius as a multiple of the screen's half-diagonal. 1 puts the circle exactly through the four corners - the smallest circle that encloses the view, so nothing spawns on-screen.")]
        public float screenScale = 1f;

        [Tooltip("Superellipse exponent. 2 is a true circle. Higher values square the ring off towards the screen corners.")]
        public float cornerSharpness = 2f;

        [Tooltip("Normal spawning is multiplied by this while the ring is the focus. 0 stops it entirely.")]
        [Range(0f, 1f)]
        public float spawnDampen = 0.3f;

        [Tooltip("How long the damping lasts. The run is never blocked on the ring being cleared, so a straggler cannot stall anything.")]
        public float dampenDuration = 12f;
    }

    // A tight cluster of one archetype dropped in from off-screen. Like ring events
    // this never blocks the run - it injects the group, damps normal spawning for a
    // few seconds, and the difficulty clock carries on.
    [System.Serializable]
    public class SwarmEvent
    {
        [Tooltip("Seconds after run start when this event fires.")]
        public float timeSeconds = 90f;

        [Tooltip("How many enemies arrive in the cluster.")]
        public int enemyCount = 8;

        [Tooltip("Which archetype makes up the swarm. Matched against EnemyArchetype.id.")]
        public string archetypeId = "Hair";

        [Tooltip("How tightly packed the group is on arrival, in world units.")]
        public float clusterRadius = 1.2f;

        [Tooltip("Distance from the player as a multiple of the screen's half-diagonal. Above 1 the whole cluster starts off-screen.")]
        public float spawnDistance = 1.15f;

        [Tooltip("Normal spawning is multiplied by this while the swarm is the focus.")]
        [Range(0f, 1f)]
        public float spawnDampen = 0.5f;

        [Tooltip("How long the damping lasts.")]
        public float dampenDuration = 8f;
    }

    [Header("References")]
    [Tooltip("Found automatically if left empty.")]
    public EnemySpawner spawner;

    [Tooltip("Told when the run is completed. Found automatically if left empty.")]
    public GameOverManager gameOverManager;

    [Header("Run Shape")]
    [Tooltip("Seconds until the run is complete. Reaching this ends the run unless endless mode is on.")]
    public float runLength = 600f;

    [Tooltip("Off: the run ends at runLength. On: the curves keep growing past it. The planned 'continue in endless mode' button flips this via ContinueEndless().")]
    public bool endlessMode = false;

    [Header("Difficulty Curves")]
    [Tooltip("Enemies per second, before the rhythm envelope. Authored over 0..runLength seconds.")]
    public AnimationCurve threatPerSecond;

    [Tooltip("Enemy health multiplier over the run. Deliberately NOT exponential - player damage has a hard ceiling (~x180-400 for a fully maxed build), so a compounding health curve guarantees a wall where nothing dies.")]
    public AnimationCurve healthMultiplier;

    [Tooltip("Enemy contact damage multiplier over the run. Player is 100 HP baseline.")]
    public AnimationCurve damageMultiplier;

    [Tooltip("XP dropped per unit of threat killed.")]
    public AnimationCurve xpPerThreat;

    [Header("Rhythm")]
    [Tooltip("Length of one build/surge/breather cycle. 60s so the beat lines up with how the player reads the MM:SS timer.")]
    public float beatCycle = 60f;

    [Tooltip("Spawn rate multiplier across one cycle, sampled 0..1. Averages ~1, so this reshapes pressure without changing total volume.")]
    public AnimationCurve intensityOverCycle;

    [Header("Endless Continuation")]
    [Tooltip("Compounding per minute past runLength, applied once endless mode is on.")]
    public float endlessThreatGrowthPerMinute = 0.15f;

    public float endlessHealthGrowthPerMinute = 0.25f;

    [Header("Enemy Power")]
    [Tooltip("Flat across the whole run - speed is not a difficulty axis. Tuned so a ground enemy averages ~18s and a bat ~12s to cross the screen. Volume and health carry the curve instead.")]
    public float enemySpeed = 0.525f;

    // Hardcore meta upgrade, cached in Awake: the level cannot change mid run, and
    // reading PlayerPrefs on every spawn would be wasteful.
    private float hardcoreMultiplier = 1f;

    // Applied here rather than by overwriting enemySpeed, so the authored value stays
    // readable in the inspector and cannot be multiplied twice. EnemySpawner still
    // applies archetype.speedMultiplier on top, so per type ratios are preserved.
    private float EffectiveEnemySpeed => enemySpeed * hardcoreMultiplier;

    [Header("Archetypes")]
    public EnemyArchetype[] archetypes;

    [Header("Encirclement Events")]
    [Tooltip("Must be in ascending time order - entries are processed in sequence.")]
    public RingEvent[] ringEvents;

    [Header("Swarm Events")]
    [Tooltip("Must be in ascending time order - entries are processed in sequence.")]
    public SwarmEvent[] swarmEvents;

    [Header("Safety")]
    [Tooltip("Most enemies the director will spawn in a single frame. Stops a frame spike from dumping the whole banked budget at once.")]
    public int maxSpawnsPerFrame = 24;

    [Tooltip("Most threat that can sit banked waiting to be spent, so a hitch cannot bank a huge burst.")]
    public float maxBankedThreat = 12f;

    [Header("Debug")]
    public bool logDifficulty = false;

    public float logInterval = 15f;

    /// <summary>Fired once when the run reaches runLength with endless mode off.</summary>
    public event Action OnRunComplete;

    private float threatBudget;
    private int nextRingIndex;
    private int nextSwarmIndex;
    private float dampenUntil;
    private float dampenFactor = 1f;
    private bool runComplete;
    private bool runStarted;
    private PlayerStats playerStats;
    private float nextLogTime;

    // Fallback clock, used only if there is no RunTimer in the scene.
    private float ownElapsed;

    /// <summary>Seconds into the run. Freezes while paused, same as RunTimer.</summary>
    public float Elapsed => RunTimer.Instance != null ? RunTimer.Instance.ElapsedTime : ownElapsed;

    /// <summary>0 at run start, 1 at runLength. Stays at 1 in endless mode.</summary>
    public float Progress => runLength > 0f ? Mathf.Clamp01(Elapsed / runLength) : 0f;

    /// <summary>True once the run has been completed and spawning has stopped.</summary>
    public bool IsRunComplete => runComplete;

    /// <summary>True once the player has taken the fireball unlock and the run is live.</summary>
    public bool HasRunStarted => runStarted;

    /// <summary>
    /// Starts the clock and enemy spawning. Called when the player takes the
    /// fireball unlock, so a new player is never left defenceless while they read
    /// the first upgrade panel.
    /// </summary>
    public void BeginRun()
    {
        if (runStarted)
            return;

        runStarted = true;

        RunTimer.StartAll();

        if (logDifficulty)
            Debug.Log("[Difficulty] run started");
    }

    // Curves are authored over 0..runLength, so past that they are clamped and the
    // endless terms below take over.
    private float CurveTime => Mathf.Min(Elapsed, runLength);

    private float EndlessMinutes => Mathf.Max(0f, (Elapsed - runLength) / 60f);

    // Display only, so the debug log still reads in waves. Nothing derives from it.
    public int WaveNumber => Mathf.FloorToInt(Elapsed / 10f) + 1;

    private void Awake()
    {
        Instance = this;

        EnsureCurves();

        hardcoreMultiplier =
            1f + MetaUpgrades.GetTotalValue(MetaUpgradeIds.Hardcore);

        if (spawner == null)
            spawner = FindObjectOfType<EnemySpawner>();

        if (gameOverManager == null)
            gameOverManager = FindObjectOfType<GameOverManager>();

        // Safety net: if the fireball is already unlocked (a stat block set up for
        // testing, or a future save), there is nothing to wait for.
        playerStats = FindObjectOfType<PlayerStats>();

        if (playerStats != null && playerStats.hasFireballAttack)
            BeginRun();
    }

    private void Update()
    {
        // The unlock is what starts the run. Watching for it here rather than relying
        // on a single call in UpgradeManager, which has been lost in a merge before.
        if (!runStarted && playerStats != null && playerStats.hasFireballAttack)
            BeginRun();

        // Nothing happens until the player is armed. The threat budget accumulates
        // on Time.deltaTime, so holding the clock at zero is not enough on its own -
        // without this gate enemies would still spawn at threatPerSecond(0).
        if (!runStarted)
            return;

        if (RunTimer.Instance == null)
            ownElapsed += Time.deltaTime;

        float t = Elapsed;

        FireDueRingEvents(t);
        FireDueSwarmEvents(t);

        if (!runComplete && !endlessMode && runLength > 0f && t >= runLength)
        {
            CompleteRun();
            return;
        }

        if (runComplete)
            return;

        SpendThreat();

        if (logDifficulty && t >= nextLogTime)
        {
            nextLogTime = t + Mathf.Max(1f, logInterval);

            Debug.Log(
                $"[Difficulty] t={t:F0}s wave={WaveNumber} " +
                $"rate={CurrentThreatRate():F2}/s (base={threatPerSecond.Evaluate(CurveTime):F2} " +
                $"intensity={CurrentIntensity():F2} dampen={CurrentDampen():F2}) " +
                $"hpX={CurrentHealthMultiplier():F2} dmgX={CurrentDamageMultiplier():F2} " +
                $"xp={CurrentXPValue():F1} | alive={(spawner != null ? spawner.EnemiesAlive() : 0)}"
            );
        }
    }

    // --- Difficulty curve ---

    /// <summary>Enemies per second right now, including rhythm and any ring damping.</summary>
    public float CurrentThreatRate()
    {
        float rate = threatPerSecond.Evaluate(CurveTime);

        if (EndlessMinutes > 0f)
            rate *= Mathf.Pow(1f + endlessThreatGrowthPerMinute, EndlessMinutes);

        return Mathf.Max(0f, rate * CurrentIntensity() * CurrentDampen());
    }

    /// <summary>The build/surge/breather multiplier at this point in the cycle.</summary>
    public float CurrentIntensity()
    {
        if (beatCycle <= 0f)
            return 1f;

        float phase = Elapsed - Mathf.Floor(Elapsed / beatCycle) * beatCycle;

        return intensityOverCycle.Evaluate(phase / beatCycle);
    }

    public float CurrentHealthMultiplier()
    {
        float mult = healthMultiplier.Evaluate(CurveTime);

        if (EndlessMinutes > 0f)
            mult *= Mathf.Pow(1f + endlessHealthGrowthPerMinute, EndlessMinutes);

        // Multiplied in, not added, so Hardcore composes with the run curve instead of
        // flattening it: base x runCurve x hardcore.
        return mult * hardcoreMultiplier;
    }

    public float CurrentDamageMultiplier()
    {
        return damageMultiplier.Evaluate(CurveTime);
    }

    public float CurrentXPValue()
    {
        return xpPerThreat.Evaluate(CurveTime);
    }

    private float CurrentDampen()
    {
        return Elapsed < dampenUntil ? dampenFactor : 1f;
    }

    // --- Spawning ---

    private void SpendThreat()
    {
        if (spawner == null || archetypes == null || archetypes.Length == 0)
            return;

        threatBudget = Mathf.Min(threatBudget + CurrentThreatRate() * Time.deltaTime, maxBankedThreat);

        float hpMult = CurrentHealthMultiplier();
        float dmgMult = CurrentDamageMultiplier();
        float xp = CurrentXPValue();

        for (int i = 0; i < maxSpawnsPerFrame; i++)
        {
            EnemyArchetype pick = PickArchetype(threatBudget);

            if (pick == null)
                break;

            spawner.SpawnAt(pick, spawner.GetRandomEdgePosition(), hpMult, dmgMult, EffectiveEnemySpeed, xp * pick.threatCost);

            threatBudget -= pick.threatCost;
        }
    }

    // Weighted by each archetype's curve at the current run time, restricted to
    // what the budget can actually afford - so a cheap type still trickles out
    // while an expensive one is being saved up for.
    private EnemyArchetype PickArchetype(float budget)
    {
        float total = 0f;

        for (int i = 0; i < archetypes.Length; i++)
        {
            EnemyArchetype a = archetypes[i];

            if (a == null || a.prefab == null || a.weightOverRun == null || a.threatCost > budget)
                continue;

            total += Mathf.Max(0f, a.weightOverRun.Evaluate(CurveTime));
        }

        if (total <= 0f)
            return null;

        float roll = UnityEngine.Random.value * total;

        for (int i = 0; i < archetypes.Length; i++)
        {
            EnemyArchetype a = archetypes[i];

            if (a == null || a.prefab == null || a.weightOverRun == null || a.threatCost > budget)
                continue;

            roll -= Mathf.Max(0f, a.weightOverRun.Evaluate(CurveTime));

            if (roll <= 0f)
                return a;
        }

        return null;
    }

    /// <summary>Exact id match, or null. No fallback.</summary>
    public EnemyArchetype FindArchetypeExact(string id)
    {
        if (archetypes == null)
            return null;

        for (int i = 0; i < archetypes.Length; i++)
        {
            if (archetypes[i] != null && archetypes[i].id == id)
                return archetypes[i];
        }

        return null;
    }

    public EnemyArchetype FindArchetype(string id)
    {
        if (archetypes == null || archetypes.Length == 0)
            return null;

        for (int i = 0; i < archetypes.Length; i++)
        {
            if (archetypes[i] != null && archetypes[i].id == id)
                return archetypes[i];
        }

        return archetypes[0];
    }

    // --- Ring events ---

    // Walks the schedule in order. Nothing is awaited: the ring fires, damps normal
    // spawning for a few seconds, and the run carries on. The old version blocked
    // here for up to 60s and froze the difficulty curve with it.
    private void FireDueRingEvents(float t)
    {
        if (ringEvents == null || spawner == null)
            return;

        while (nextRingIndex < ringEvents.Length)
        {
            RingEvent ev = ringEvents[nextRingIndex];

            if (ev == null)
            {
                nextRingIndex++;
                continue;
            }

            if (t < ev.timeSeconds)
                break;

            nextRingIndex++;

            EnemyArchetype archetype = FindArchetype(ev.archetypeId);

            if (archetype == null)
                continue;

            spawner.SpawnRing(
                archetype, ev.enemyCount, ev.screenScale, ev.cornerSharpness,
                CurrentHealthMultiplier(), CurrentDamageMultiplier(), EffectiveEnemySpeed,
                CurrentXPValue() * archetype.threatCost
            );

            dampenFactor = ev.spawnDampen;
            dampenUntil = t + ev.dampenDuration;

            if (logDifficulty)
                Debug.Log($"[Ring] fired at {t:F0}s count={ev.enemyCount} dampen={ev.spawnDampen:F2} for {ev.dampenDuration:F0}s");
        }
    }

    private void FireDueSwarmEvents(float t)
    {
        if (swarmEvents == null || spawner == null)
            return;

        while (nextSwarmIndex < swarmEvents.Length)
        {
            SwarmEvent ev = swarmEvents[nextSwarmIndex];

            if (ev == null)
            {
                nextSwarmIndex++;
                continue;
            }

            if (t < ev.timeSeconds)
                break;

            nextSwarmIndex++;

            // Strict, unlike ring events: a swarm names an event-only archetype, so
            // falling back to archetypes[0] would quietly drop a pack of ground
            // enemies on the player instead of the intended swarm.
            EnemyArchetype archetype = FindArchetypeExact(ev.archetypeId);

            if (archetype == null)
            {
                Debug.LogWarning($"[Swarm] no archetype with id '{ev.archetypeId}' - swarm at {ev.timeSeconds:F0}s skipped.");
                continue;
            }

            spawner.SpawnCluster(
                archetype, ev.enemyCount, ev.clusterRadius, ev.spawnDistance,
                CurrentHealthMultiplier(), CurrentDamageMultiplier(), EffectiveEnemySpeed,
                CurrentXPValue() * archetype.threatCost
            );

            dampenFactor = ev.spawnDampen;
            dampenUntil = t + ev.dampenDuration;

            if (logDifficulty)
                Debug.Log($"[Swarm] fired at {t:F0}s id={ev.archetypeId} count={ev.enemyCount}");
        }
    }

    // --- Run completion ---

    private void CompleteRun()
    {
        runComplete = true;

        OnRunComplete?.Invoke();

        if (gameOverManager != null)
            gameOverManager.ShowRunComplete();
    }

    /// <summary>
    /// The seam for the planned "continue in endless mode" button. Turns endless
    /// mode on, resumes the clock and lets spawning restart from where it left off.
    /// </summary>
    public void ContinueEndless()
    {
        endlessMode = true;
        runComplete = false;

        Time.timeScale = 1f;

        if (RunTimer.Instance != null)
            RunTimer.Instance.StartTimer();
    }

    // --- Curve defaults ---

    private void Reset()
    {
        EnsureCurves();
        EnsureArchetypes();
    }

    private void OnValidate()
    {
        EnsureCurves();
    }

    // Curves are populated in code rather than authored into the scene YAML: an
    // AnimationCurve field that deserialises empty evaluates to 0, which would
    // silently mean "no spawns at all". Once a curve is edited in the inspector it
    // serialises normally and these guards skip it.
    private void EnsureCurves()
    {
        if (CurveUtil.IsEmpty(threatPerSecond))
        {
            threatPerSecond = CurveUtil.LinearCurve(
                0f, 0.7f, 60f, 1.2f, 120f, 2.0f, 180f, 3.0f, 240f, 4.2f, 300f, 5.6f,
                360f, 7.0f, 420f, 8.5f, 480f, 10.0f, 540f, 11.5f, 600f, 13.0f
            );
        }

        if (CurveUtil.IsEmpty(healthMultiplier))
        {
            healthMultiplier = CurveUtil.LinearCurve(
                0f, 1.0f, 60f, 2.2f, 150f, 4.5f, 300f, 8.9f, 450f, 16.0f, 600f, 25.0f
            );
        }

        if (CurveUtil.IsEmpty(damageMultiplier))
            damageMultiplier = CurveUtil.LinearCurve(0f, 1.0f, 600f, 3.0f);

        if (CurveUtil.IsEmpty(xpPerThreat))
            xpPerThreat = CurveUtil.LinearCurve(0f, 5.0f, 600f, 15.0f);

        if (CurveUtil.IsEmpty(intensityOverCycle))
        {
            // Build 0-35s, Surge 35-48s, Breather 48-60s. Hard steps so the beat is
            // legible rather than a vague swell. Cycle average is ~1.01, so this
            // reshapes pressure without changing total volume.
            intensityOverCycle = CurveUtil.StepCurve(
                0f, 1.0f, 35f / 60f, 1.8f, 48f / 60f, 0.2f
            );
        }

        // Same trap, and the one most likely to be hit by hand: an archetype added
        // in the inspector starts with an empty weight curve, which would evaluate
        // to 0 and quietly exclude it from the mix forever.
        if (archetypes != null)
        {
            for (int i = 0; i < archetypes.Length; i++)
            {
                if (archetypes[i] != null && CurveUtil.IsEmpty(archetypes[i].weightOverRun))
                    archetypes[i].weightOverRun = CurveUtil.LinearCurve(0f, 1f, 600f, 1f);
            }
        }
    }

    private void EnsureArchetypes()
    {
        if (archetypes != null && archetypes.Length > 0)
            return;

        archetypes = new EnemyArchetype[]
        {
            new EnemyArchetype
            {
                id = "Ground",
                baseHealth = 2f,
                speedMultiplier = 0.68f,
                contactDamage = 10f,
                dieOnContact = true,
                threatCost = 1f,
                weightOverRun = CurveUtil.LinearCurve(0f, 1f, 600f, 1f)
            },
            new EnemyArchetype
            {
                id = "Bat",
                baseHealth = 1.5f,
                speedMultiplier = 1.25f,
                contactDamage = 6f,
                dieOnContact = true,
                threatCost = 1f,
                weightOverRun = CurveUtil.LinearCurve(0f, 1f, 600f, 1f)
            },
            new EnemyArchetype
            {
                id = "Hair",
                baseHealth = 1f,
                speedMultiplier = 1.6f,
                contactDamage = 8f,
                dieOnContact = true,
                threatCost = 1f,

                // Flat ZERO, and deliberately authored rather than left empty: hair
                // enemies only ever arrive via swarm events, and EnsureCurves fills
                // an EMPTY weight curve with a constant 1 - which would drop them
                // into the normal spawn mix.
                weightOverRun = CurveUtil.LinearCurve(0f, 0f, 600f, 0f)
            }
        };
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
