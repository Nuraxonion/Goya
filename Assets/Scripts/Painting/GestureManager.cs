using PDollarGestureRecognizer;
using System.Collections.Generic;
using UnityEngine;

public class GestureManager : MonoBehaviour
{
    private readonly List<Vector2> points = new List<Vector2>();
    private readonly List<Point> gesturePoints = new List<Point>();
    private readonly List<Gesture> trainingSet = new List<Gesture>();

    public string currentAttack = "";

    void Start()
    {
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

    public void AddPoint(Vector2 screenPos)
    {
        Vector2 normalized = new Vector2(
            (screenPos.x / Screen.width) * 100f,
            (screenPos.y / Screen.height) * 100f
        );

        if (points.Count == 0 || Vector2.Distance(points[^1], normalized) > 1f)
        {
            points.Add(normalized);
        }
    }

    public void Recognize()
    {
        currentAttack = "";

        if (points.Count < 10)
        {
            points.Clear();
            return;
        }

        gesturePoints.Clear();

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

        if (result.GestureClass == "circle")
        {
            currentAttack = AttackIds.Fireball;
        }
    }
    public void Clear()
    {
        points.Clear();
    }
}