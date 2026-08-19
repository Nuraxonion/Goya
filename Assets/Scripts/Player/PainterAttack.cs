using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using static DrawingSystem;

public class PlayerAttack : MonoBehaviour
{
    //PREFABS
    public GameObject fireballPrefab;
    public GameObject wavePrefab;

    public LightningAttack lightningAttack;

    public AttackDuration attackDuration;

    //IMPORTS
    private GestureManager gestureManager;
    public PlayerStats playerStats;
    public GestureMultiplierManager gestureMultiplierManager;

    public UpgradeManager upgradeManager;

    [Tooltip("Source of the live enemy list for auto-aim. Found automatically if left empty.")]
    public EnemySpawner enemySpawner;

    // Reused by FindNearestEnemies so auto-aim allocates nothing per cast.
    private readonly List<Transform> nearestBuffer = new List<Transform>();
    private Vector2 sortOrigin;
    private System.Comparison<Transform> nearestFirst;

    public float attackRate = 1f;
    public float range = 10f;

    //Cooldowns
    public float fireballCooldown;
    public float waveCooldown;

    // Public so a HUD bubble can read it the way CooldownBubbleManager reads the
    // other two.
    public float spiralCooldown;

    private float lightningCooldown;

    private CooldownBubbleManager cooldownBubbleManager;

    void Start()
    {
        gestureManager = FindObjectOfType<GestureManager>();

        gestureMultiplierManager =
            FindObjectOfType<GestureMultiplierManager>();

        cooldownBubbleManager =
            FindObjectOfType<CooldownBubbleManager>();

        if (enemySpawner == null)
            enemySpawner = FindObjectOfType<EnemySpawner>();

        // Cached once: passing a lambda to Sort would allocate a closure per cast.
        nearestFirst = CompareByDistance;

        if (lightningAttack == null)
            lightningAttack = GetComponent<LightningAttack>();
    }

    void Update()
    {
        fireballCooldown -= Time.deltaTime;
        waveCooldown -= Time.deltaTime;
        spiralCooldown -= Time.deltaTime;
        lightningCooldown -= Time.deltaTime;

        if (attackDuration == null)
            return;

        // Each active attack is checked independently rather than as an if/else
        // chain over a single id, so with Multi-Tasking several can fire at once.
        // AttackDuration is the source of truth for what is still running.
        if (attackDuration.IsActive(AttackIds.Fireball)
            && playerStats.hasFireballAttack
            && fireballCooldown <= 0f)
        {
            float multiplier = attackDuration.GetMultiplier(AttackIds.Fireball);

            FireballAttack(multiplier);
            FireAutoAimProjectiles(multiplier);

            fireballCooldown = playerStats.fireballAttackInterval;
        }

        if (attackDuration.IsActive(AttackIds.Wave)
            && playerStats.hasWaveAttack
            && waveCooldown <= 0f)
        {
            WaveAttack(attackDuration.GetMultiplier(AttackIds.Wave));

            waveCooldown = playerStats.waveAttackInterval;
        }

        if (attackDuration.IsActive(AttackIds.Lightning)
    && playerStats.hasLightningAttack
    && lightningCooldown <= 0f)
        {
            Debug.Log("⚡ PLAYER ATTACK: Lightning triggered!");

            LightningAttack(
                attackDuration.GetMultiplier(AttackIds.Lightning)
            );

            lightningCooldown = playerStats.lightningCastSpeed;
        }
    }

    // Fire-once utility attack: pulls every XP orb in the level to the player.
    // Called straight from GestureManager instead of going through AttackDuration,
    // so it has no duration and the Multi-Tasking upgrades don't touch it - it is
    // gated by its own cooldown alone. Returns false when it could not be cast.
    public bool TryCastSpiral()
    {
        if (playerStats == null || !playerStats.hasSpiralAttack)
            return false;

        if (spiralCooldown > 0f)
            return false;

        xpPoint[] orbs = FindObjectsByType<xpPoint>(FindObjectsSortMode.None);

        for (int i = 0; i < orbs.Length; i++)
        {
            orbs[i].AttractTo(playerStats.spiralCollectSpeed);
        }

        spiralCooldown = playerStats.spiralAttackInterval;

        Debug.Log($"🌀 Spiral collected {orbs.Length} XP orbs");

        return true;
    }

    public void Initialize(PlayerStats stats)
    {
        fireballCooldown = stats.fireballCooldown;
        Debug.Log($"Fireball cooldown initialized to: {fireballCooldown}");
        waveCooldown = stats.waveCooldown;
    }

