using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightningAttack : MonoBehaviour
{
    [Header("Lightning Settings")]
    public int minDamage = 15;
    public int maxDamage = 30;

    public int minTargets = 2;
    public int maxTargets = 5;

    public EnemySpawner enemySpawner;

    public float delayBetweenStrikes = 0.1f;

    public float boltLifetime = 0.5f;

    public GameObject lightningPrefab;

    public void Cast()
    {
        StartCoroutine(LightningCoroutine());
    }

    IEnumerator LightningCoroutine()
    {
        List<EnemyHealth> aliveEnemies = new List<EnemyHealth>();

        foreach (EnemyHealth enemy in enemySpawner.activeEnemies)
        {
            if (enemy != null)
                aliveEnemies.Add(enemy);
        }

        if (aliveEnemies.Count == 0)
            yield break;

        int amount = Random.Range(minTargets, maxTargets + 1);
        amount = Mathf.Min(amount, aliveEnemies.Count);

        List<EnemyHealth> selected = new List<EnemyHealth>();

        while (selected.Count < amount)
        {
            EnemyHealth randomEnemy =
                aliveEnemies[Random.Range(0, aliveEnemies.Count)];

            if (!selected.Contains(randomEnemy))
            {
                selected.Add(randomEnemy);
            }
        }

        foreach (EnemyHealth enemy in selected)
        {
            if (lightningPrefab != null)
            {
                GameObject bolt = Instantiate(
                    lightningPrefab,
                    enemy.transform.position + Vector3.up * 3f,
                    Quaternion.identity);

                Destroy(bolt, boltLifetime);
            }

            int damage = Random.Range(minDamage, maxDamage + 1);

            enemy.TakeDamage(damage);

            yield return new WaitForSeconds(delayBetweenStrikes);
        }
    }
}