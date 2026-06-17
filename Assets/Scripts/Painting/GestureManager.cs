using PDollarGestureRecognizer;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class GestureManager : MonoBehaviour
{
    private readonly List<Vector2> points = new List<Vector2>();
    private readonly List<Gesture> trainingSet = new List<Gesture>();
    private readonly List<Point> gesturePointsReusable = new List<Point>();

    // ИСПРАВЛЕНО: Изменено обратно на GameObject (или замени на точное имя твоего скрипта панели с большой буквы)
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
        // Очищаем холст при старте
        ClearCanvas();

        // Базовый жест круга
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
        if (upgradePanel != null && upgradePanel.activeSelf) return;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
        if (canvasTexture == null || brushMaterial == null || brushPNG == null) return; // Защита от пустых ссылок

        // Получаем позицию мыши на экране, переводим в пиксели RenderTexture
        Vector3 mPos = Input.mousePosition;
        Vector2 currentPixelPos = new Vector2(
            (mPos.x / Screen.width) * canvasTexture.width,
            (mPos.y / Screen.height) * canvasTexture.height
        );

        Vector3 worldPos = mainCamera.ScreenToWorldPoint(new Vector3(mPos.x, mPos.y, 10f));

        if (Input.GetMouseButtonDown(0))
        {
            points.Clear();
            points.Add(new Vector2(worldPos.x, worldPos.y));
            lastRenderPos = currentPixelPos;
            isFirstFrameOfStroke = true;
            DrawStamp(currentPixelPos);
        }

        if (Input.GetMouseButton(0))
        {
            float distance = Vector2.Distance(lastRenderPos, currentPixelPos);
            float dynamicSpacing = baseSpacing + distance * 0.05f;

            if (distance > dynamicSpacing || isFirstFrameOfStroke)
            {
                isFirstFrameOfStroke = false;
                points.Add(new Vector2(worldPos.x, worldPos.y));

                Vector2 dir = (currentPixelPos - lastRenderPos).normalized;

                for (float d = 0; d < distance; d += dynamicSpacing)
                {
                    Vector2 pos = lastRenderPos + dir * d;
                    DrawStamp(pos);
                }

                lastRenderPos = currentPixelPos;
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            Recognize();
        }
    }

    private void DrawStamp(Vector2 pixelPos)
    {
        // Сохраняем старый активный RenderTexture, чтобы ничего не сломать в Unity
        RenderTexture previousActive = RenderTexture.active;
        RenderTexture.active = canvasTexture;

        GL.PushMatrix();
        GL.LoadPixelMatrix(0, canvasTexture.width, 0, canvasTexture.height);

        // Рандомизация размера (в пределах 90%-110%)
        float currentSize = brushSize * Random.Range(0.9f, 1.1f);

        Rect rect = new Rect(pixelPos.x - currentSize / 2, pixelPos.y - currentSize / 2, currentSize, currentSize);

        // Отрисовка
        Graphics.DrawTexture(rect, brushPNG, brushMaterial);

        GL.PopMatrix();

        // Восстанавливаем старый RenderTexture обратно
        RenderTexture.active = previousActive;
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