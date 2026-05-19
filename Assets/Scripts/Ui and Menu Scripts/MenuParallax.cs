using UnityEngine;

public class MenuParallax : MonoBehaviour
{
    public RectTransform farBackground;
    public RectTransform midBackground;
    public RectTransform foreground;

    public float farStrength = 5f;
    public float midStrength = 15f;
    public float foregroundStrength = 30f;

    void Update()
    {
        Vector2 mousePos = Input.mousePosition;

        float x = (mousePos.x / Screen.width) - 0.5f;
        float y = (mousePos.y / Screen.height) - 0.5f;

        farBackground.anchoredPosition =
            new Vector2(x * farStrength, y * farStrength);

        midBackground.anchoredPosition =
            new Vector2(x * midStrength, y * midStrength);

        foreground.anchoredPosition =
            new Vector2(x * foregroundStrength, y * foregroundStrength);
    }
}