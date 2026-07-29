using System.Collections;
using TMPro;
using UnityEngine;

public class GestureRankCursorUI : MonoBehaviour
{
    [Header("UI Reference")]
    public TextMeshProUGUI rankText;


[Header("Cursor Position")]
    [Tooltip("Moves the text relative to the cursor.")]
    public Vector2 cursorOffset = new Vector2(0f, -45f);

    [Header("Display")]
    [Min(0.1f)]
    public float displayTime = 1f;

    [Header("Rank Colors")]
    public Color normalColor = Color.blue;

    public Color epicColor = new Color(
        0.65f,
        0.2f,
        1f,
        1f
    );

    public Color legendaryColor = Color.yellow;

    private Coroutine hideCoroutine;

    private void Awake()
    {
        if (rankText != null)
        {
            // Keeps the middle of the text aligned with the cursor.
            rankText.alignment = TextAlignmentOptions.Center;
        }
    }

    private void Update()
    {
        if (
            rankText != null &&
            rankText.gameObject.activeSelf
        )
        {
            rankText.transform.position =
                Input.mousePosition +
                (Vector3)cursorOffset;
        }
    }

    public void ShowRank(string rank)
    {
        if (rankText == null)
        {
            Debug.LogWarning(
                "GestureRankCursorUI: Rank Text is not assigned."
            );

            return;
        }

        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
        }

        rankText.text = rank;

        // Select the color based on the rank.
        switch (rank.ToLower())
        {
            case "normal":
                rankText.color = normalColor;
                break;

            case "epic":
                rankText.color = epicColor;
                break;

            case "legendary":
                rankText.color = legendaryColor;
                break;

            default:
                rankText.color = Color.white;
                break;
        }

        // Put the text at the cursor immediately.
        rankText.transform.position =
            Input.mousePosition +
            (Vector3)cursorOffset;

        rankText.gameObject.SetActive(true);

        hideCoroutine =
            StartCoroutine(HideAfterTime());
    }

    private IEnumerator HideAfterTime()
    {
        yield return new WaitForSecondsRealtime(
            displayTime
        );

        rankText.gameObject.SetActive(false);
    }


}
