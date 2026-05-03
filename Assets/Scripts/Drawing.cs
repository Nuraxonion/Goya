using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class DrawingSystem : MonoBehaviour
{
    [Header("References")]
    public RectTransform drawingArea;   // UI panel area
    public Camera cam;
    public LineRenderer lineRenderer;

    [Header("Settings")]
    public float minDistance = 10f;     // Minimum distance between points
    public float lineWidth = 0.1f;
    public float clearDelay = 1.5f;

    private List<Vector3> worldPoints = new List<Vector3>();
    private List<int> visitedCells = new List<int>();

    private bool isDrawing = false;
    private Vector2 lastScreenPos;

    void Start()
    {
        if (lineRenderer == null)
            lineRenderer = GetComponent<LineRenderer>();

        lineRenderer.positionCount = 0;
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
    }

    void Update()
    {
        HandleInput();
    }

    void HandleInput()
    {
        if (Mouse.current == null) return;

        Vector2 mousePos = Mouse.current.position.ReadValue();

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (IsInsideDrawingArea(mousePos))
            {
                StartDrawing();
            }
        }

        if (Mouse.current.leftButton.isPressed && isDrawing)
        {
            if (IsInsideDrawingArea(mousePos))
            {
                ContinueDrawing(mousePos);
            }
        }

        if (Mouse.current.leftButton.wasReleasedThisFrame && isDrawing)
        {
            EndDrawing();
        }
    }

    void StartDrawing()
    {
        isDrawing = true;

        worldPoints.Clear();
        visitedCells.Clear();

        lineRenderer.positionCount = 0;

        lastScreenPos = Input.mousePosition;
        AddPoint(lastScreenPos);
    }

    void ContinueDrawing(Vector2 screenPos)
    {
        if (Vector2.Distance(screenPos, lastScreenPos) < minDistance)
            return;

        lastScreenPos = screenPos;
        AddPoint(screenPos);
    }

    void EndDrawing()
    {
        isDrawing = false;

        Debug.Log("Visited Cells: " + string.Join(" -> ", visitedCells));

        Invoke(nameof(ClearLine), clearDelay);
    }

    void AddPoint(Vector2 screenPos)
    {
        // Convert to world point
        Vector3 worldPoint = cam.ScreenToWorldPoint(
            new Vector3(screenPos.x, screenPos.y, 10f)
        );

        worldPoints.Add(worldPoint);

        lineRenderer.positionCount = worldPoints.Count;
        lineRenderer.SetPositions(worldPoints.ToArray());

        // Detect grid cell
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            drawingArea,
            screenPos,
            null,
            out localPoint
        );

        int cell = GetCell(localPoint);

        if (visitedCells.Count == 0 || visitedCells[visitedCells.Count - 1] != cell)
        {
            visitedCells.Add(cell);
        }
    }

    int GetCell(Vector2 localPos)
    {
        Rect rect = drawingArea.rect;

        float width = rect.width;
        float height = rect.height;

        // Shift from (-width/2 → width/2) to (0 → width)
        float x = localPos.x + width / 2f;
        float y = localPos.y + height / 2f;

        float cellWidth = width / 3f;
        float cellHeight = height / 3f;

        int col = Mathf.Clamp(Mathf.FloorToInt(x / cellWidth), 0, 2);
        int row = Mathf.Clamp(Mathf.FloorToInt(y / cellHeight), 0, 2);

        return row * 3 + col;
    }

    bool IsInsideDrawingArea(Vector2 screenPos)
    {
        return RectTransformUtility.RectangleContainsScreenPoint(
            drawingArea,
            screenPos,
            null
        );
    }

    void ClearLine()
    {
        lineRenderer.positionCount = 0;
        worldPoints.Clear();
    }
}