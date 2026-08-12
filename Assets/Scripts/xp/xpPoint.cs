using UnityEngine;

public class xpPoint : MonoBehaviour
{
    public float speed = 5f;

    private bool isMouseOver = false;

    public float xpValue = 10f;

    // Set by the Spiral attack - see PlayerAttack.TryCastSpiral().
    private bool isCollecting = false;

    private static bool warnedNoTarget = false;

    // Read by XPBottleTarget so an uncollected orb sitting under the bottle
    // doesn't pay out on its own.
    public bool IsCollecting => isMouseOver || isCollecting;

    void OnMouseOver()
    {
        isMouseOver = true;
    }

    // Sends this orb flying at the bottle. The destination is not passed in:
    // every orb homes on the single XPBottleTarget in the scene.
    public void AttractTo(float collectSpeed)
    {
        speed = collectSpeed;
        isCollecting = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (!IsCollecting)
            return;

        XPBottleTarget bottle = XPBottleTarget.Instance;

        if (bottle == null)
        {
            // Warned rather than ignored: a missing target looks exactly like a dead
            // hover from the player's side, with nothing in the Console to explain it.
            if (!warnedNoTarget)
            {
                warnedNoTarget = true;
                Debug.LogWarning(
                    "xpPoint: no XPBottleTarget in the scene, so collected orbs cannot move. " +
                    "InkXPUI.Start() normally creates one - check that the ExperienceUI object is active.");
            }

            return;
        }

        // Scaled time on purpose: orbs must freeze completely while the upgrade
        // panel holds timeScale at 0, including ones already in flight from a
        // Spiral cast a moment earlier. PlayerXP.pendingXP covers the orb that
        // lands on the exact frame the level up fires.
        transform.position = Vector3.MoveTowards(
            transform.position,
            bottle.transform.position,
            speed * Time.deltaTime);
    }
}
