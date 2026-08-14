using System.Collections.Generic;
using UnityEngine;

// Spawn service. This decides WHERE bodies go and writes the stats it is handed
// onto them; DifficultyDirector decides WHAT pressure to apply and when.
//
// activeEnemies is a public List<EnemyHealth> field by contract: PlayerAttack's
// auto-aim (PainterAttack.cs) indexes it directly and tolerates destroyed entries,
// and EnemyHealth.Die removes itself from it.
public class EnemySpawner : MonoBehaviour
{
    public Transform target;

    public List<EnemyHealth> activeEnemies = new List<EnemyHealth>();

    private int enemiesAlive = 0;

    // Spawns one enemy at a given point. Returns it so callers can track what they
    // created.
    public EnemyHealth SpawnAt(
        DifficultyDirector.EnemyArchetype archetype, Vector3 spawnPos,
        float healthMult, float damageMult, float speed, float xpValue)
    {
        if (archetype == null || archetype.prefab == null)
            return null;

        GameObject enemy = Instantiate(archetype.prefab, spawnPos, Quaternion.identity);

        enemiesAlive++;

        float finalSpeed = speed * archetype.speedMultiplier;

        // One lookup for every movement type - ground, bat, hair and anything added
        // later all derive from EnemyMovement.
        EnemyMovement move = enemy.GetComponent<EnemyMovement>();
        if (move != null)
        {
            move.target = target;
            move.speed = finalSpeed;
        }

        EnemyHealth health = enemy.GetComponent<EnemyHealth>();
        if (health != null)
        {
            health.spawner = this;
            health.health = archetype.baseHealth * healthMult;
            health.damage = archetype.contactDamage * damageMult;
            health.dieOnContact = archetype.dieOnContact;
            health.xpValue = xpValue;

            activeEnemies.Add(health);
        }

        return health;
    }

    // A full ring closing in from every side at once. All on one frame: the oval
    // appearing at once is the effect.
    public void SpawnRing(
        DifficultyDirector.EnemyArchetype archetype, int enemyCount,
        float screenScale, float cornerSharpness,
        float healthMult, float damageMult, float speed, float xpValue)
    {
        Camera cam = Camera.main;

        if (target == null || archetype == null || archetype.prefab == null || enemyCount <= 0 || cam == null)
            return;

        // Same camera maths GetRandomEdgePosition uses, so the ring and the normal
        // edge spawns stay consistent at any aspect ratio.
        float halfHeight = cam.orthographicSize;
        float halfWidth = halfHeight * cam.aspect;

        // A circle through the screen's corners - the smallest circle that fully
        // encloses the view, so every enemy starts off-screen and all of them are
        // equidistant from the player.
        float radius = Mathf.Sqrt(halfWidth * halfWidth + halfHeight * halfHeight) * screenScale;

        float a = radius;
        float b = radius;

        // Even ANGLE steps around a superellipse do NOT give even spacing - the
        // point speed varies around the curve, bunching positions near the corners.
        // Distribute by arc length instead.
        const int SAMPLES = 256;

        float[] arc = new float[SAMPLES + 1];
        Vector2 prev = SuperellipsePoint(0f, a, b, cornerSharpness);

        for (int i = 1; i <= SAMPLES; i++)
        {
            float t = (i / (float)SAMPLES) * Mathf.PI * 2f;
            Vector2 p = SuperellipsePoint(t, a, b, cornerSharpness);

            arc[i] = arc[i - 1] + Vector2.Distance(prev, p);
            prev = p;
        }

        float perimeter = arc[SAMPLES];

        // Target arc lengths only increase, so one forward walk resolves every
        // position.
        int seg = 1;

        for (int i = 0; i < enemyCount; i++)
        {
            float targetArc = (i / (float)enemyCount) * perimeter;

            while (seg < SAMPLES && arc[seg] < targetArc)
                seg++;

            float f = Mathf.InverseLerp(arc[seg - 1], arc[seg], targetArc);
            float t0 = (seg - 1) / (float)SAMPLES * Mathf.PI * 2f;
            float t1 = seg / (float)SAMPLES * Mathf.PI * 2f;
            float t = Mathf.Lerp(t0, t1, f);

            Vector3 offset = SuperellipsePoint(t, a, b, cornerSharpness);

            SpawnAt(archetype, target.position + offset, healthMult, damageMult, speed, xpValue);
        }
    }

