using UnityEngine;

public class CreditsScroll : MonoBehaviour
{
    public float scrollSpeed = 20f;
    public float loopPoint = 1000f;

    private RectTransform creditsRect;
    private Vector2 startingPosition;

    void Start()
    {
        creditsRect = GetComponent<RectTransform>();
        startingPosition = creditsRect.anchoredPosition;
    }

    void OnEnable()
    {
        creditsRect.anchoredPosition = startingPosition;
    }

    void Update()
    {

        creditsRect.anchoredPosition += Vector2.up * scrollSpeed * Time.deltaTime;

        if (creditsRect.anchoredPosition.y > loopPoint)
        {
            creditsRect.anchoredPosition = startingPosition;
        }
    }
}