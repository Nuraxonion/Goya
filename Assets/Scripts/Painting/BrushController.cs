using UnityEngine;

public class BrushController : MonoBehaviour
{
    private TrailRenderer tr;

    public float smoothSpeed = 15f;
    private Vector3 targetPos;

    void Awake()
    {
        tr = GetComponent<TrailRenderer>();
        tr.emitting = false;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            tr.emitting = true;
        }

        if (Input.GetMouseButtonUp(0))
        {
            tr.emitting = false;
            tr.Clear(); 
        }

        if (Input.GetMouseButton(0))
        {
            Vector3 mouse = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mouse.z = 0f;
            targetPos = mouse;
        }

        transform.position = Vector3.Lerp(
            transform.position,
            targetPos,
            Time.deltaTime * smoothSpeed
        );
    }
}