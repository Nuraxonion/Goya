using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MagicDrawingSystem : MonoBehaviour
{
    // ===== STORES DRAWN POINTS =====
    private List<Vector2> points = new List<Vector2>();

    // ===== VISUAL LINE =====
    private LineRenderer lineRenderer;

    // ===== BURN SETTINGS =====
    public float burnSpeed = 25f;
    private bool burning = false;
    private float burnTimer = 0f;

    void Start()
    {
        // Get LineRenderer component
        lineRenderer = GetComponent<LineRenderer>();

        // ===== MAKE LINE VISIBLE =====
        lineRenderer.useWorldSpace = true;
        lineRenderer.startWidth = 0.08f;
        lineRenderer.endWidth = 0.08f;
        lineRenderer.positionCount = 0;

        // Simple visible material
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));

        // Render above world objects
        lineRenderer.sortingOrder = 10;

        // ===== MAGIC COLOR GRADIENT =====
        Gradient gradient = new Gradient();

        gradient.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(Color.red, 0f),
                new GradientColorKey(Color.black, 1f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0f, 1f)
            }
        );

        lineRenderer.colorGradient = gradient;
    }

    void Update()
    {
        // ===== START DRAWING =====
        if (Input.GetMouseButtonDown(0))
        {
            StartDrawing();
        }

        // ===== DRAWING =====
        if (Input.GetMouseButton(0))
        {
            Draw();
        }

        // ===== STOP DRAWING =====
        if (Input.GetMouseButtonUp(0))
        {
            burning = true; // start "magic burn effect"
        }

        // ===== BURN EFFECT =====
        if (burning)
        {
            BurnFromStart();
        }
    }

    // =========================
    // START DRAWING
    // =========================
    void StartDrawing()
    {
        points.Clear();
        lineRenderer.positionCount = 0;

        burning = false;
        burnTimer = 0f;
    }

    // =========================
    // DRAW MOUSE PATH
    // =========================
    void Draw()
    {
        Vector2 normalizedPoint = new Vector2(
            (Input.mousePosition.x / Screen.width) * 100f,
            (Input.mousePosition.y / Screen.height) * 100f
        );

        // avoid too dense points
        if (points.Count == 0 || Vector2.Distance(points[^1], normalizedPoint) > 1f)
        {
            points.Add(normalizedPoint);

            Vector3 worldPos = Camera.main.ScreenToWorldPoint(
                new Vector3(Input.mousePosition.x, Input.mousePosition.y, 10f)
            );

            worldPos.z = 0f;

            lineRenderer.positionCount = points.Count;
            lineRenderer.SetPosition(points.Count - 1, worldPos);
        }
    }

    // =========================
    // BURN LINE FROM START
    // =========================
    void BurnFromStart()
    {
        burnTimer += Time.deltaTime;

        int removeCount = Mathf.FloorToInt(burnTimer * burnSpeed);

        if (removeCount <= 0 || points.Count == 0)
            return;

        removeCount = Mathf.Min(removeCount, points.Count);

        // remove first points (burn effect from start)
        points.RemoveRange(0, removeCount);

        burnTimer = 0f;

        // rebuild line
        lineRenderer.positionCount = points.Count;

        for (int i = 0; i < points.Count; i++)
        {
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(
                new Vector3(
                    points[i].x / 100f * Screen.width,
                    points[i].y / 100f * Screen.height,
                    10f
                )
            );

            worldPos.z = 0;
            lineRenderer.SetPosition(i, worldPos);
        }

        // stop when finished
        if (points.Count == 0)
        {
            burning = false;
            lineRenderer.positionCount = 0;
        }
    }
}