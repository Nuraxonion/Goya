using PDollarGestureRecognizer;
using System.Collections.Generic;
using UnityEngine;

public class GestureManager : MonoBehaviour
{
    private readonly List<Vector2> points = new List<Vector2>();
    private readonly List<Gesture> trainingSet = new List<Gesture>();
    private readonly List<Point> gesturePointsReusable = new List<Point>();

    public string currentAttack = "";

    private bool isDrawing = false;

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

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            points.Clear();
            isDrawing = true;
        }

        if (isDrawing && Input.GetMouseButton(0))
        {
            Vector3 mPos = Input.mousePosition;

            Vector2 normalized = new Vector2(
                (mPos.x / Screen.width) * 100f,
                (mPos.y / Screen.height) * 100f
            );

            points.Add(normalized);
        }

        if (Input.GetMouseButtonUp(0))
        {
            isDrawing = false;
            Recognize();
        }
    }

    void Recognize()
    {
        currentAttack = "";

        if (points.Count < 10)
        {
            points.Clear();
            return;
        }

        gesturePointsReusable.Clear();

        for (int i = 0; i < points.Count; i++)
        {
            gesturePointsReusable.Add(new Point(points[i].x, points[i].y, 0));
        }

        Result result = PointCloudRecognizer.Classify(
            new Gesture(gesturePointsReusable.ToArray(), "input"),
            trainingSet.ToArray()
        );

        if (result.Score < 0.75f)
        {
            points.Clear();
            return;
        }

        if (result.GestureClass == "circle")
        {
            currentAttack = "circle";
        }

        Debug.Log($"Gesture: {result.GestureClass} | Score: {result.Score}");
        Debug.Log($"Attack: {currentAttack}");

        points.Clear();
    }
}