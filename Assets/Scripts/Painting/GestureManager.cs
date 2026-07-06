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

    [Header("Gesture Recognition")]
    [Tooltip("Minimum $P confidence (0-1) required to accept a recognized gesture.")]
    [Range(0f, 1f)] public float recognitionThreshold = 0.75f;
    [Tooltip("Minimum distance in screen pixels between captured points; filters out dense duplicates.")]
    public float minPointDistance = 5f;
    [Tooltip("Gesture names expected in the gesture file; a warning is logged if any are missing.")]
    public string[] expectedGestures = { "check", "circle", "spiral", "butterfly" };

    // Attack id of the currently recognized gesture (AttackIds.None when idle).
    // Driven by data loaded from gestures.json + gesture_attack_map.json instead
    // of a hardcoded switch, so new gesture->attack pairs need no code change here.
    public string currentAttack = AttackIds.None;

    void Start()
    {
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

    public void Recognize()
    {
        currentAttack = AttackIds.None;

        if (points.Count < 10 || trainingSet.Count == 0)
        {
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
            points.Clear();
            return;
        }

        // Dynamic mapping lookup replaces the old hardcoded switch statement.
        if (gestureToAttack.TryGetValue(result.GestureClass, out string attackId)
            && !string.IsNullOrEmpty(attackId))
        {
            currentAttack = attackId;
            if (attackDuration != null)
                attackDuration.StartAttackTimer(currentAttack);
        }

        Debug.Log($"Gesture: {result.GestureClass} | Score: {result.Score} | Attack: {currentAttack}");

        points.Clear();
    }

    public void Clear()
    {
        points.Clear();
    }
}
