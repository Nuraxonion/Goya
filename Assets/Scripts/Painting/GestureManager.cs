using PDollarGestureRecognizer;
using System.Collections.Generic;
using UnityEngine;

public class GestureManager : MonoBehaviour
{
    private readonly List<Vector2> points = new List<Vector2>();
    private readonly List<Point> gesturePoints = new List<Point>();
    private readonly List<Gesture> trainingSet = new List<Gesture>();

    // Recognized gesture name -> attack id, loaded from the external mapping file.
    private Dictionary<string, string> gestureToAttack = new Dictionary<string, string>();

    public AttackDuration attackDuration;
    public GestureMultiplierManager gestureMultiplierManager;

    // Only needed by fire-once attacks (Spiral); resolved lazily so the scene
    // needs no extra wiring.
    public PlayerAttack playerAttack;

    [Header("Feedback")]
    [Tooltip("Shown at the cursor when a stroke is too sloppy to recognize.")]
    public string missText = "Miss";
    [Tooltip("Shown when a gesture is recognized but has no attack mapped to it yet.")]
    public string noMatchText = "No Match";
    [Tooltip("Shown when the recognized attack exists but is not unlocked yet.")]
    public string lockedText = "Locked";
    [Tooltip("Shown when a fire-once attack is recognized but still on cooldown.")]
    public string cooldownText = "Cooldown";
    [Tooltip("Shown when the spiral successfully pulls in the XP orbs.")]
    public string collectText = "Collect";

    [Tooltip("Strokes shorter than this are treated as a stray click and stay silent.")]
    public int minPointsForFeedback = 3;

    // Resolved at runtime so no extra scene wiring is needed.
    private GestureRankCursorUI rankUI;
    private PlayerStats playerStats;

    [Header("Gesture Recognition")]
    [Tooltip("Minimum $P confidence (0-1) required to accept a recognized gesture.")]
    [Range(0f, 1f)] public float recognitionThreshold = 0.75f;
    [Tooltip("Minimum distance in screen pixels between captured points; filters out dense duplicates.")]
    public float minPointDistance = 5f;
    [Tooltip("Gesture names expected in the gesture file; a warning is logged if any are missing.")]
    public string[] expectedGestures = { "check", "circle", "spiral", "lightning", "butterfly" };

    // Attack id of the currently recognized gesture (AttackIds.None when idle).
    // Driven by data loaded from gestures.json + gesture_attack_map.json instead
    // of a hardcoded switch, so new gesture->attack pairs need no code change here.
    public string currentAttack = AttackIds.None;

    void Start()
    {
        // Prefer the already-wired reference: the CursorRankUI object starts
        // inactive (it activates itself when a rank is shown), so a plain
        // FindObjectOfType would miss it.
        if (gestureMultiplierManager != null)
            rankUI = gestureMultiplierManager.cursorUI;

        if (rankUI == null)
            rankUI = FindObjectOfType<GestureRankCursorUI>(true);

        playerStats = FindObjectOfType<PlayerStats>(true);

        if (playerStats == null)
            Debug.LogWarning("[GestureManager] No PlayerStats found - locked attacks cannot be filtered.");

        LoadGestures();
    }

    // Loads gesture templates and the gesture->attack mapping from the external
    // data files, then validates that the expected gestures are present.
    void LoadGestures()
    {
        GestureDatabase database = GestureStorageManager.LoadDatabase();

        trainingSet.Clear();
        foreach (GestureEntry entry in database.gestures)
        {
            foreach (GestureSample sample in entry.samples)
            {
                Point[] pts = new Point[sample.points.Count];
                for (int i = 0; i < sample.points.Count; i++)
                    pts[i] = new Point(sample.points[i].x, sample.points[i].y, 0);

                trainingSet.Add(new Gesture(pts, entry.name));
            }
        }

        gestureToAttack = GestureAttackMap.Load();

        ValidateGestures(database);

        Debug.Log($"[GestureManager] Loaded {trainingSet.Count} samples across " +
                  $"{database.gestures.Count} gestures with {gestureToAttack.Count} attack mappings.");
    }

    // Confirms the loaded gestures line up with the gesture editor definitions and
    // that mappings point at gestures that actually exist.
    void ValidateGestures(GestureDatabase database)
    {
        foreach (string expected in expectedGestures)
        {
            if (!database.gestures.Exists(g => g.name == expected))
                Debug.LogWarning($"[GestureManager] Expected gesture '{expected}' is missing from the gesture file.");
        }

        foreach (KeyValuePair<string, string> map in gestureToAttack)
        {
            if (string.IsNullOrEmpty(map.Value))
                continue; // reserved mapping (e.g. spiral / butterfly) — intentionally has no attack yet

            if (!database.gestures.Exists(g => g.name == map.Key))
                Debug.LogWarning($"[GestureManager] Mapping references gesture '{map.Key}' which is not in the gesture file.");
        }
    }

    // Called by BrushStrokeManager while the stroke is being drawn. Points are kept
    // in raw screen pixels so the aspect ratio is preserved: the $P recognizer scales
    // uniformly (Math.Max of width/height), so feeding width/height-normalized points
    // would distort shapes relative to the stored templates.
    public void AddPoint(Vector2 screenPos)
    {
        if (points.Count == 0 || Vector2.Distance(points[^1], screenPos) > minPointDistance)
        {
            points.Add(screenPos);
        }
    }

