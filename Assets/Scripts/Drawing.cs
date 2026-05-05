using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class DrawingSystem : MonoBehaviour
{
    // =========================
    // ENUMS
    // =========================
    public enum AttackDirection
    {
        None,
        North,
        Northeast,
        East,
        Southeast,
        South,
        Southwest,
        West,
        Northwest,
        BracketLeft,
        BracketRight,
        Circle
    }

    // =========================
    // INSPECTOR FIELDS
    // =========================
    [Header("References")]
    public RectTransform drawingArea;
    public Camera cam;
    public LineRenderer lineRenderer;

    [Header("Settings")]
    public float minDistance = 10f;
    public float lineWidth = 0.1f;
    public float clearDelay = 1.5f;

    // =========================
    // STATE
    // =========================
    private List<Vector3> worldPoints = new List<Vector3>();
    private List<int> visitedCells = new List<int>();

    private bool isDrawing = false;
    private Vector2 lastScreenPos;

    public bool isStraightLine = false;
    public AttackDirection currentAttackDirection = AttackDirection.None;

    // =========================
    // PATTERNS
    // =========================
    static readonly int[][] lines =
    {
        // Horizontal
        new[] {0,1,2}, new[] {3,4,5}, new[] {6,7,8},

        // Vertical
        new[] {0,3,6}, new[] {1,4,7}, new[] {2,5,8},

        // Diagonal
        new[] {0,4,8}, new[] {2,4,6}
    };

    static readonly int[][] uShapes =
    {
        // ] shape
        new[] {0,1,2,5,8,7,6},
        new[] {6,7,8,5,2,1,0},

        // [ shape
        new[] {2,1,0,3,6,7,8},
        new[] {8,7,6,3,0,1,2}
    };

    static readonly int[][] circleShapes =
    {
        new[] {0,1,2,5,8,7,6,3,0}, // clockwise
        new[] {0,3,6,7,8,5,2,1,0}  // counter-clockwise
    };

    // =========================
    // UNITY LIFECYCLE
    // =========================
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

    // =========================
    // INPUT
    // =========================
    void HandleInput()
    {
        if (Mouse.current == null) return;

        Vector2 mousePos = Mouse.current.position.ReadValue();

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (IsInsideDrawingArea(mousePos))
                StartDrawing();
        }

        if (Mouse.current.leftButton.isPressed && isDrawing)
        {
            if (IsInsideDrawingArea(mousePos))
                ContinueDrawing(mousePos);
        }

        if (Mouse.current.leftButton.wasReleasedThisFrame && isDrawing)
        {
            EndDrawing();
        }
    }

    // =========================
    // DRAWING LOGIC
    // =========================
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

        bool success = IsValidInput(visitedCells);
        isStraightLine = success;

        if (success)
        {
            currentAttackDirection = GetAttackDirection(visitedCells);
        }
        else
        {
            currentAttackDirection = AttackDirection.None;
        }

        // ✅ DEBUG OUTPUT
        //Debug.Log($"Attack Direction: {currentAttackDirection}");

        // Optional: also print raw cells
        Debug.Log($"Cells: {string.Join(" → ", visitedCells)}");

        Invoke(nameof(ClearLine), clearDelay);
    }

    void AddPoint(Vector2 screenPos)
    {
        Vector3 worldPoint = cam.ScreenToWorldPoint(
            new Vector3(screenPos.x, screenPos.y, 10f)
        );

        worldPoints.Add(worldPoint);

        lineRenderer.positionCount = worldPoints.Count;
        lineRenderer.SetPositions(worldPoints.ToArray());

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

    void ClearLine()
    {
        lineRenderer.positionCount = 0;
        worldPoints.Clear();
    }

    // =========================
    // DETECTION CORE
    // =========================
    bool IsValidInput(List<int> drawn)
    {
        if (drawn.Count < 3)
            return false;

        foreach (var line in lines)
        {
            if (MatchesLine(drawn, line))
                return true;
        }

        if (IsValidUShape(drawn))
            return true;

        if (IsValidCircle(drawn))
            return true;

        return false;
    }

    bool IsValidCircle(List<int> drawn)
    {
        foreach (var shape in circleShapes)
        {
            if (MatchesCircle(drawn, shape))
                return true;
        }

        return false;
    }

    AttackDirection GetAttackDirection(List<int> drawn)
    {
        if (drawn.Count < 2)
            return AttackDirection.None;

        int start = drawn[0];
        int end = drawn[drawn.Count - 1];

        // --- CIRCLE ---
        foreach (var shape in circleShapes)
        {
            if (MatchesCircle(drawn, shape))
                return AttackDirection.Circle;
        }

        // U shapes
        foreach (var shape in uShapes)
        {
            if (MatchesUShape(drawn, shape))
            {
                if (shape[0] == 0 || shape[0] == 6)
                    return AttackDirection.BracketRight;

                if (shape[0] == 2 || shape[0] == 8)
                    return AttackDirection.BracketLeft;
            }
        }

        // Lines
        foreach (var line in lines)
        {
            if (MatchesLine(drawn, line))
            {
                Vector2Int startPos = new Vector2Int(start % 3, start / 3);
                Vector2Int endPos = new Vector2Int(end % 3, end / 3);

                Vector2Int dir = endPos - startPos;

                dir.x = Mathf.Clamp(dir.x, -1, 1);
                dir.y = Mathf.Clamp(dir.y, -1, 1);

                if (dir == new Vector2Int(0, 1)) return AttackDirection.North;
                if (dir == new Vector2Int(1, 1)) return AttackDirection.Northeast;
                if (dir == new Vector2Int(1, 0)) return AttackDirection.East;
                if (dir == new Vector2Int(1, -1)) return AttackDirection.Southeast;
                if (dir == new Vector2Int(0, -1)) return AttackDirection.South;
                if (dir == new Vector2Int(-1, -1)) return AttackDirection.Southwest;
                if (dir == new Vector2Int(-1, 0)) return AttackDirection.West;
                if (dir == new Vector2Int(-1, 1)) return AttackDirection.Northwest;
            }
        }

        return AttackDirection.None;
    }

    // =========================
    // MATCHING LOGIC
    // =========================
    bool MatchesLine(List<int> drawn, int[] line)
    {
        bool isDiagonal = (line[1] == 4);

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

        if (!IsPathValidForLine(drawn, line))
            return false;

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

    bool MatchesUShape(List<int> drawn, int[] pattern)
    {
        if (!ContainsSequence(drawn, pattern))
            return false;

        HashSet<int> allowed = new HashSet<int>(pattern);

        foreach (int c in drawn)
        {
            if (!allowed.Contains(c))
                return false;
        }

        if (drawn.Count > pattern.Length + 2)
            return false;

        return true;
    }

    bool IsValidUShape(List<int> drawn)
    {
        foreach (var shape in uShapes)
        {
            if (MatchesUShape(drawn, shape))
                return true;
        }

        return false;
    }

    bool MatchesCircle(List<int> drawn, int[] pattern)
    {
        // must use at least most of the ring
        HashSet<int> required = new HashSet<int> { 0, 1, 2, 3, 5, 6, 7, 8 };

        foreach (int c in drawn)
        {
            if (!required.Contains(c))
                return false;
        }

        // must have enough points to form a loop
        if (drawn.Count < 7)
            return false;

        if (drawn.Count > 11)
            return false;

        // check that movement is circular (not random backtracking)
        int changes = 0;

        for (int i = 1; i < drawn.Count; i++)
        {
            if (drawn[i] != drawn[i - 1])
                changes++;
        }

        return changes >= 6; // ensures it actually goes around
    }

    // =========================
    // HELPERS
    // =========================
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

    int[] Reverse(int[] arr)
    {
        return new int[] { arr[2], arr[1], arr[0] };
    }

    bool IsPathValidForLine(List<int> drawn, int[] line)
    {
        HashSet<int> allowed;

        bool isDiagonal = (line[1] == 4);

        if (!isDiagonal)
        {
            allowed = new HashSet<int>(line);
        }
        else
        {
            if (line[0] == 0)
                allowed = new HashSet<int> { 0, 1, 3, 4, 5, 7, 8 };
            else
                allowed = new HashSet<int> { 1, 2, 3, 4, 5, 6, 7 };
        }

        foreach (int c in drawn)
        {
            if (!allowed.Contains(c))
                return false;
        }

        return true;
    }

    int GetCell(Vector2 localPos)
    {
        Rect rect = drawingArea.rect;

        float width = rect.width;
        float height = rect.height;

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

    bool ContainsCircularSequence(List<int> drawn, int[] pattern)
    {
        int n = pattern.Length;

        // Try every possible rotation of the pattern
        for (int start = 0; start < n; start++)
        {
            int index = 0;

            for (int i = 0; i < drawn.Count; i++)
            {
                int expected = pattern[(start + index) % n];

                if (drawn[i] == expected)
                {
                    index++;
                    if (index == n)
                        return true;
                }
            }
        }

        return false;
    }
}