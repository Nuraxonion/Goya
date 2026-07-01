using PDollarGestureRecognizer;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class GestureManager : MonoBehaviour
{
    private List<Vector2> points = new List<Vector2>();
    private List<Gesture> trainingSet = new List<Gesture>();

    // Recognized gesture name -> attack id, loaded from the external mapping file.
    private Dictionary<string, string> gestureToAttack = new Dictionary<string, string>();

    public GameObject upgradePanel;
    public AttackDuration attackDuration;

    [Header("Brush Settings")]
    public GameObject brushPrefab;
    public float brushSize = 0.2f;
    public float spacing = 0.1f;

    [Header("Gesture Recognition")]
    [Tooltip("Minimum $P confidence (0-1) required to accept a recognized gesture.")]
    [Range(0f, 1f)] public float recognitionThreshold = 0.75f;
    [Tooltip("Gesture names expected in the gesture file; a warning is logged if any are missing.")]
    public string[] expectedGestures = { "check", "circle", "spiral", "butterfly" };

    private Vector3 lastPos;

    // Attack id of the currently recognized gesture (AttackIds.None when idle).
    // Driven by data loaded from gestures.json + gesture_attack_map.json instead
    // of a hardcoded enum, so new gesture->attack pairs need no code change here.
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

    void Update()
    {
        if (upgradePanel != null && upgradePanel.activeSelf)
            return;

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        // START DRAW
        if (Input.GetMouseButtonDown(0))
        {
            points.Clear();
            lastPos = Vector3.zero;
        }

        // DRAW
        if (Input.GetMouseButton(0))
        {
            // Capture raw screen-pixel coordinates so the candidate keeps its aspect
            // ratio. The $P recognizer normalizes scale/translation itself, so this
            // matches the editor templates (which are stored in draw-area pixels).
            points.Add(new Vector2(Input.mousePosition.x, Input.mousePosition.y));

            Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(
                Input.mousePosition.x,
                Input.mousePosition.y,
                10f
            ));

            if (brushPrefab != null)
            {
                if (Vector3.Distance(worldPos, lastPos) > spacing)
                {
                    GameObject stamp = Instantiate(brushPrefab, worldPos, Quaternion.identity);

                    float size = brushSize * Random.Range(0.8f, 1.2f);
                    stamp.transform.localScale = new Vector3(size, size, 1);

                    stamp.transform.rotation = Quaternion.Euler(0, 0, Random.Range(0, 360));

                    lastPos = worldPos;

                    Destroy(stamp, 5f);
                }
            }
        }

        // END DRAW
        if (Input.GetMouseButtonUp(0))
        {
            Recognize();
        }
    }

    void Recognize()
    {
        currentAttack = AttackIds.None;

        if (points.Count < 10 || trainingSet.Count == 0)
        {
            points.Clear();
            return;
        }

        List<Point> gesturePoints = new List<Point>();

        for (int i = 0; i < points.Count; i++)
        {
            gesturePoints.Add(new Point(points[i].x, points[i].y, 0));
        }

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
            attackDuration.StartAttackTimer(currentAttack);
        }

        Debug.Log($"Gesture: {result.GestureClass}");
        Debug.Log($"Score: {result.Score}");
        Debug.Log($"Attack: {currentAttack}");

        points.Clear();
    }
}
