using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class GestureDrawingManager : MonoBehaviour
{
    public RectTransform drawArea;
    public Camera uiCamera;

    public LineRenderer linePrefab;
    public float minDistance = 2f;

    private LineRenderer currentLine;
    private List<Vector2> currentPoints = new();

    public List<Vector2> GetPoints() => new(currentPoints);

    void Update()
    {
        if (Mouse.current == null) return;

        Vector2 mousePos = Mouse.current.position.ReadValue();

        if (!RectTransformUtility.RectangleContainsScreenPoint(drawArea, mousePos, uiCamera))
            return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            StartStroke();
        }

        if (Mouse.current.leftButton.isPressed && currentLine != null)
        {
            AddPoint(mousePos);
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

    void StartStroke()
    {
        currentPoints.Clear();
        currentLine = Instantiate(linePrefab, transform);
    }

    void AddPoint(Vector2 screenPos)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            drawArea, screenPos, uiCamera, out Vector2 localPoint);

        if (currentPoints.Count > 0 &&
            Vector2.Distance(currentPoints[^1], localPoint) < minDistance)
            return;

        currentPoints.Add(localPoint);

        currentLine.positionCount = currentPoints.Count;
        currentLine.SetPosition(currentPoints.Count - 1, localPoint);
    }

    void EndStroke()
    {
        currentLine = null;
    }

    public void Clear()
    {
        foreach (Transform child in transform)
            Destroy(child.gameObject);

        currentPoints.Clear();
    }
}