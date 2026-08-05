using UnityEngine;

public class CursorManager : MonoBehaviour
{
    [Header("Cursor Textures")]
    public Texture2D normalCursor;
    public Texture2D durationCursor;

    [Header("Cursor Settings")]
    public Vector2 cursorHotspot = new Vector2(0, 0);
    //public Vector2 cursorHotspot = new Vector2(70, 300);

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
