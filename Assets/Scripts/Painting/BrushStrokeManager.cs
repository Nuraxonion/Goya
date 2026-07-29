using System.Collections;
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
    private Camera cam;

    private Vector3 target;

    private Coroutine stopCoroutine;

    void Awake()
    {
        trail = GetComponent<TrailRenderer>();
        trail.emitting = false;

        cam = Camera.main;
    }

    void Update()
    {
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
            if (stopCoroutine != null)
            {
                StopCoroutine(stopCoroutine);
                stopCoroutine = null;
            }

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

        transform.position = Vector3.MoveTowards(
            transform.position,
            target,
            smooth * 25f * Time.deltaTime
        );

        if (Input.GetMouseButtonUp(0))
        {
            gestureManager.Recognize();

            stopCoroutine = StartCoroutine(StopTrailAfterDelay());
        }
    }

    IEnumerator StopTrailAfterDelay()
    {
        yield return new WaitForSeconds(1f);

        trail.emitting = false;
        stopCoroutine = null;
    }

    bool IsInputBlocked()
    {
        if (upgradePanel != null && upgradePanel.activeSelf)
            return true;

        if (EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject())
            return true;

        return false;
    }

    Vector3 MousePos()
    {
        Vector3 p = Input.mousePosition;
        p.z = 10f;

        Vector3 w = cam.ScreenToWorldPoint(p);
        w.z = 0f;

        return w;
    }
}