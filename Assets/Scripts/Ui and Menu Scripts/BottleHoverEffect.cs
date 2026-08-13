using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BottleHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Scale Settings")]
    public float hoverScaleMultiplier = 1.15f; // 15% bigger on hover
    public float animationSpeed = 10f;

    [Header("Glow Settings")]
    public Image glowImage;
    public float glowIntensity = 0.5f;
    public float glowAnimationSpeed = 5f;

    private Vector3 originalScale;
    private Vector3 targetScale;
    private bool isHovering = false;
    private Color originalGlowColor;
    private bool isEnabled = true;

    void Start()
    {
        originalScale = transform.localScale;
        targetScale = originalScale;

        if (glowImage != null)
        {
            originalGlowColor = glowImage.color;
            Color c = glowImage.color;
            c.a = 0f;
            glowImage.color = c;
        }
    }

    void Update()
    {
        if (!isEnabled) return;

        // Smoothly animate scale
        transform.localScale = Vector3.Lerp(
            transform.localScale,
            targetScale,
            animationSpeed * Time.unscaledDeltaTime
        );

        // Animate glow
        if (glowImage != null && isHovering)
        {
            float pulse = Mathf.Sin(Time.unscaledTime * glowAnimationSpeed) * 0.5f + 0.5f;
            float alpha = Mathf.Lerp(0.3f, glowIntensity, pulse);

            Color c = glowImage.color;
            c.a = alpha;
            glowImage.color = c;
        }
        else if (glowImage != null && !isHovering)
        {
            Color c = glowImage.color;
            c.a = Mathf.Lerp(c.a, 0f, animationSpeed * Time.unscaledDeltaTime);
            glowImage.color = c;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!isEnabled) return;
        isHovering = true;
        targetScale = originalScale * hoverScaleMultiplier;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!isEnabled) return;
        isHovering = false;
        targetScale = originalScale;
    }

    public void Enable()
    {
        isEnabled = true;
    }

    public void Disable()
    {
        isEnabled = false;
        isHovering = false;
        targetScale = originalScale;
        transform.localScale = originalScale;

        if (glowImage != null)
        {
            Color c = glowImage.color;
            c.a = 0f;
            glowImage.color = c;
        }
    }

    void OnEnable()
    {
        isEnabled = true;
    }

    void OnDisable()
    {
        Disable();
    }
}