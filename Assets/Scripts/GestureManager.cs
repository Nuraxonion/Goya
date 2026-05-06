using PDollarGestureRecognizer;
using System.Collections.Generic;
using UnityEngine;

public class GestureManager : MonoBehaviour
{
    private List<Vector2> points = new List<Vector2>();
    private List<Gesture> trainingSet = new List<Gesture>();

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
        if (Input.GetMouseButton(0))
        {
            Vector2 normalizedPoint = new Vector2(
                (Input.mousePosition.x / Screen.width) * 100f,
                (Input.mousePosition.y / Screen.height) * 100f
            );

            points.Add(normalizedPoint);
        }

        if (Input.GetMouseButtonUp(0))
        {
            Recognize();
        }
    }

    void Recognize()
    {
        if (points.Count < 10) return;

        // Convert to PDollar format (depends on repo)
        List<Point> gesturePoints = new List<Point>();

        for (int i = 0; i < points.Count; i++)
        {
            gesturePoints.Add(new Point(points[i].x, points[i].y, 0));
        }

        // Run recognizer
        Result result = PointCloudRecognizer.Classify(
        new Gesture(gesturePoints.ToArray(), "input"),
        trainingSet.ToArray()
        );

        // ⭐ STEP 3: confidence filter (ADD HERE)
        if (result.Score < 0.75f)
        {
            Debug.Log("No confident match");
            points.Clear();
            return;
        }

        Debug.Log("Gesture: " + result.GestureClass);
        Debug.Log("Score: " + result.Score);

        points.Clear();
    }
}