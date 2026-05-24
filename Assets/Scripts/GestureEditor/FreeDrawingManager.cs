using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class FreeDrawingManager : MonoBehaviour
{
    [Header("Stroke Setup")]
    public LineRenderer strokePrefab;

    [Header("Visual Settings")]
    public Color brushColor = Color.magenta;
    public float lineWidth = 0.1f;

    private LineRenderer currentStroke;
    private List<Vector3> points = new List<Vector3>();

    void Update()
    {
        if (Mouse.current == null) return;

        Vector2 mousePos = Mouse.current.position.ReadValue();
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, 10f));

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            StartStroke(worldPos);
        }

        if (Mouse.current.leftButton.isPressed && currentStroke != null)
        {
            AddPoint(worldPos);
        }

        if (Mouse.current.leftButton.wasReleasedThisFrame)
        {
            EndStroke();
        }

        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            Clear();
        }
    }

    void StartStroke(Vector3 startPos)
    {
        currentStroke = Instantiate(strokePrefab, transform);

        currentStroke.startColor = brushColor;
        currentStroke.endColor = brushColor;

        currentStroke.startWidth = lineWidth;
        currentStroke.endWidth = lineWidth;

        points.Clear();
        AddPoint(startPos);
    }

    void AddPoint(Vector3 pos)
    {
        if (points.Count > 0 && Vector3.Distance(points[^1], pos) < 0.05f)
            return;

        points.Add(pos);

        currentStroke.positionCount = points.Count;
        currentStroke.SetPosition(points.Count - 1, pos);
    }

    void EndStroke()
    {
        currentStroke = null;
    }

    void Clear()
    {
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }

        points.Clear();
        currentStroke = null;
    }
}