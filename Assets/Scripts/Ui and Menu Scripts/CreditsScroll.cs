using UnityEngine;

public class CreditsScroll : MonoBehaviour
{
    public float scrollSpeed = 25f;
    public float loopPoint = 1000f;

    private RectTransform creditsRect;
    private Vector2 startingPosition;
    void Start()
    {
        creditsRect = GetComponent<RectTransform>();

        if (creditsRect != null)
        {
            startingPosition = creditsRect.anchoredPosition;
        }
        else
        {
            Debug.LogWarning("CreditsScroll: No RectTransform found on " + gameObject.name);
        }
    }
    void OnEnable()
    {
        if (creditsRect != null)
        {
            creditsRect.anchoredPosition = startingPosition;
        }
        else
        {
            Debug.LogWarning("CreditsScroll: RectTransform is null, can't reset position");
        }
    }
    void Update()
    {
        if (creditsRect == null)
        {
            return;
        }

        creditsRect.anchoredPosition += Vector2.up * scrollSpeed * Time.deltaTime;

        if (creditsRect.anchoredPosition.y > loopPoint)
        {
            creditsRect.anchoredPosition = startingPosition;
        }
    }
}