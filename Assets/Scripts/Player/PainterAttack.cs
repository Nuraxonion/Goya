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

    private CursorManager cursorManager;

    // Multiplier saved when the gesture is recognized.
    // All attacks from this cast use the same multiplier.
    private float activeAttackMultiplier = 1f;

    public UpgradeManager upgradeManager;

    public float attackRate = 1f;
    public float range = 10f;

    //Cooldowns
    public float fireballCooldown;
    public float waveCooldown;

    private float attackTimer;
    private bool fireballTimerStarted = false;
    private bool waveTimerStarted = false;

    private CooldownBubbleManager cooldownBubbleManager;

    void Start()
    {
        cursorManager = FindFirstObjectByType<CursorManager>();

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

        string attack = gestureManager.currentAttack;

        if (attack == AttackIds.Fireball)
        {
            if (fireballCooldown <= 0f)
            {
                SaveCurrentGestureMultiplier();

                FireballAttack();
                FireAutoAimProjectiles();
                attackTimer = attackRate;
                fireballCooldown = playerStats.fireballAttackInterval;

                // Start duration timer ONCE
                if (!fireballTimerStarted && attackDuration != null)
                {
                    attackDuration.StartAttackTimer("Fireball");
                    fireballTimerStarted = true;
                }
            }
        }
        else if (attack == AttackIds.Wave && playerStats.hasWaveAttack)
        {
            if (waveCooldown <= 0f)
            {
                SaveCurrentGestureMultiplier();

                WaveAttack();
                attackTimer = attackRate;
                waveCooldown = playerStats.waveAttackInterval;

                // Start duration timer ONCE
                if (!waveTimerStarted && attackDuration != null)
                {
                    attackDuration.StartAttackTimer("Wave");
                    waveTimerStarted = true;
                }
            }
        }
        else if (attack == AttackIds.Lightning)
        {
            Debug.Log("Lightning attack triggered!");
        }
        else if (!string.IsNullOrEmpty(attack))
        {
            // GestureManager now refuses to hand out an attack that can't be cast,
            // so reaching this means a new attack id was added without teaching
            // PlayerStats.IsAttackAvailable about it.
            Debug.LogWarning($"PlayerAttack: no handler for attack id '{attack}' - discarding.");

            gestureManager.currentAttack = AttackIds.None;
            fireballTimerStarted = false;
            waveTimerStarted = false;
        }

        // Reset timer flags when cooldown is ready (attack is done)
        if (fireballCooldown <= 0f && fireballTimerStarted)
        {
            fireballTimerStarted = false;
        }

        if (waveCooldown <= 0f && waveTimerStarted)
        {
            waveTimerStarted = false;
        }
    }

    public void Initialize(PlayerStats stats)
    {
        fireballCooldown = stats.fireballCooldown;
        Debug.Log($"Fireball cooldown initialized to: {fireballCooldown}");
        waveCooldown = stats.waveCooldown;
    }

    void SaveCurrentGestureMultiplier()
    {
        if (gestureMultiplierManager != null)
        {
            activeAttackMultiplier =
                gestureMultiplierManager.GetDamageMultiplier();
        }
        else
        {
            activeAttackMultiplier = 1f;
        }

        Debug.Log(
            $"Current attack multiplier: x{activeAttackMultiplier}"
        );
    }

    void WaveAttack()
    {
        SpawnWave();

        if (playerStats.waveDoubleCast)
            StartCoroutine(SpawnWaveDelayed(playerStats.waveSecondCastDelay));
    }

    void SpawnWave()
    {
        cursorManager.ShowDurationCursor();
        GameObject wave = Instantiate(
            wavePrefab,
            transform.position,
            Quaternion.identity
        );

        WaveAttack waveAttack =
            wave.GetComponent<WaveAttack>();

        waveAttack.Initialize(
            playerStats,
            activeAttackMultiplier
        );
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

        Vector2 direction = (Vector2)(mousePosition - transform.position);

        SpawnFireball(direction, playerStats.fireballDamage);
    }

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
                direction = Random.insideUnitCircle.normalized;
            }

            SpawnFireball(direction, playerStats.autoAimDamage);
        }
    }

    void SpawnFireball(Vector2 direction, float damage)
    {
        cursorManager.ShowDurationCursor();
        // Apply gesture accuracy multiplier.
        damage *= activeAttackMultiplier;

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