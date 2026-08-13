using UnityEngine;

public class xpPoint : MonoBehaviour
{
    public float speed = 5f;
    public float xpValue = 10f;

    private bool isCollecting = false;
    private static bool warnedNoTarget = false;

    // Read by XPBottleTarget so an uncollected orb sitting under the bottle
    // doesn't pay out on its own.
    public bool IsCollecting => isCollecting;

    void OnMouseOver()
    {
        // Start collecting when mouse hovers over the orb
        isCollecting = true;
        Debug.Log($"XP Orb: Mouse over! Starting to collect. XP Value: {xpValue}");
    }

    void OnMouseExit()
    {
        // Optional: Stop collecting when mouse leaves
        // Comment this out if you want orbs to keep flying after hover
        // isCollecting = false;
        // Debug.Log($"XP Orb: Mouse exit!");
    }

    // Sends this orb flying at the bottle (called by spiral attack)
    public void AttractTo(float collectSpeed)
    {
        speed = collectSpeed;
        isCollecting = true;
        Debug.Log($"XP Orb attracted to bottle! Speed: {speed}");
    }

    void Update()
    {
        if (!isCollecting)
            return;

        XPBottleTarget bottle = XPBottleTarget.Instance;

        if (bottle == null)
        {
            if (!warnedNoTarget)
            {
                warnedNoTarget = true;
                Debug.LogWarning(
                    "xpPoint: no XPBottleTarget in the scene, so collected orbs cannot move.");
            }
            return;
        }

        // Move toward the bottle
        transform.position = Vector3.MoveTowards(
            transform.position,
            bottle.transform.position,
            speed * Time.deltaTime);
    }
}