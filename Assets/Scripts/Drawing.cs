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

    //Experiment variable
    public bool isStraightLine = false;

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

    static readonly int[][] lines =
    {
        // Horizontal
        new[] {0,1,2}, new[] {3,4,5}, new[] {6,7,8},

        // Vertical
        new[] {0,3,6}, new[] {1,4,7}, new[] {2,5,8},

        // Diagonal
        new[] {0,4,8}, new[] {2,4,6}
    };

    int[] Reverse(int[] arr)
    {
        return new int[] { arr[2], arr[1], arr[0] };
    }

    bool ContainsSequence(List<int> drawn, int[] pattern)
    {
        int index = 0;

        for (int i = 0; i < drawn.Count; i++)
        {
            if (drawn[i] == pattern[index])
            {
                index++;
                if (index == pattern.Length)
                    return true;
            }
        }

        return false;
    }

    bool IsPathValidForLine(List<int> drawn, int[] line)
    {
        HashSet<int> allowed = new HashSet<int>();

        bool isDiagonal = (line[1] == 4);

        if (!isDiagonal)
        {
            // Horizontal / Vertical → strict
            allowed = new HashSet<int>(line);
        }
        else
        {
            // Diagonal → allow neighbors (soft diagonal band)

            if (line[0] == 0) // 0-4-8 diagonal
            {
                allowed = new HashSet<int> { 0, 1, 3, 4, 5, 7, 8 };
            }
            else // 2-4-6 diagonal
            {
                allowed = new HashSet<int> { 1, 2, 3, 4, 5, 6, 7 };
            }
        }

        foreach (int c in drawn)
        {
            if (!allowed.Contains(c))
                return false;
        }

        return true;
    }

    bool MatchesLine(List<int> drawn, int[] line)
    {
        bool isDiagonal = (line[1] == 4);

        // 1. Sequence check
        bool forward = ContainsSequence(drawn, line);
        bool backward = ContainsSequence(drawn, Reverse(line));

        if (isDiagonal)
        {
            int start = line[0];
            int mid = line[1];
            int end = line[2];

            bool diagForward = ContainsSequence(drawn, new int[] { start, mid, end });
            bool diagBackward = ContainsSequence(drawn, new int[] { end, mid, start });

            if (!diagForward && !diagBackward)
                return false;
        }
        else
        {
            if (!forward && !backward)
                return false;
        }

        // 2. Stay inside allowed cells
        if (!IsPathValidForLine(drawn, line))
            return false;

        // 3. Length rules
        if (isDiagonal)
        {
            if (drawn.Count > 5) return false;
        }
        else
        {
            if (drawn.Count != 3) return false;
        }

        return true;
    }

    bool IsValidLine(List<int> drawn)
    {
        if (drawn.Count < 3)
            return false;

        foreach (var line in lines)
        {
            if (MatchesLine(drawn, line))
                return true;
        }

        return false;
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

        lastScreenPos = Mouse.current.position.ReadValue();
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

        bool success = IsValidLine(visitedCells);

        isStraightLine = success ? true : false;

        //Debug.Log(success ? "✅ Valid line" : "❌ Invalid input");

        Invoke(nameof(ClearLine), clearDelay);

        Debug.Log(string.Join(" → ", visitedCells));
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