    // A tight cluster arriving from one point off-screen. Every member is handed the
    // same groupSeed so HairMoveScript's shared drift path makes them move as one
    // pack, plus its own memberSeed so it darts individually within that pack.
    public void SpawnCluster(
        DifficultyDirector.EnemyArchetype archetype, int enemyCount,
        float clusterRadius, float spawnDistance,
        float healthMult, float damageMult, float speed, float xpValue)
    {
        Camera cam = Camera.main;

        if (target == null || archetype == null || archetype.prefab == null || enemyCount <= 0 || cam == null)
            return;

        float halfHeight = cam.orthographicSize;
        float halfWidth = halfHeight * cam.aspect;

        // Same half-diagonal the ring event uses, so a spawnDistance of 1 puts the
        // cluster exactly on the smallest circle that encloses the view.
        float radius = Mathf.Sqrt(halfWidth * halfWidth + halfHeight * halfHeight) * spawnDistance;

        float angle = Random.value * Mathf.PI * 2f;
        Vector3 clusterCentre = target.position + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius;

        float groupSeed = Random.value * 1000f;

        for (int i = 0; i < enemyCount; i++)
        {
            Vector3 offset = Random.insideUnitCircle * clusterRadius;

            EnemyHealth spawned = SpawnAt(
                archetype, clusterCentre + offset,
                healthMult, damageMult, speed, xpValue
            );

            if (spawned == null)
                continue;

            HairMoveScript hair = spawned.GetComponent<HairMoveScript>();

            if (hair != null)
            {
                hair.groupSeed = groupSeed;
                hair.memberSeed = groupSeed + (i + 1) * 13.7f;
            }
        }
    }

    // |x/a|^n + |y/b|^n = 1. n=2 is an ellipse; raising n squares it off towards
    // the screen's rectangle, which is what lets the ring sit just outside the
    // view without having to be scaled far away from it.
    static Vector2 SuperellipsePoint(float t, float a, float b, float exponent)
    {
        float cos = Mathf.Cos(t);
        float sin = Mathf.Sin(t);

        // Guarded: an exponent of 0 would divide by zero, and below 2 the shape
        // caves inwards and would no longer enclose the screen.
        float e = 2f / Mathf.Max(2f, exponent);

        return new Vector2(
            a * Mathf.Sign(cos) * Mathf.Pow(Mathf.Abs(cos), e),
            b * Mathf.Sign(sin) * Mathf.Pow(Mathf.Abs(sin), e)
        );
    }

    public void OnEnemyKilled()
    {
        enemiesAlive = Mathf.Max(0, enemiesAlive - 1);
    }

    public int EnemiesAlive()
    {
        return enemiesAlive;
    }

    public Vector3 GetRandomEdgePosition()
    {
        Camera cam = Camera.main;

        if (cam == null)
            return Vector3.zero;

        float height = 2f * cam.orthographicSize;
        float width = height * cam.aspect;

        float x = 0;
        float y = 0;

        int side = Random.Range(0, 4);

        switch (side)
        {
            case 0: x = Random.Range(-width / 2, width / 2); y = height / 2; break;
            case 1: x = Random.Range(-width / 2, width / 2); y = -height / 2; break;
            case 2: x = -width / 2; y = Random.Range(-height / 2, height / 2); break;
            case 3: x = width / 2; y = Random.Range(-height / 2, height / 2); break;
        }

        return new Vector3(x, y, 0);
    }
}
