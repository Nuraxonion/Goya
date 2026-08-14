using UnityEngine;

// Hair enemy: arrives as a tight cluster during a swarm event, darts erratically
// around the play area as a pack for a while, then turns and drives straight at
// the player.
//
// Cohesion without flocking: every member of one group is handed the same
// groupSeed, and the swarm's centre is a pure function of (groupSeed, time). They
// therefore share an identical centre path with no neighbour queries, no averaging
// and no manager object - each one just adds its own noise offset on top, which is
// what makes them dart individually while still reading as one swarm.
public class HairMoveScript : EnemyMovement
{
    [Header("Erratic Phase")]
    [Tooltip("Seconds spent darting around before the swarm commits to the player. Uses scaled time, so it freezes with the pause menu and the upgrade panel.")]
    public float erraticDuration = 20f;

    [Tooltip("Speed multiplier while roaming. They move noticeably faster than they do once they commit.")]
    public float erraticSpeedMultiplier = 1.8f;

    [Tooltip("How far past the screen edge the swarm may roam, as a fraction of the view. 0.15 lets them dip just off-screen and come straight back.")]
    [Range(0f, 1f)]
    public float roamOvershoot = 0.15f;

    [Tooltip("How quickly the swarm's shared centre drifts around the play area.")]
    public float wanderFrequency = 0.35f;

    [Tooltip("How quickly each individual darts around the swarm centre.")]
    public float personalFrequency = 1.6f;

    [Tooltip("How far each individual strays from the swarm centre, in world units.")]
    public float personalRadius = 1.2f;

    // Stamped by EnemySpawner.SpawnCluster. Shared across one group; unique per member.
    [HideInInspector] public float groupSeed;
    [HideInInspector] public float memberSeed;

    private float phaseTimer;
    private Camera cam;
    private Vector3 lastPosition;

    /// <summary>True once the swarm has stopped roaming and is closing on the player.</summary>
    public bool IsChasing => phaseTimer >= erraticDuration;

    private void Awake()
    {
        cam = Camera.main;
        lastPosition = transform.position;
    }

    protected override void Move(float deltaTime)
    {
        phaseTimer += deltaTime;

        lastPosition = transform.position;

        if (IsChasing)
            ChaseTarget(deltaTime);
        else
            Roam(deltaTime);
    }

    private void ChaseTarget(float deltaTime)
    {
        float distance = Vector2.Distance(transform.position, target.position);

        if (distance <= stopDistance)
            return;

        Vector3 direction = (target.position - transform.position).normalized;
        transform.position += direction * speed * deltaTime;
    }

    private void Roam(float deltaTime)
    {
        Vector3 destination = SwarmCentre() + PersonalOffset();

        transform.position = Vector3.MoveTowards(
            transform.position,
            destination,
            speed * erraticSpeedMultiplier * deltaTime);
    }

    // The shared drift path. Perlin rather than random waypoints so the motion is
    // continuous - random targets would produce visible stop-turn-go stutter.
    private Vector3 SwarmCentre()
    {
        GetRoamBounds(out float halfWidth, out float halfHeight);

        float t = Time.time * wanderFrequency;

        float x = (Mathf.PerlinNoise(groupSeed, t) - 0.5f) * 2f * halfWidth;
        float y = (Mathf.PerlinNoise(groupSeed + 31.7f, t) - 0.5f) * 2f * halfHeight;

        return new Vector3(x, y, 0f);
    }

    private Vector3 PersonalOffset()
    {
        float t = Time.time * personalFrequency;

        float x = (Mathf.PerlinNoise(memberSeed, t) - 0.5f) * 2f * personalRadius;
        float y = (Mathf.PerlinNoise(memberSeed + 57.3f, t) - 0.5f) * 2f * personalRadius;

        return new Vector3(x, y, 0f);
    }

    // Same camera maths EnemySpawner uses, widened by roamOvershoot so the swarm
    // can stray a little past the edge of the view.
    private void GetRoamBounds(out float halfWidth, out float halfHeight)
    {
        if (cam == null)
            cam = Camera.main;

        if (cam == null)
        {
            halfWidth = 8f;
            halfHeight = 5f;
            return;
        }

        halfHeight = cam.orthographicSize * (1f + roamOvershoot);
        halfWidth = cam.orthographicSize * cam.aspect * (1f + roamOvershoot);
    }

    // Face the way it is actually travelling while roaming - facing the player
    // while darting sideways looks wrong. Once chasing, the base behaviour (face
    // the target) is correct again.
    protected override void FixedUpdate()
    {
        if (target == null)
            return;

        if (IsChasing)
        {
            FaceTarget();
            return;
        }

        FaceDirection(transform.position.x - lastPosition.x);
    }
}
