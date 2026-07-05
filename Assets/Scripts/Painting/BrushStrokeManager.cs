using UnityEngine;

[RequireComponent(typeof(TrailRenderer))]
public class BrushStrokeManager : MonoBehaviour
{
    public GestureManager gestureManager;
    public float smooth = 25f;

    private TrailRenderer trail;
    private Vector3 target;

    void Awake()
    {
        trail = GetComponent<TrailRenderer>();
        trail.emitting = false;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 p = MousePos();
            transform.position = p;
            target = p;

            trail.Clear();
            trail.emitting = true;

            gestureManager.Clear();
        }

        if (Input.GetMouseButton(0))
        {
            target = MousePos();

            gestureManager.AddPoint(Input.mousePosition);
        }

        transform.position = Vector3.Lerp(
            transform.position,
            target,
            smooth * Time.deltaTime
        );

        if (Input.GetMouseButtonUp(0))
        {
            trail.emitting = false;
            gestureManager.Recognize();
        }
    }

    Vector3 MousePos()
    {
        Vector3 p = Input.mousePosition;
        p.z = 10f;

        Vector3 w = Camera.main.ScreenToWorldPoint(p);
        w.z = 0;
        return w;
    }
}