    // The multiplier is passed down the spawn chain rather than held in a field:
    // the delayed double-cast resolves after the fact, so a shared field would
    // pick up another attack's multiplier once two are active at once.
    void WaveAttack(float multiplier)
    {
        SpawnWave(multiplier);

        if (playerStats.waveDoubleCast)
            StartCoroutine(SpawnWaveDelayed(playerStats.waveSecondCastDelay, multiplier));
    }

    void SpawnWave(float multiplier)
    {
        GameObject wave = Instantiate(
            wavePrefab,
            transform.position,
            Quaternion.identity
        );

        WaveAttack waveAttack =
            wave.GetComponent<WaveAttack>();

        waveAttack.Initialize(
            playerStats,
            multiplier
        );
    }

    IEnumerator SpawnWaveDelayed(float delay, float multiplier)
    {
        yield return new WaitForSeconds(delay);
        SpawnWave(multiplier);
    }

    void FireballAttack(float multiplier)
    {
        Vector3 mousePosition =
            Camera.main.ScreenToWorldPoint(
                Mouse.current.position.ReadValue()
            );

        mousePosition.z = 0;

        Vector2 direction = (Vector2)(mousePosition - transform.position);

        SpawnFireball(
            direction,
            playerStats.fireballDamage + playerStats.fireballBonusDamage,
            multiplier);
    }

    void FireAutoAimProjectiles(float multiplier)
    {
        int count = playerStats.autoAimCount;
        if (count <= 0)
            return;

        List<Transform> targets = FindNearestEnemies(count);

        for (int i = 0; i < count; i++)
        {
            Vector2 direction;

            if (i < targets.Count)
            {
                direction = (Vector2)(targets[i].position - transform.position);
            }
            else
            {
                direction = Random.insideUnitCircle.normalized;
            }

            SpawnFireball(direction, playerStats.autoAimDamage, multiplier);
        }
    }

    void SpawnFireball(Vector2 direction, float damage, float multiplier)
    {
        // Apply gesture accuracy multiplier.
        damage *= multiplier;

        GameObject fireball =
            Instantiate(
                fireballPrefab,
                transform.position,
                Quaternion.identity
            );

        Fireball fireballScript =
            fireball.GetComponent<Fireball>();

        fireballScript.Initialize(
            damage,
            playerStats.fireballSpeed,
            playerStats.fireballPierce
        );

        fireballScript.SetDirection(direction);
    }

    // Reads the spawner's live enemy list (the same source LightningAttack uses)
    // instead of scanning every tagged object in the scene. The buffer and the
    // comparison delegate are reused so a cast allocates nothing, and distances
    // are compared squared to skip a square root per comparison.
    List<Transform> FindNearestEnemies(int count)
    {
        nearestBuffer.Clear();

        if (enemySpawner == null)
            return nearestBuffer;

        sortOrigin = transform.position;

        float rangeSqr = range * range;

        List<EnemyHealth> active = enemySpawner.activeEnemies;

        for (int i = 0; i < active.Count; i++)
        {
            EnemyHealth enemy = active[i];

            if (enemy == null)
                continue;

            Vector2 offset = (Vector2)enemy.transform.position - sortOrigin;

            if (offset.sqrMagnitude <= rangeSqr)
                nearestBuffer.Add(enemy.transform);
        }

        nearestBuffer.Sort(nearestFirst);

        if (nearestBuffer.Count > count)
            nearestBuffer.RemoveRange(count, nearestBuffer.Count - count);

        return nearestBuffer;
    }

    int CompareByDistance(Transform a, Transform b)
    {
        float aSqr = ((Vector2)a.position - sortOrigin).sqrMagnitude;
        float bSqr = ((Vector2)b.position - sortOrigin).sqrMagnitude;

        return aSqr.CompareTo(bSqr);
    }

    void LightningAttack(float multiplier)
    {
        Debug.Log("⚡ PlayerAttack.LightningAttack() called!");

        if (lightningAttack == null)
        {
            Debug.LogError("⚡ LightningAttack component is NULL!");
            return;
        }

        // Where the gesture was drawn, captured once at cast time - not the live
        // cursor. The bolt stays put for the whole duration, so placing it is the
        // decision and enemies can walk back out of it.
        if (!attackDuration.TryGetCastPosition(
                AttackIds.Lightning,
                out Vector2 castPosition))
        {
            castPosition = transform.position;
        }

        Debug.Log("⚡ Casting Lightning at: " + castPosition);

        lightningAttack.Cast(
            castPosition,
            multiplier
        );
    }
}