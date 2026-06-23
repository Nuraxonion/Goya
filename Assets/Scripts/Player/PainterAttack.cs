using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using static DrawingSystem;

public class PlayerAttack : MonoBehaviour
{

    //PREFABAS
    public GameObject fireballPrefab;
    public GameObject wavePrefab;

    public AttackDuration attackDuration;

    Vector2 targetPosition;

    //IMPORTS
    private GestureManager gestureManager;
    public GestureManager.AttackType attackType;
    public PlayerStats playerStats;

    public UpgradeManager upgradeManager;

    public float attackRate = 1f;
    public float range = 10f;

    //Attack rates
    public float fireballRate = 1f;

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
        if (gestureManager.currentAttack != GestureManager.AttackType.NoAttack && gestureManager.currentAttack != GestureManager.AttackType.Circle && gestureManager.currentAttack == GestureManager.AttackType.Bracket)
            {
                if (fireballCooldown <= 0f) {
                //AttackNearestEnemy();
                FireballAttack();
                    attackTimer = attackRate;
                    fireballCooldown = 1f / fireballRate;
                }
            }
            else if (gestureManager.currentAttack == GestureManager.AttackType.Circle && playerStats.hasWaveAttack)
            {
                if (waveCooldown <= 0f)
                {
                    WaveAttack();
                    attackTimer = attackRate;
                    waveCooldown = 5f; // Example cooldown for wave attack
                }
            }
            else
            {
                gestureManager.currentAttack = GestureManager.AttackType.NoAttack;
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
        Instantiate(
                wavePrefab,
                transform.position,
                Quaternion.identity
            );

        GameObject wave = Instantiate(
            wavePrefab,
            transform.position,
            Quaternion.identity
        );
    }

    void FireballAttack()
    {
        Vector3 mousePosition =
            Camera.main.ScreenToWorldPoint(
                Mouse.current.position.ReadValue()
            );

        mousePosition.z = 0;

        GameObject fireball =
    Instantiate(
        fireballPrefab,
        transform.position,
        Quaternion.identity
    );

        Fireball fireballScript = fireball.GetComponent<Fireball>();

        fireballScript.Initialize(playerStats);
        fireballScript.SetDirection(mousePosition);
    }

    void AttackNearestEnemy()
    {
        GameObject[] enemies =
            GameObject.FindGameObjectsWithTag("Enemy");

        if (enemies.Length == 0)
            return;

        GameObject nearestEnemy = null;

        float closestDistance = Mathf.Infinity;

        foreach (GameObject enemy in enemies)
        {
            float distance =
                Vector2.Distance(
                    transform.position,
                    enemy.transform.position
                );

            if (distance < closestDistance
                && distance <= range)
            {
                closestDistance = distance;
                nearestEnemy = enemy;
            }
        }

        if (nearestEnemy != null)
        {
            targetPosition = nearestEnemy.transform.position;
        }
        else
        {
            Vector2 randomDirection =
                Random.insideUnitCircle.normalized;

            targetPosition =
                (Vector2)transform.position
                + randomDirection * 10f;
        }

        GameObject fireball =
            Instantiate(
                fireballPrefab,
                transform.position,
                Quaternion.identity
            );

        fireball
            .GetComponent<Fireball>()
            .SetDirection(targetPosition);
    }
}