using UnityEngine;

public class CursorSensitivity : MonoBehaviour
{
    void Start()
    {
        Cursor.visible = true;
    }

    void Update()
    {
        float sensitivity = MenuManager.mouseSensitivity;

        Vector3 mousePos = Input.mousePosition;

        mousePos.x *= sensitivity;
        mousePos.y *= sensitivity;
    }
}