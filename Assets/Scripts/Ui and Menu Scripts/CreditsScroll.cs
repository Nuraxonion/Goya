using UnityEngine;

public class CreditsScroll : MonoBehaviour
{
    public float scrollSpeed = 20f;

    RectTransform rect;

    void Start()
    {
        rect = GetComponent<RectTransform>();
    }

    void Update()
    {
        rect.anchoredPosition += Vector2.up * scrollSpeed * Time.deltaTime;
    }
}