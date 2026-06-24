using PDollarGestureRecognizer;
using System.Collections.Generic;
using UnityEngine;

public class GestureManager : MonoBehaviour
{
    private readonly List<Vector2> points = new List<Vector2>();
    private readonly List<Gesture> trainingSet = new List<Gesture>();
    private readonly List<Point> gesturePointsReusable = new List<Point>();
<<<<<<< Updated upstream

    [Header("UI Links")]
    public GameObject upgradePanel;

    [Header("Drawing Settings")]
    public RenderTexture canvasTexture;
    public Material brushMaterial;
    public Texture2D brushPNG;

    public float brushSize = 20f;
    public float baseSpacing = 2f;

    private Vector2 lastRenderPos;
    private Camera mainCamera;
    private bool isFirstFrameOfStroke;
=======
>>>>>>> Stashed changes

    public enum AttackType { NoAttack, Circle, Bracket }
    public AttackType currentAttack = AttackType.NoAttack;

<<<<<<< Updated upstream
    void Awake()
    {
        mainCamera = Camera.main;
    }

    void Start()
    {
        ClearCanvas();

=======
    private bool isDrawing = false;

    void Start()
    {
>>>>>>> Stashed changes
        trainingSet.Add(new Gesture(new Point[]
        {
            new Point(50, 0, 0), new Point(75, 10, 0), new Point(95, 35, 0),
            new Point(95, 65, 0), new Point(75, 90, 0), new Point(50, 100, 0),
            new Point(25, 90, 0), new Point(5, 65, 0), new Point(5, 35, 0),
            new Point(25, 10, 0), new Point(50, 0, 0)
        }, "circle"));
    }

    void Update()
    {
<<<<<<< Updated upstream
        if (Input.GetMouseButton(0))
        {
            Vector2 p = new Vector2(
                Input.mousePosition.x / Screen.width * canvasTexture.width,
                Input.mousePosition.y / Screen.height * canvasTexture.height
            );

            DrawStamp(p);
=======
        // 👉 НАЖАТИЕ (СТАРТ)
        if (Input.GetMouseButtonDown(0))
        {
            points.Clear();
            isDrawing = true;
        }

        // 👉 ДВИЖЕНИЕ (ЗАПИСЬ)
        if (isDrawing && Input.GetMouseButton(0))
        {
            Vector3 mPos = Input.mousePosition;

            Vector2 normalized = new Vector2(
                (mPos.x / Screen.width) * 100f,
                (mPos.y / Screen.height) * 100f
            );

            points.Add(normalized);
>>>>>>> Stashed changes
        }
    }
    private void DrawStamp(Vector2 pixelPos)
    {
        if (canvasTexture == null || brushPNG == null) return;

<<<<<<< Updated upstream
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = canvasTexture;

        GL.PushMatrix();
        GL.LoadPixelMatrix(0, canvasTexture.width, 0, canvasTexture.height);

        float size = brushSize;

        Rect rect = new Rect(
            pixelPos.x - size * 0.5f,
            pixelPos.y - size * 0.5f,
            size,
            size
        );

        Graphics.DrawTexture(rect, brushPNG, brushMaterial);

        GL.PopMatrix();
        RenderTexture.active = previous;
    }
    public void ClearCanvas()
    {
        if (canvasTexture == null) return;

        RenderTexture previousActive = RenderTexture.active;
        RenderTexture.active = canvasTexture;
        GL.Clear(true, true, new Color(0, 0, 0, 0));
        RenderTexture.active = previousActive;
=======
        // 👉 ОТПУСКАНИЕ (ЗАВЕРШЕНИЕ)
        if (Input.GetMouseButtonUp(0))
        {
            isDrawing = false;
            Recognize();
        }
>>>>>>> Stashed changes
    }

    void Recognize()
    {
        currentAttack = AttackType.NoAttack;
        if (points.Count < 10) { points.Clear(); return; }

<<<<<<< Updated upstream
        gesturePointsReusable.Clear();
=======
        if (points.Count < 10)
        {
            points.Clear();
            return;
        }

        gesturePointsReusable.Clear();

>>>>>>> Stashed changes
        for (int i = 0; i < points.Count; i++)
        {
            gesturePointsReusable.Add(new Point(points[i].x, points[i].y, 0));
        }

        Result result = PointCloudRecognizer.Classify(
            new Gesture(gesturePointsReusable.ToArray(), "input"),
            trainingSet.ToArray()
        );

        if (result.Score >= 0.75f)
        {
            if (result.GestureClass == "circle") currentAttack = AttackType.Circle;
        }
<<<<<<< Updated upstream
=======

        if (result.GestureClass == "circle")
            currentAttack = AttackType.Circle;

        Debug.Log($"Gesture: {result.GestureClass} | Score: {result.Score}");
        Debug.Log($"Attack: {currentAttack}");

>>>>>>> Stashed changes
        points.Clear();
    }
}