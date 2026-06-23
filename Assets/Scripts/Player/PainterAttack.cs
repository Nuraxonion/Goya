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
        if (gestureManager == null)
            gestureManager = FindObjectOfType<GestureManager>();

        if (gestureManager != null)
            gestureManager.OnGestureRecognized += HandleGestureRecognized;
        else
            Debug.LogError("[PlayerAttack] No GestureManager found; attacks won't be triggered.");
    }

    void OnDestroy()
    {
        if (gestureManager != null)
            gestureManager.OnGestureRecognized -= HandleGestureRecognized;
    }

    void Update()
    {
        // Cooldowns keep ticking; attacks themselves fire from the recognition event.
        fireballCooldown -= Time.deltaTime;
        waveCooldown -= Time.deltaTime;
    }

    // Event-driven entry point: invoked once each time a gesture is recognized.
    // The recognized name has already been mapped to an AttackType on the GestureManager.
    private void HandleGestureRecognized(string gestureName, float score)
    {
        switch (gestureManager.currentAttack)
        {
            case GestureManager.AttackType.Bracket:
                if (fireballCooldown <= 0f)
                {
                    FireballAttack();
                    attackTimer = attackRate;
                    fireballCooldown = 1f / fireballRate;
                }
                break;

            case GestureManager.AttackType.Circle:
                if (playerStats != null && playerStats.hasWaveAttack && waveCooldown <= 0f)
                {
                    WaveAttack();
                    attackTimer = attackRate;
                    waveCooldown = 5f; // Example cooldown for wave attack
                }
                break;
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