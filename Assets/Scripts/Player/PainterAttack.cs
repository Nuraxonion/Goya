using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using static DrawingSystem;

public class PlayerAttack : MonoBehaviour
{
    public GameObject fireballPrefab;
    public GameObject wavePrefab;

    Vector2 targetPosition;

    // Importing drawing types
    // NEED TO IMPORT NEW CODE AND NEW VARIABLES
    private GestureManager gestureManager;
    public GestureManager.AttackType attackType;
    //private DrawingSystem drawingSystem;
    //public AttackDirection attackDirection;
    public PlayerStats playerStats;

    public UpgradeManager upgradeManager;

    public float attackRate = 1f;
    public float range = 10f;

    private float attackTimer;

    void Start()
    {
        gestureManager = FindObjectOfType<GestureManager>();
        //drawingSystem =
        //GameObject.FindGameObjectWithTag("Finish")
        //.GetComponent<DrawingSystem>();
    }

    void Update()
    {
        if (gestureManager.currentAttack != GestureManager.AttackType.NoAttack && gestureManager.currentAttack != GestureManager.AttackType.Circle && gestureManager.currentAttack == GestureManager.AttackType.Bracket)
            {
                //Debug.Log($"Hello {drawingSystem.currentAttackDirection}");
                AttackNearestEnemy();
                gestureManager.currentAttack = GestureManager.AttackType.NoAttack;
                attackTimer = attackRate;
            }
            else if (gestureManager.currentAttack == GestureManager.AttackType.Circle && playerStats.hasWaveAttack)
            {
                Debug.Log("HI");
                WaveAttack();
                gestureManager.currentAttack = GestureManager.AttackType.NoAttack;
            }
            else
            {
                gestureManager.currentAttack = GestureManager.AttackType.NoAttack;
            }
    }

    void WaveAttack()
    {
        Instantiate(
                wavePrefab,
                transform.position,
                Quaternion.identity
            );
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

    Vector2 GetDirectionVector(AttackDirection dir)
    {
        switch (dir)
        {
            case AttackDirection.North: return Vector2.up;
            case AttackDirection.Northeast: return new Vector2(1, 1).normalized;
            case AttackDirection.East: return Vector2.right;
            case AttackDirection.Southeast: return new Vector2(1, -1).normalized;
            case AttackDirection.South: return Vector2.down;
            case AttackDirection.Southwest: return new Vector2(-1, -1).normalized;
            case AttackDirection.West: return Vector2.left;
            case AttackDirection.Northwest: return new Vector2(-1, 1).normalized;
        }

        return Vector2.right;
    }

    Vector2 AddSpread(Vector2 baseDir, float angle)
    {
        float randomAngle = Random.Range(-angle, angle);
        return Quaternion.Euler(0, 0, randomAngle) * baseDir;
    }
}