using UnityEngine;

public class CursorManager : MonoBehaviour
{
    [Header("Cursor Textures")]
    public Texture2D normalCursor;
    public Texture2D durationCursor;

    [Header("Cursor Settings")]
    public Vector2 cursorHotspot = new Vector2(-10, 80);

    private void Start()
    {
        ShowNormalCursor();
    }

    public void ShowNormalCursor()
    {
        Cursor.SetCursor(
            normalCursor,
            cursorHotspot,
            CursorMode.ForceSoftware
        );
    }

    public void ShowDurationCursor()
    {
        Cursor.SetCursor(
            durationCursor,
            cursorHotspot,
            CursorMode.ForceSoftware
        );
    }
}