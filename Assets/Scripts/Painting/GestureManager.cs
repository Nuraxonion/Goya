using PDollarGestureRecognizer;
using System.Collections.Generic;
using UnityEngine;

public class GestureManager : MonoBehaviour
{
    private readonly List<Vector2> points = new List<Vector2>();
    private readonly List<Point> processedPoints = new List<Point>();
    private readonly List<Gesture> trainingSet = new List<Gesture>(); // ✔ ВОТ ЭТО ВАЖНО

    public enum AttackType { NoAttack, Circle }
    public AttackType currentAttack = AttackType.NoAttack;

    private bool isDrawing = false;
    private Vector2 lastPoint;

    public float minDistance = 2f;

    void Start()
    {
        // ✔ ШАБЛОН КРУГА
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
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            points.Clear();
            isDrawing = true;

            lastPoint = GetPoint();
            points.Add(lastPoint);
        }

        if (isDrawing && Input.GetMouseButton(0))
        {
            Vector2 p = GetPoint();

            if (Vector2.Distance(lastPoint, p) > minDistance)
            {
                points.Add(p);
                lastPoint = p;
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            isDrawing = false;
            Recognize();
        }
    }

    Vector2 GetPoint()
    {
        Vector3 m = Input.mousePosition;

        return new Vector2(
            (m.x / Screen.width) * 100f,
            (m.y / Screen.height) * 100f
        );
    }

    void Recognize()
    {
        currentAttack = AttackType.NoAttack;

        if (points.Count < 8)
        {
            points.Clear();
            return;
        }

        processedPoints.Clear();

        for (int i = 0; i < points.Count; i++)
        {
            processedPoints.Add(new Point(points[i].x, points[i].y, 0));
        }

        Result result = PointCloudRecognizer.Classify(
            new Gesture(processedPoints.ToArray(), "input"),
            trainingSet.ToArray() // ✔ теперь работает
        );

        Debug.Log($"Gesture: {result.GestureClass} | Score: {result.Score}");

        if (result.Score < 0.6f)
        {
            points.Clear();
            return;
        }

        if (result.GestureClass == "circle")
        {
            currentAttack = AttackType.Circle;
            Debug.Log("CIRCLE DETECTED!");
        }

        points.Clear();
    }
}