using System.Collections.Generic;
using PDollarGestureRecognizer;
using UnityEngine;

public class GestureRecognizerManager : MonoBehaviour
{
    public GestureStorageManager storage;

    private List<Gesture> trainingSet = new();

    void Start()
    {
        Reload();
    }

    public void Reload()
    {
        trainingSet.Clear();

        foreach (var g in storage.database.gestures)
        {
            foreach (var sample in g.samples)
            {
                List<Point> pts = new();

                foreach (var p in sample.points)
                    pts.Add(new Point(p.x, p.y, 0));

                trainingSet.Add(new Gesture(pts.ToArray(), g.name));
            }
        }
    }

    public Result Recognize(List<Vector2> input)
    {
        List<Point> pts = new();

        foreach (var p in input)
            pts.Add(new Point(p.x, p.y, 0));

        return PointCloudRecognizer.Classify(
            new Gesture(pts.ToArray(), "input"),
            trainingSet.ToArray()
        );
    }
}