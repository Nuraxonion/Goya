using PDollarGestureRecognizer;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class GestureManager : MonoBehaviour
{
    private readonly List<Vector2> points = new List<Vector2>();
    private readonly List<Gesture> trainingSet = new List<Gesture>();
    private readonly List<Point> gesturePointsReusable = new List<Point>();

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

    public enum AttackType { NoAttack, Circle, Bracket }
    public AttackType currentAttack = AttackType.NoAttack;

    void Awake()
    {
        mainCamera = Camera.main;
    }

    void Start()
    {
        ClearCanvas();

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
        if (Input.GetMouseButton(0))
        {
            Vector2 p = new Vector2(
                Input.mousePosition.x / Screen.width * canvasTexture.width,
                Input.mousePosition.y / Screen.height * canvasTexture.height
            );

            DrawStamp(p);
        }
    }
    private void DrawStamp(Vector2 pixelPos)
    {
        if (canvasTexture == null || brushPNG == null) return;

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
    }

    void Recognize()
    {
        currentAttack = AttackType.NoAttack;
        if (points.Count < 10) { points.Clear(); return; }

        gesturePointsReusable.Clear();
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
        points.Clear();
    }
}