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

    public AttackDuration attackDuration;

    //IMPORTS
    private GestureManager gestureManager;
    public PlayerStats playerStats;
    public GestureMultiplierManager gestureMultiplierManager;

    public UpgradeManager upgradeManager;

    public float attackRate = 1f;
    public float range = 10f;

    //Cooldowns
    public float fireballCooldown;
    public float waveCooldown;

    // Public so a HUD bubble can read it the way CooldownBubbleManager reads the
    // other two.
    public float spiralCooldown;

    private CooldownBubbleManager cooldownBubbleManager;

    void Start()
    {
        gestureManager = FindObjectOfType<GestureManager>();

        gestureMultiplierManager =
            FindObjectOfType<GestureMultiplierManager>();

        cooldownBubbleManager =
            FindObjectOfType<CooldownBubbleManager>();
    }

    void Update()
    {
        fireballCooldown -= Time.deltaTime;
        waveCooldown -= Time.deltaTime;
        spiralCooldown -= Time.deltaTime;

        if (attackDuration == null)
            return;

        // Each active attack is checked independently rather than as an if/else
        // chain over a single id, so with Multi-Tasking several can fire at once.
        // AttackDuration is the source of truth for what is still running.
        if (attackDuration.IsActive(AttackIds.Fireball) && fireballCooldown <= 0f)
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

        SpawnFireball(direction, playerStats.fireballDamage, multiplier);
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

    List<Transform> FindNearestEnemies(int count)
    {
        GameObject[] enemies =
            GameObject.FindGameObjectsWithTag("Enemy");

        List<Transform> inRange = new List<Transform>();

        foreach (GameObject enemy in enemies)
        {
            if (enemy == null)
                continue;

            float distance =
                Vector2.Distance(
                    transform.position,
                    enemy.transform.position
                );

            if (distance <= range)
                inRange.Add(enemy.transform);
        }

        inRange.Sort((a, b) =>
            Vector2.Distance(transform.position, a.position)
            .CompareTo(
                Vector2.Distance(transform.position, b.position)));

        if (inRange.Count > count)
            inRange.RemoveRange(count, inRange.Count - count);

        return inRange;
    }

    void LightningAttack()
    {

    }
}