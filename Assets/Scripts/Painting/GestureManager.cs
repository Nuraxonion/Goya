using PDollarGestureRecognizer;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class GestureManager : MonoBehaviour
{
    private List<Vector2> points = new List<Vector2>();
    private List<Gesture> trainingSet = new List<Gesture>();

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

    public AttackType currentAttack = AttackType.NoAttack;

    void Start()
    {
        // Circle
        trainingSet.Add(new Gesture(new Point[]
        {
            new Point(50, 0, 0),
            new Point(75, 10, 0),
            new Point(95, 35, 0),
            new Point(95, 65, 0),
            new Point(75, 90, 0),
            new Point(50, 100, 0),
            new Point(25, 90, 0),
            new Point(5, 65, 0),
            new Point(5, 35, 0),
            new Point(25, 10, 0),
            new Point(50, 0, 0)
        }, "circle"));

        trainingSet.Add(new Gesture(new Point[]
        {
            new Point(80, 0, 0),
            new Point(20, 0, 0),
            new Point(20, 25, 0),
            new Point(20, 50, 0),
            new Point(20, 75, 0),
            new Point(20, 100, 0),
            new Point(80, 100, 0)
        }, "left_bracket"));

        trainingSet.Add(new Gesture(new Point[]
        {
            new Point(20, 0, 0),
            new Point(80, 0, 0),
            new Point(80, 25, 0),
            new Point(80, 50, 0),
            new Point(80, 75, 0),
            new Point(80, 100, 0),
            new Point(20, 100, 0)
        }, "right_bracket"));
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

        if (points.Count < 10)
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

        if (result.Score < 0.75f)
        {
            points.Clear();
            return;
        }

        switch (result.GestureClass)
        {
            case "circle":
                currentAttack = AttackType.Circle;
                break;

            case "left_bracket":
            case "right_bracket":
                currentAttack = AttackType.Bracket;
                break;

            default:
                currentAttack = AttackType.NoAttack;
                break;
        }

        Debug.Log($"Gesture: {result.GestureClass}");
        Debug.Log($"Score: {result.Score}");
        Debug.Log($"Attack: {currentAttack}");

        points.Clear();
    }
}