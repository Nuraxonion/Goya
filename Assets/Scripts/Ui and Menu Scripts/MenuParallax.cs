using UnityEngine;

public class MenuParallax : MonoBehaviour
{
    [Header("Background Layers")]
    public RectTransform farBackground;
    public RectTransform midBackground;
    public RectTransform foreground;

    [Header("Movement Strengths")]
    public float farStrength = 5f;
    public float midStrength = 15f;
    public float foregroundStrength = 30f;

    [Header("Responsive Settings")]
    public float responseSpeed = 8f; // How fast it follows mouse (higher = faster)
    public float maxOffsetX = 100f; // Maximum horizontal movement
    public float maxOffsetY = 50f; // Maximum vertical movement
    public float idleSwayAmount = 0.5f; // Subtle movement when mouse is still
    public float idleSwaySpeed = 0.3f; // Speed of idle sway

    private Vector2 farCurrentOffset;
    private Vector2 midCurrentOffset;
    private Vector2 foregroundCurrentOffset;

    private Vector2 farOriginalPosition;
    private Vector2 midOriginalPosition;
    private Vector2 foregroundOriginalPosition;

    private Vector2 targetOffset;

    void Start()
    {
        // Store original positions
        if (farBackground != null)
            farOriginalPosition = farBackground.anchoredPosition;
        if (midBackground != null)
            midOriginalPosition = midBackground.anchoredPosition;
        if (foreground != null)
            foregroundOriginalPosition = foreground.anchoredPosition;
    }

    void Update()
    {
        // Get mouse position normalized to -1 to 1
        Vector2 mousePos = Input.mousePosition;

        float x = (mousePos.x / Screen.width - 0.5f) * 2f;
        float y = (mousePos.y / Screen.height - 0.5f) * 2f;

        // Clamp values
        x = Mathf.Clamp(x, -1f, 1f);
        y = Mathf.Clamp(y, -1f, 1f);

        // Calculate target offsets with strength multipliers
        Vector2 targetFar = new Vector2(x * farStrength, y * farStrength);
        Vector2 targetMid = new Vector2(x * midStrength, y * midStrength);
        Vector2 targetForeground = new Vector2(x * foregroundStrength, y * foregroundStrength);

        // Add idle sway when mouse is near center
        if (Mathf.Abs(x) < 0.1f && Mathf.Abs(y) < 0.1f)
        {
            float swayX = Mathf.Sin(Time.unscaledTime * idleSwaySpeed) * idleSwayAmount;
            float swayY = Mathf.Cos(Time.unscaledTime * idleSwaySpeed * 0.7f) * idleSwayAmount;

            targetFar += new Vector2(swayX * 0.3f, swayY * 0.3f);
            targetMid += new Vector2(swayX * 0.6f, swayY * 0.6f);
            targetForeground += new Vector2(swayX * 1.0f, swayY * 1.0f);
        }

        // Clamp final targets to max offset
        targetFar = Vector2.ClampMagnitude(targetFar, maxOffsetX);
        targetMid = Vector2.ClampMagnitude(targetMid, maxOffsetX);
        targetForeground = Vector2.ClampMagnitude(targetForeground, maxOffsetX);

        // Smoothly interpolate current offsets toward target offsets
        float deltaTime = Time.unscaledDeltaTime;
        if (deltaTime > 0.1f) deltaTime = 0.1f; // Cap delta time for smoothness

        farCurrentOffset = Vector2.Lerp(farCurrentOffset, targetFar, responseSpeed * deltaTime);
        midCurrentOffset = Vector2.Lerp(midCurrentOffset, targetMid, responseSpeed * deltaTime);
        foregroundCurrentOffset = Vector2.Lerp(foregroundCurrentOffset, targetForeground, responseSpeed * deltaTime);

        // Apply offsets with slight easing
        if (farBackground != null)
        {
            farBackground.anchoredPosition = farOriginalPosition + farCurrentOffset;
        }

        if (midBackground != null)
        {
            midBackground.anchoredPosition = midOriginalPosition + midCurrentOffset;
        }

        if (foreground != null)
        {
            foreground.anchoredPosition = foregroundOriginalPosition + foregroundCurrentOffset;
        }
    }

    // Call this to reset all positions (useful when switching scenes)
    public void ResetPositions()
    {
        farCurrentOffset = Vector2.zero;
        midCurrentOffset = Vector2.zero;
        foregroundCurrentOffset = Vector2.zero;

        if (farBackground != null)
            farBackground.anchoredPosition = farOriginalPosition;
        if (midBackground != null)
            midBackground.anchoredPosition = midOriginalPosition;
        if (foreground != null)
            foreground.anchoredPosition = foregroundOriginalPosition;
    }

    // Call this to manually set a target position (for animations)
    public void SetTargetOffset(Vector2 target)
    {
        targetOffset = target;
    }

    // Call this to instantly snap to a position (no smoothing)
    public void SnapToPosition(Vector2 position)
    {
        farCurrentOffset = position * farStrength;
        midCurrentOffset = position * midStrength;
        foregroundCurrentOffset = position * foregroundStrength;

        if (farBackground != null)
            farBackground.anchoredPosition = farOriginalPosition + farCurrentOffset;
        if (midBackground != null)
            midBackground.anchoredPosition = midOriginalPosition + midCurrentOffset;
        if (foreground != null)
            foreground.anchoredPosition = foregroundOriginalPosition + foregroundCurrentOffset;
    }
}