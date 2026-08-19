using UnityEngine;
using UnityEngine.UI;

public class CursorController : MonoBehaviour
{
    [Header("Cursor")]
    public RectTransform cursor;
    public Vector2 cursorOffset;

    [Header("Duration")]
    public GameObject durationOutline;
    public Image durationFill;

    private void Awake()
    {
        Cursor.visible = false;
        HideDuration();
    }

    private void Update()
    {
        cursor.position = Input.mousePosition + new Vector3(cursorOffset.x, cursorOffset.y, 0f);
    }

    public void StartDuration()
    {
        durationOutline.SetActive(true);
        durationFill.gameObject.SetActive(true);

        durationFill.fillAmount = 1f;
    }

    public void SetDuration(float normalizedValue)
    {
        durationFill.fillAmount = Mathf.Clamp01(normalizedValue);
    }

    public void EndDuration()
    {
        durationFill.fillAmount = 0f;

        durationOutline.SetActive(false);
        durationFill.gameObject.SetActive(false);
    }

    public void HideDuration()
    {
        durationOutline.SetActive(false);
        durationFill.gameObject.SetActive(false);
    }
}