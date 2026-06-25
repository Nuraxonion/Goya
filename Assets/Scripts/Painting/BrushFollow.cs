using UnityEngine;

public class BrushFollow : MonoBehaviour
{
    void Update()
    {
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = 10f;

        transform.position =
            Camera.main.ScreenToWorldPoint(mousePos);
    }
}