    // Every path out of here reports something to the player. A stroke that
    // silently does nothing is indistinguishable from a broken game, which is
    // exactly how the "sometimes nothing happens" bug felt.
    public void Recognize()
    {
        currentAttack = AttackIds.None;

        int strokeLength = points.Count;

        if (strokeLength < 10 || trainingSet.Count == 0)
        {
            // A stray click is not a failed gesture, so keep it quiet.
            if (strokeLength >= minPointsForFeedback)
            {
                ShowFeedback(missText);
                Debug.Log($"Gesture: none | stroke too short ({strokeLength} points) | Attack: none");
            }

            points.Clear();
            return;
        }

        gesturePoints.Clear();
        for (int i = 0; i < points.Count; i++)
            gesturePoints.Add(new Point(points[i].x, points[i].y, 0));

        Result result = PointCloudRecognizer.Classify(
            new Gesture(gesturePoints.ToArray(), "input"),
            trainingSet.ToArray()
        );

        if (result.Score < recognitionThreshold)
        {
            ShowFeedback(missText);
            Debug.Log($"Gesture: {result.GestureClass} | Score: {result.Score} | below threshold {recognitionThreshold} | Attack: none");

            points.Clear();
            return;
        }

        // Dynamic mapping lookup replaces the old hardcoded switch statement.
        if (!gestureToAttack.TryGetValue(result.GestureClass, out string attackId)
            || string.IsNullOrEmpty(attackId))
        {
            // The $P recognizer always returns its nearest template, so an
            // unmapped gesture is a normal outcome, not an error.
            ShowFeedback(noMatchText);
            Debug.Log($"Gesture: {result.GestureClass} | Score: {result.Score} | no attack mapped | Attack: none");

            points.Clear();
            return;
        }

        if (playerStats != null && !playerStats.IsAttackAvailable(attackId))
        {
            // Locked or unimplemented: say so instead of showing a rank and a
            // duration bar for an attack that can never fire.
            ShowFeedback(lockedText);
            Debug.Log($"Gesture: {result.GestureClass} | Score: {result.Score} | {attackId} not available | Attack: none");

            points.Clear();
            return;
        }

        // Fire-once attacks resolve here and never reach AttackDuration, so they
        // have no duration and the Multi-Tasking upgrades neither extend them nor
        // get refreshed by them. currentAttack deliberately stays None: only
        // AttackDuration.ClearAll() ever resets it, so setting it here would leave
        // it stuck on this id forever.
        if (attackId == AttackIds.Spiral)
        {
            if (playerAttack == null)
                playerAttack = FindObjectOfType<PlayerAttack>();

            bool cast = playerAttack != null && playerAttack.TryCastSpiral();

            ShowFeedback(cast ? collectText : cooldownText);
            Debug.Log($"Gesture: {result.GestureClass} | Score: {result.Score} | Attack: {attackId} | cast: {cast}");

            points.Clear();
            return;
        }

        currentAttack = attackId;

        float multiplier = 1f;

        if (gestureMultiplierManager != null)
        {
            gestureMultiplierManager.CalculateMultiplier(result.Score);
            multiplier = gestureMultiplierManager.GetDamageMultiplier();
        }

        // The multiplier is handed over here so this attack keeps the accuracy of
        // the stroke that cast it, even once another attack is drawn alongside it.
        // The stroke's position goes with it for the same reason, and because this
        // is the last moment it exists at all - points is cleared on the way out of
        // every path through this method, and the recognizer normalises position
        // away on its own copy. Lightning lands there instead of on the cursor.
        if (attackDuration != null)
        {
            // No camera to unproject with is the only way this fails, and world origin
            // is a real place an attack must not silently land on.
            if (!TryGetStrokeCenter(out Vector2 strokeCenter) && playerStats != null)
                strokeCenter = playerStats.transform.position;

            attackDuration.StartAttackTimer(currentAttack, multiplier, strokeCenter);
        }

        Debug.Log($"Gesture: {result.GestureClass} | Score: {result.Score} | Attack: {currentAttack}");

        points.Clear();
    }

    // Middle of the stroke's bounding box, in world space. Deliberately the box centre
    // rather than an average of the points: a shape drawn with a dense cluster on one
    // side - a zigzag bolt, say - would drag a centroid off where it visually sits.
    // Uses BrushStrokeManager.MousePos' z = 10 convention so the unprojection matches
    // the plane the brush trail is drawn on.
    bool TryGetStrokeCenter(out Vector2 world)
    {
        world = Vector2.zero;

        Camera cam = Camera.main;

        if (cam == null || points.Count == 0)
            return false;

        Vector2 min = points[0];
        Vector2 max = points[0];

        for (int i = 1; i < points.Count; i++)
        {
            min = Vector2.Min(min, points[i]);
            max = Vector2.Max(max, points[i]);
        }

        Vector2 screenCenter = (min + max) * 0.5f;

        world = cam.ScreenToWorldPoint(
            new Vector3(screenCenter.x, screenCenter.y, 10f)
        );

        return true;
    }

    // Reuses the rank label the multiplier system already shows at the cursor;
    // it colours anything that isn't a known rank white.
    void ShowFeedback(string message)
    {
        if (rankUI != null)
            rankUI.ShowRank(message);
    }

    public void Clear()
    {
        points.Clear();
    }
}
