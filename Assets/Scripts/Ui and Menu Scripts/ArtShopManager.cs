using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ArtShopManager : MonoBehaviour
{
    [Header("UI References")]
    public Scrollbar scrollbar;
    public RectTransform scrollContent;
    public Button backButton;

    [Header("Scene Names")]
    public string mainSceneName = "Title Screen and Main Menu";

    [Header("Scroll Settings")]
    public float scrollSpeed = 0.1f;

    private float maxScrollOffset;
    private bool isLoading = false;

    void Start()
    {
        if (backButton != null)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(GoBackToMainMenu);
        }

        if (scrollbar != null)
        {
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scrollbar.onValueChanged.RemoveAllListeners();
            scrollbar.onValueChanged.AddListener(OnScrollValueChanged);
            scrollbar.value = 1f;
        }

        CalculateMaxScrollOffset();
        OnScrollValueChanged(1f);
    }

    private void CalculateMaxScrollOffset()
    {
        if (scrollContent == null)
        {
            return;
        }

        RectTransform viewport = scrollContent.parent as RectTransform;

        if (viewport == null)
        {
            return;
        }

        float contentHeight = scrollContent.rect.height;
        float viewportHeight = viewport.rect.height;
        maxScrollOffset = Mathf.Max(0f, contentHeight - viewportHeight);
    }

    private void OnScrollValueChanged(float value)
    {
        if (scrollContent == null)
        {
            return;
        }

        float scrollOffset = (1f - value) * maxScrollOffset;
        Vector2 position = scrollContent.anchoredPosition;
        position.y = -scrollOffset;
        scrollContent.anchoredPosition = position;
    }

    public void GoBackToMainMenu()
    {
        if (isLoading)
        {
            return;
        }

        isLoading = true;
        SceneManager.LoadScene(mainSceneName);
    }

    void Update()
    {
        float mouseScroll = Input.GetAxis("Mouse ScrollWheel");

        if (mouseScroll != 0f && scrollbar != null)
        {
            float newValue = scrollbar.value - mouseScroll * scrollSpeed;
            scrollbar.value = Mathf.Clamp01(newValue);
        }
    }
}