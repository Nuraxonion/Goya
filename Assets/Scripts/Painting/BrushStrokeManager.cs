using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(TrailRenderer))]
public class BrushStrokeManager : MonoBehaviour
{
    public GestureManager gestureManager;
    [Tooltip("Drawing is suppressed while this panel is active (e.g. the upgrade screen).")]
    public GameObject upgradePanel;
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
        // Suppress drawing/recognition while a blocking panel is up or the pointer is
        // over a UI element, so menu clicks aren't captured as gestures. Abandon any
        // in-progress stroke so a partial drawing can't be recognized on release.
        if (IsInputBlocked())
        {
            if (trail.emitting)
            {
                trail.emitting = false;
                gestureManager.Clear();
            }
            return;
        }

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
            gestureManager.Recognize();
            StartCoroutine(StopTrailAfterDelay());
        }
    }
    private System.Collections.IEnumerator StopTrailAfterDelay()
    {
        yield return new WaitForSeconds(1f); // ждать 1 секунду
        trail.emitting = false;
    }

    // True when gameplay drawing should be ignored: a blocking panel is active, or
    // the pointer is hovering a UI raycast target.
    bool IsInputBlocked()
    {
        if (upgradePanel != null && upgradePanel.activeSelf)
            return true;

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return true;

        return false;
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
