using PDollarGestureRecognizer;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class GestureManager : MonoBehaviour
{
    private List<Vector2> points = new List<Vector2>();

    [Header("Gesture Source (Gesture Editor)")]
    [Tooltip("Recognizer driven by the Gesture Editor's saved gestures (gestures.json). " +
             "This is the single source of truth for gesture definitions.")]
    public GestureRecognizerManager recognizer;

    [Header("Recognition Settings")]
    [Tooltip("Minimum captured points required before a stroke is classified.")]
    public int minPoints = 10;
    [Tooltip("Minimum P-Dollar confidence (0-1) required to accept a match.")]
    public float scoreThreshold = 0.75f;

    public GameObject upgradePanel;

    [Header("Brush Settings")]
    public GameObject brushPrefab;
    public float brushSize = 0.2f;
    public float spacing = 0.1f;

    private Vector3 lastPos;

    public enum AttackType
    {
        NoAttack,
        Circle,
        Bracket
    }

    // Data-driven mapping: a recognized gesture name -> the attack it triggers.
    // Editable in the Inspector so new gesture/attack pairs need no code changes.
    [System.Serializable]
    public class GestureAttackBinding
    {
        public string gestureName;
        public AttackType attack = AttackType.NoAttack;
    }

    [Header("Gesture -> Attack Mapping")]
    public List<GestureAttackBinding> bindings = new List<GestureAttackBinding>();

    public AttackType currentAttack = AttackType.NoAttack;

    // Event-driven hook: raised when a gesture clears the score threshold.
    // Payload: recognized gesture name, confidence score.
    public event System.Action<string, float> OnGestureRecognized;

    void Start()
    {
        if (recognizer == null)
            recognizer = FindObjectOfType<GestureRecognizerManager>();

        if (recognizer == null)
            Debug.LogError("[GestureManager] No GestureRecognizerManager assigned/found. " +
                           "Gestures from the editor file cannot be recognized.");
    }

    // Looks up the attack mapped to a recognized gesture name (NoAttack if unmapped).
    private AttackType ResolveAttack(string gestureName)
    {
        foreach (var binding in bindings)
        {
            if (binding.gestureName == gestureName)
                return binding.attack;
        }
        return AttackType.NoAttack;
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
            Vector2 normalizedPoint = new Vector2(
                (Input.mousePosition.x / Screen.width) * 100f,
                (Input.mousePosition.y / Screen.height) * 100f
            );

            points.Add(normalizedPoint);

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
        currentAttack = AttackType.NoAttack;

        if (points.Count < minPoints)
        {
            points.Clear();
            return;
        }

        if (recognizer == null)
        {
            points.Clear();
            return;
        }

        // Match the captured stroke against the editor's gesture definitions.
        Result result = recognizer.Recognize(points);

        if (result.Score < scoreThreshold)
        {
            points.Clear();
            return;
        }

        // Map the recognized gesture name to its attack via the data-driven bindings.
        currentAttack = ResolveAttack(result.GestureClass);

        Debug.Log($"Gesture: {result.GestureClass}");
        Debug.Log($"Score: {result.Score}");
        Debug.Log($"Attack: {currentAttack}");

        points.Clear();

        // Event-driven: notify subscribers (e.g. PlayerAttack) of the recognized gesture.
        OnGestureRecognized?.Invoke(result.GestureClass, result.Score);
    }
}