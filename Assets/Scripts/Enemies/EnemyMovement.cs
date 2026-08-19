using System.Collections;
using UnityEngine;

// Shared base for every enemy movement type.
//
// Before this existed, EnemyMoveScript and BatMoveScript were copy-pasted and four
// separate call sites hardcoded "one of exactly those two types" - so any new enemy
// silently lost its lightning stun, its knockback direction and its death-effect
// mirroring. Those sites now ask for EnemyMovement and work for every type.
//
// Subclasses implement Move(); the stun guard, the null-target guard, the facing
// flip and the knockback direction all live here.
public abstract class EnemyMovement : MonoBehaviour
{
    public Transform target;
    public float speed = 3f;

    [Tooltip("The enemy stops closing once it is this near the target.")]
    public float stopDistance = 0f;

    protected bool isStunned = false;

    // Readable from outside so an attack can tell an already-stunned enemy from a
    // fresh one - the lightning chain upgrade only fires off enemies that were
    // already stunned when the blast landed.
    public bool IsStunned => isStunned;

    // When the current stun runs out. One coroutine watches this instead of one
    // coroutine per Stun call: overlapping stuns used to each own the flag, so a
    // short stun landing on top of a long one cleared it early and cut the long
    // stun short. Extending the deadline never shortens an existing stun.
    private float stunEndTime;
    private bool stunRoutineRunning;

    public void Stun(float duration)
    {
        if (duration <= 0f)
            return;

        stunEndTime = Mathf.Max(stunEndTime, Time.time + duration);

        if (stunRoutineRunning)
            return;

        StartCoroutine(StunRoutine());
    }

    private IEnumerator StunRoutine()
    {
        stunRoutineRunning = true;
        isStunned = true;

        while (Time.time < stunEndTime)
            yield return null;

        isStunned = false;
        stunRoutineRunning = false;
    }


    // Health-band artwork. Enemies degrade with the PLAYER's health, not their own -
    // the same world-wide tell the player's idle and the soundtrack already use. A
    // subclass opts in by naming its clip family; the Animator states are the family
    // suffix prefixed with "1" / "2" / "3". Enemies that leave this null keep whatever
    // their controller's default state is.
    protected virtual string WalkStateSuffix => null;

    [Tooltip("For enemies whose art has no 1/2/3 band variants: stay on the controller's default state instead of banding.")]
    [SerializeField] private bool disableHealthBanding;

    private Animator bandAnimator;
    private bool bandAnimatorResolved;
    private PlayerHealth bandPlayerHealth;
    private int currentHealthBand = -1;   // -1 forces the first update

    private void UpdateHealthBandAnimation()
    {
        if (disableHealthBanding)
            return;

        string suffix = WalkStateSuffix;

        if (suffix == null)
            return;

        if (!bandAnimatorResolved)
        {
            bandAnimator = GetComponent<Animator>();
            bandAnimatorResolved = true;
        }

        // target is stamped by EnemySpawner.SpawnAt after Instantiate, so the player
        // cannot be resolved in Awake - keep trying until it lands.
        if (bandPlayerHealth == null && target != null)
            bandPlayerHealth = target.GetComponent<PlayerHealth>();

        if (bandAnimator == null || bandPlayerHealth == null)
            return;

        int band = bandPlayerHealth.CurrentHealthBand;

        if (band == currentHealthBand)
            return;

        currentHealthBand = band;

        // Every clip in one family shares a length, so carrying the playback position
        // over swaps only the artwork - the walk cycle never hitches back to frame 0.
        float t = bandAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime % 1f;
        bandAnimator.Play(band + suffix, 0, t);
    }

    protected virtual void Update()
    {
        // Ahead of the stun guard on purpose: a stunned enemy should still track
        // the player's health band. It froze mid-stun while the bat owned this.
        UpdateHealthBandAnimation();

        if (isStunned)
            return;

        if (target == null)
            return;

        Move(Time.deltaTime);
    }

    /// <summary>Called once per frame while not stunned and with a live target.</summary>
    protected abstract void Move(float deltaTime);

    protected virtual void FixedUpdate()
    {
        // Guarded, unlike the two original scripts - both dereferenced target here
        // with no check and threw every physics step if it was ever unassigned.
        if (target == null)
            return;

        FaceTarget();
    }

    // Convention the rest of the code depends on: localScale.x < 0 means the enemy
    // is facing right. EnemyHealth.Die reads this to mirror the death effect.
    protected void FaceTarget()
    {
        FaceDirection(target.position.x - transform.position.x);
    }

    protected void FaceDirection(float deltaX)
    {
        if (Mathf.Approximately(deltaX, 0f))
            return;

        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * (deltaX > 0f ? -1f : 1f);
        transform.localScale = scale;
    }

    public Vector2 GetKnockbackDirection()
    {
        if (target == null) return Vector2.right;

        return (transform.position - target.position).normalized;
    }
}
