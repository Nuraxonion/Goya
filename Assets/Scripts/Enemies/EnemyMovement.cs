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

    public void Stun(float duration)
    {
        StartCoroutine(StunRoutine(duration));
    }

    private IEnumerator StunRoutine(float duration)
    {
        isStunned = true;

        yield return new WaitForSeconds(duration);

        isStunned = false;
    }

    protected virtual void Update()
    {
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
