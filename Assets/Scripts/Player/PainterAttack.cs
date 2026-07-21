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

    public UpgradeManager upgradeManager;

    public float attackRate = 1f;
    public float range = 10f;

    //Cooldowns
    public float fireballCooldown;
    public float waveCooldown;

    private float attackTimer;

    void Start()
    {
        gestureManager = FindObjectOfType<GestureManager>();
    }

    void Update()
    {
        fireballCooldown -= Time.deltaTime;
        waveCooldown -= Time.deltaTime;

        // Dispatch on the data-driven attack id set by GestureManager. Each known
        // attack id maps to its spawn handler; new attacks (spiral / butterfly)
        // are added by registering another case here plus a mapping in the data file.
        string attack = gestureManager.currentAttack;

        if (attack == AttackIds.Fireball)
        {
            if (fireballCooldown <= 0f)
            {
                FireballAttack();
                FireAutoAimProjectiles();
                attackTimer = attackRate;
                fireballCooldown = playerStats.fireballAttackInterval;
            }
        }
        else if (attack == AttackIds.Wave && playerStats.hasWaveAttack)  // ← RENAMED from "hasWave"
        {
            if (waveCooldown <= 0f)
            {
                WaveAttack();
                attackTimer = attackRate;
                waveCooldown = playerStats.waveAttackInterval;
            }
        }
        else if (attack == AttackIds.Lightning)
        {
            Debug.Log("Lightning attack triggered!");
        }
        else if (!string.IsNullOrEmpty(attack))
        {
            // Recognized gesture maps to an attack the player can't use yet
            // (e.g. Wave before it's unlocked, or a reserved spiral/butterfly attack).
            gestureManager.currentAttack = AttackIds.None;
        }
    }

    public void Initialize(PlayerStats stats)
    {
        fireballCooldown = stats.fireballCooldown;
        Debug.Log($"Fireball cooldown initialized to: {fireballCooldown}");
        waveCooldown = stats.waveCooldown;
    }

    void WaveAttack()
    {
        SpawnWave();

        if (playerStats.waveDoubleCast)
            StartCoroutine(SpawnWaveDelayed(playerStats.waveSecondCastDelay));
    }

    void SpawnWave()
    {
        GameObject wave = Instantiate(
            wavePrefab,
            transform.position,
            Quaternion.identity
        );

        wave.GetComponent<WaveAttack>().Initialize(playerStats);
    }

    IEnumerator SpawnWaveDelayed(float delay)
    {
        yield return new WaitForSeconds(delay);
        SpawnWave();
    }

    void FireballAttack()
    {
        Vector3 mousePosition =
            Camera.main.ScreenToWorldPoint(
                Mouse.current.position.ReadValue()
            );

        mousePosition.z = 0;

        // Aim toward the mouse relative to the player, not the absolute world point.
        Vector2 direction = (Vector2)(mousePosition - transform.position);

        SpawnFireball(direction, playerStats.fireballDamage);
    }

    // Spawns playerStats.autoAimCount extra projectiles, each aimed at a
    // nearby enemy (falling back to a random direction when none are in range).
    void FireAutoAimProjectiles()
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
                // No (more) enemies to target — fire in a random direction.
                direction = Random.insideUnitCircle.normalized;
            }

            SpawnFireball(direction, playerStats.autoAimDamage);
        }
    }

    // Shared spawn path for both regular and auto-aimed fireballs.
    void SpawnFireball(Vector2 direction, float damage)
    {
        GameObject fireball =
            Instantiate(
                fireballPrefab,
                transform.position,
                Quaternion.identity
            );

        Fireball fireballScript = fireball.GetComponent<Fireball>();

        fireballScript.Initialize(
            damage,
            playerStats.fireballSpeed,
            playerStats.fireballPierce
        );
        fireballScript.SetDirection(direction);
    }

    // Returns up to `count` distinct enemies within `range`, nearest first.
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