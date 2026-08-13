using UnityEngine;

public class XPBottleTarget : MonoBehaviour
{
    public static XPBottleTarget Instance { get; private set; }

    [Tooltip("RectTransform of the HUD bottle graphic to sit on. Orbs fly to its centre.")]
    public RectTransform bottleAnchor;

    public PlayerXP playerXP;

    private Camera cam;

    void Awake()
    {
        if (Instance != null && Instance != this)
            return;

        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public static XPBottleTarget EnsureExists(RectTransform anchor, PlayerXP owner)
    {
        if (anchor == null)
        {
            Debug.LogError("XPBottleTarget: no bottle anchor to aim at!");
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
        circle.radius = 0.8f; // Increased radius for better collection

        XPBottleTarget target = go.AddComponent<XPBottleTarget>();
        target.bottleAnchor = anchor;
        target.playerXP = owner;

        target.SnapToAnchor();

        return target;
    }

    void LateUpdate()
    {
        SnapToAnchor();
    }

    private void SnapToAnchor()
    {
        if (bottleAnchor == null)
            return;

        if (!bottleAnchor.gameObject.activeInHierarchy)
            return;

        if (cam == null)
            cam = Camera.main;

        if (cam == null)
            return;

        Vector3 screenPos = bottleAnchor.position;
        screenPos.z = -cam.transform.position.z;

        transform.position = cam.ScreenToWorldPoint(screenPos);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        xpPoint orb = other.GetComponent<xpPoint>();

        if (orb == null)
            return;

        if (!orb.IsCollecting)
        {
            Debug.Log("XP Orb not collecting yet - waiting for hover");
            return;
        }

        if (playerXP != null && playerXP.IsLevelingUp())
        {
            Debug.Log("XP Orb reached bottle but player is leveling up - destroying orb");
            Destroy(orb.gameObject);
            return;
        }

        if (playerXP != null)
        {
            playerXP.AddXP(orb.xpValue);
            Debug.Log($"XP Orb collected! +{orb.xpValue} XP");
        }

        Destroy(orb.gameObject);
    }
}