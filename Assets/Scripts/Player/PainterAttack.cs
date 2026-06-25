using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAttack : MonoBehaviour
{
    // PREFABS
    public GameObject fireballPrefab;
    public GameObject wavePrefab;

    public AttackDuration attackDuration;

    Vector2 targetPosition;

    // IMPORTS
    private GestureManager gestureManager;
    public PlayerStats playerStats;
    public UpgradeManager upgradeManager;

    public float attackRate = 1f;
    public float range = 10f;

    // Attack rates
    public float fireballRate = 1f;

    // Cooldowns
    public float fireballCooldown;
    public float waveCooldown;

    void Start()
    {
        gestureManager = FindObjectOfType<GestureManager>();
    }

    void Update()
    {
        fireballCooldown -= Time.deltaTime;
        waveCooldown -= Time.deltaTime;

        // ✔ STRING система
        string attack = gestureManager.currentAttack;

        if (attack == AttackIds.Fireball)
        {
            if (fireballCooldown <= 0f)
            {
                FireballAttack();
                fireballCooldown = 1f / fireballRate;
            }
        }
        else if (attack == AttackIds.Wave && playerStats.hasWaveAttack)
        {
            if (waveCooldown <= 0f)
            {
                WaveAttack();
                waveCooldown = 5f;
            }
        }
        else if (attack == AttackIds.Lightning)
        {
            Debug.Log("Lightning attack triggered!");
        }
        else if (!string.IsNullOrEmpty(attack))
        {
            // неизвестный или заблокированный спелл
            gestureManager.currentAttack = AttackIds.None;
        }
    }

    public void Initialize(PlayerStats stats)
    {
        fireballCooldown = stats.fireballCooldown;
        waveCooldown = stats.waveCooldown;
    }

    void WaveAttack()
    {
        Instantiate(wavePrefab, transform.position, Quaternion.identity);
    }

    void FireballAttack()
    {
        Vector3 mousePosition =
            Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());

        mousePosition.z = 0;

        GameObject fireball = Instantiate(
            fireballPrefab,
            transform.position,
            Quaternion.identity
        );

        Fireball fireballScript = fireball.GetComponent<Fireball>();
        fireballScript.Initialize(playerStats);
        fireballScript.SetDirection(mousePosition);
    }

    void LightningAttack()
    {
        Debug.Log("Lightning placeholder");
    }
}