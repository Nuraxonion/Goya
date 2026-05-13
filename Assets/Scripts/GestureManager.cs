using PDollarGestureRecognizer;
using System.Collections.Generic;
using UnityEngine;

public class GestureManager : MonoBehaviour
{
    public GestureDatabaseManager database;
    public GestureRecorder recorder;

    private List<Vector2> points = new List<Vector2>();
    private List<Gesture> trainingSet = new List<Gesture>();

    void Start()
    {
        trainingSet.Clear();

        foreach (var g in database.gestures)
        {
            List<Point> converted = new List<Point>();

            foreach (var p in g.points)
            {
                converted.Add(new Point(p.x, p.y, 0));
            }

            trainingSet.Add(new Gesture(converted.ToArray(), g.name));
        }
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

    public void TestGesture()
    {
        // 1. Get current drawing from recorder
        List<Vector2> inputPoints = recorder.GetSnapshot();

        if (inputPoints == null || inputPoints.Count < 10)
        {
            Debug.Log("Not enough points to test");
            return;
        }

        // 2. Convert to PDollar format
        List<Point> gesturePoints = new List<Point>();

        for (int i = 0; i < inputPoints.Count; i++)
        {
            gesturePoints.Add(new Point(inputPoints[i].x, inputPoints[i].y, 0));
        }

        // 3. Run recognizer
        Result result = PointCloudRecognizer.Classify(
            new Gesture(gesturePoints.ToArray(), "input"),
            trainingSet.ToArray()
        );

        // 4. Output result
        Debug.Log("TEST RESULT: " + result.GestureClass);
        Debug.Log("SCORE: " + result.Score);
    }
}