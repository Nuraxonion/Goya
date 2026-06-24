using UnityEngine;

public class BrushController : MonoBehaviour
{
    private TrailRenderer tr;
    private bool isDrawing = false;

    void Awake()
    {
        tr = GetComponent<TrailRenderer>();
        tr.emitting = false; // старт = не рисуем
    }

    void Update()
    {
        // 👉 НАЖАЛИ — включили кисть
        if (Input.GetMouseButtonDown(0))
        {
            isDrawing = true;
            tr.emitting = true;
        }

        // 👉 ДВИЖЕНИЕ КИСТИ ТОЛЬКО ПРИ ЗАЖАТОЙ КНОПКЕ
        if (isDrawing && Input.GetMouseButton(0))
        {
            Vector3 pos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            pos.z = 0f;

            transform.position = pos;
        }

        // 👉 ОТПУСТИЛИ — выключили кисть
        if (Input.GetMouseButtonUp(0))
        {
            isDrawing = false;
            tr.emitting = false;
        }
    }
}