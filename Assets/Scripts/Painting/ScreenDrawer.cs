using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class ScreenDrawer : MonoBehaviour
{
    [Header("Brush Settings")]
    [SerializeField] private float lineWidth = 0.1f;
    [SerializeField] private float lineOpacity = 1f;
    [SerializeField] private float lineLifetime = 2.5f;

    private Camera mainCamera;

    private LineRenderer currentLine;
    private List<Vector3> currentPoints = new List<Vector3>();

    private bool isDrawing;

    private void Awake()
    {
        mainCamera = Camera.main;
    }

    private void Update()
    {
        Mouse mouse = Mouse.current;

        if (mouse == null)
            return;

        // Start drawing
        if (mouse.leftButton.wasPressedThisFrame)
        {
            StartNewLine();
        }

        // Continue drawing
        if (mouse.leftButton.isPressed && currentLine != null)
        {
            Draw();
        }

        // Stop drawing
        if (mouse.leftButton.wasReleasedThisFrame)
        {
            currentLine = null;
            currentPoints.Clear();
        }
    }

    private void StartNewLine()
    {
        GameObject lineObject = new GameObject("BrushStroke");

        currentLine = lineObject.AddComponent<LineRenderer>();

        // Material
        Material mat = new Material(Shader.Find("Sprites/Default"));
        currentLine.material = mat;

        // Color
        Color lineColor = new Color(1f, 0f, 1f, lineOpacity); // Magenta
        currentLine.startColor = lineColor;
        currentLine.endColor = lineColor;

        // Width
        currentLine.startWidth = lineWidth;
        currentLine.endWidth = lineWidth;

        // Smoother lines
        currentLine.numCapVertices = 10;

        // Important settings
        currentLine.useWorldSpace = true;
        currentLine.positionCount = 0;

        // Sorting
        currentLine.sortingOrder = 100;

        // Destroy after lifetime
        StartCoroutine(FadeAndDestroy(lineObject, currentLine));
    }

    private void Draw()
    {
        Vector2 mousePos = Mouse.current.position.ReadValue();

        Vector3 worldPos = mainCamera.ScreenToWorldPoint(
            new Vector3(mousePos.x, mousePos.y, 10f)
        );

        // Prevent too many points
        if (currentPoints.Count > 0)
        {
            float distance = Vector3.Distance(
                currentPoints[currentPoints.Count - 1],
                worldPos
            );

            if (distance < 0.05f)
                return;
        }

        currentPoints.Add(worldPos);

        currentLine.positionCount = currentPoints.Count;
        currentLine.SetPositions(currentPoints.ToArray());
    }

    private IEnumerator FadeAndDestroy(GameObject lineObject, LineRenderer line)
    {
        yield return new WaitForSeconds(lineLifetime);

        float fadeDuration = 0.5f;
        float timer = 0f;

        Color startColor = line.startColor;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;

            float alpha = Mathf.Lerp(startColor.a, 0f, timer / fadeDuration);

            Color newColor = new Color(
                startColor.r,
                startColor.g,
                startColor.b,
                alpha
            );

            line.startColor = newColor;
            line.endColor = newColor;

            yield return null;
        }

        Destroy(lineObject);
    }
}