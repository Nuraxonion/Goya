using UnityEngine;

// World-space collection point for XP orbs, parked on top of the HUD ink bottle.
// The bottle lives on a Screen Space - Overlay canvas, so its RectTransform position
// is already in screen pixels; this converts that to world space every frame so the
// trigger keeps sitting on the bottle through resolution changes and canvas rescaling.
public class XPBottleTarget : MonoBehaviour
{
    public static XPBottleTarget Instance { get; private set; }

    [Tooltip("RectTransform of the HUD bottle graphic to sit on. Orbs fly to its centre.")]
    public RectTransform bottleAnchor;

    public PlayerXP playerXP;

    private Camera cam;

    void Awake()
    {
        // First one wins, so a hand-placed target in the scene keeps priority over
        // anything built later by EnsureExists.
        if (Instance != null && Instance != this)
            return;

        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    // Builds the collection trigger at runtime so the feature needs no scene setup.
    // A hand-placed XPBottleTarget wins: this only builds one when none exists, and
    // otherwise just fills in references the authored object left empty.
    public static XPBottleTarget EnsureExists(RectTransform anchor, PlayerXP owner)
    {
        if (anchor == null)
        {
            Debug.LogError(
                "XPBottleTarget: no bottle anchor to aim at - XP orbs will not move. " +
                "Assign InkXPUI.orbTargetAnchor (or redFillXPBottle) on the ExperienceUI object.");
        }

        if (Instance != null)
        {
            if (Instance.bottleAnchor == null) Instance.bottleAnchor = anchor;
            if (Instance.playerXP == null) Instance.playerXP = owner;

            Instance.SnapToAnchor();
            return Instance;
        }

        GameObject go = new GameObject("XPBottleTarget (auto)");

        Rigidbody2D body = go.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;

        CircleCollider2D circle = go.AddComponent<CircleCollider2D>();
        circle.isTrigger = true;
        circle.radius = 0.5f;

        // AddComponent runs Awake immediately, so Instance is live from here on.
        XPBottleTarget target = go.AddComponent<XPBottleTarget>();
        target.bottleAnchor = anchor;
        target.playerXP = owner;

        // Placed right away rather than waiting for the first LateUpdate, so orbs
        // never briefly home in on the world origin.
        target.SnapToAnchor();

        return target;
    }

    // LateUpdate so the canvas has finished laying out for this frame.
    void LateUpdate()
    {
        SnapToAnchor();
    }

    private void SnapToAnchor()
    {
        if (bottleAnchor == null)
            return;

        // InkXPUI deactivates the bottle at full fill and during a level up. Hold the
        // last known good position rather than reading an inactive RectTransform.
        if (!bottleAnchor.gameObject.activeInHierarchy)
            return;

        if (cam == null)
            cam = Camera.main;

        if (cam == null)
            return;

        Vector3 screenPos = bottleAnchor.position;   // already screen pixels (Overlay canvas)
        screenPos.z = -cam.transform.position.z;     // 10 for the ortho camera at z = -10

        transform.position = cam.ScreenToWorldPoint(screenPos);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        xpPoint orb = other.GetComponent<xpPoint>();

        if (orb == null)
            return;

        // Only orbs the player actually picked up count. An orb that happened to drop
        // under the bottle must not pay out until it has been collected.
        if (!orb.IsCollecting)
            return;

        if (playerXP != null)
            playerXP.AddXP(orb.xpValue);

        Destroy(orb.gameObject);
    }
}
