using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class GestureDrawingManager : MonoBehaviour
{
    [Header("Bounds")]
    [Tooltip("UI rect the drawing is confined to. Mouse input outside is ignored.")]
    public RectTransform drawArea;
    [Tooltip("Camera the draw-area Canvas renders with (null = Screen Space - Overlay).")]
    public Camera uiCamera;

    [Header("Rendering")]
    [Tooltip("Camera used to place the world-space strokes (null = Camera.main).")]
    public Camera drawCamera;
    public LineRenderer linePrefab;
    [Tooltip("Distance from drawCamera at which strokes are placed in world space. " +
             "For a Screen Space - Camera canvas, keep this just below the canvas plane distance " +
             "so strokes render in front of the panel.")]
    public float drawDepth = 9f;
    public Color lineColor = Color.cyan;
    public float lineWidth = 0.1f;
    [Tooltip("Minimum spacing between captured points, in draw-area local units (pixels).")]
    public float minDistance = 5f;

    private LineRenderer currentLine;
    private List<Vector2> currentPoints = new();   // stored in draw-area LOCAL coords (resolution-independent)

    private Camera Cam => drawCamera != null ? drawCamera : Camera.main;

    public List<Vector2> GetPoints() => new(currentPoints);

    void Update()
    {
        if (Mouse.current == null) return;

        Vector2 mousePos = Mouse.current.position.ReadValue();
        bool inside = RectTransformUtility.RectangleContainsScreenPoint(drawArea, mousePos, uiCamera);

        if (Mouse.current.rightButton.wasPressedThisFrame)
            Clear();

        if (Mouse.current.leftButton.wasPressedThisFrame && inside)
            StartStroke();

        if (Mouse.current.leftButton.isPressed && currentLine != null && inside)
            AddPoint(mousePos);

        if (Mouse.current.leftButton.wasReleasedThisFrame)
            EndStroke();
    }

    void StartStroke()
    {
        currentPoints.Clear();
        currentLine = NewLine();
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
        currentLine.SetPosition(currentPoints.Count - 1, ScreenToWorld(screenPos));
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
        currentLine = null;
    }

    // Renders a previously-saved sample (local-rect points) back into the draw area
    // and makes it the active point set, so it can be re-saved or tested without redrawing.
    public void LoadPoints(List<Vector2> points)
    {
        Clear();
        if (points == null || points.Count == 0) return;

        currentPoints = new List<Vector2>(points);

        LineRenderer line = NewLine();
        line.positionCount = currentPoints.Count;
        for (int i = 0; i < currentPoints.Count; i++)
            line.SetPosition(i, LocalToWorld(currentPoints[i]));

        // displayed only; not the active drawing stroke
        currentLine = null;
    }

    private LineRenderer NewLine()
    {
        LineRenderer line = Instantiate(linePrefab, transform);
        line.useWorldSpace = true;
        line.startColor = lineColor;
        line.endColor = lineColor;
        line.startWidth = lineWidth;
        line.endWidth = lineWidth;
        line.positionCount = 0;
        return line;
    }

    // Screen point -> world position on the stroke plane in front of the draw camera.
    private Vector3 ScreenToWorld(Vector2 screenPos)
    {
        return Cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, drawDepth));
    }

    // Draw-area local point -> screen point -> world position (mirrors live capture so
    // loaded strokes land exactly where they were drawn).
    private Vector3 LocalToWorld(Vector2 localPoint)
    {
        Vector3 worldOnCanvas = drawArea.TransformPoint(localPoint);
        Vector2 screen = uiCamera != null
            ? (Vector2)uiCamera.WorldToScreenPoint(worldOnCanvas)
            : (Vector2)worldOnCanvas;            // overlay canvas: world coords == screen pixels
        return ScreenToWorld(screen);
    }
}
