using System.Collections;
using System.Collections.Generic;
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

    // A stroke already under way is never interrupted by UI: the release has to
    // reach Recognize(), or the whole gesture is silently thrown away.
    private bool isDrawing;

    private readonly List<RaycastResult> uiHits = new List<RaycastResult>();

    void Awake()
    {
        trail = GetComponent<TrailRenderer>();
        trail.emitting = false;

        cam = Camera.main;
    }

    void Update()
    {
        // A frozen game (level-up, pause, game over) cancels whatever is being
        // drawn - the stroke could not be acted on anyway.
        if (Time.timeScale == 0f)
        {
            CancelStroke();
            return;
        }

        // Everything else only decides whether a NEW stroke may start. Once the
        // player is drawing, the stroke runs to completion wherever the cursor goes.
        if (!isDrawing && IsInputBlocked())
        {
            CancelStroke();
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            isDrawing = true;

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

        // isDrawing gates sampling so a drag that began on a button (and was
        // therefore never started as a stroke) can't half-fill the point buffer.
        if (isDrawing && Input.GetMouseButton(0))
        {
            target = MousePos();

            gestureManager.AddPoint(Input.mousePosition);
        }

        transform.position = Vector3.MoveTowards(
            transform.position,
            target,
            smooth * 25f * Time.deltaTime
        );

        if (isDrawing && Input.GetMouseButtonUp(0))
        {
            isDrawing = false;

            gestureManager.Recognize();

            stopCoroutine = StartCoroutine(StopTrailAfterDelay());
        }
    }

    void CancelStroke()
    {
        isDrawing = false;

        if (trail.emitting)
        {
            trail.emitting = false;
            gestureManager.Clear();
        }
    }

    IEnumerator StopTrailAfterDelay()
    {
        // Realtime: a level-up freeze right after a stroke would otherwise park
        // this coroutine forever, leaving the trail emitting.
        yield return new WaitForSecondsRealtime(1f);

        trail.emitting = false;
        stopCoroutine = null;
    }

    bool IsInputBlocked()
    {
        if (upgradePanel != null && upgradePanel.activeSelf)
            return true;

        return IsPointerOverInteractiveUI();
    }

    // Only genuinely clickable UI should steal the cursor. The HUD is full of
    // decorative graphics with raycastTarget left on (health bar, duration bar,
    // coin text, ink drop); a blanket IsPointerOverGameObject() check let those
    // eat entire gestures.
    //
    // IPointerClickHandler is the right test rather than Selectable: Button and
    // Toggle implement it, while a Slider used as a progress bar does not - and
    // the health bar and attack-duration bar are exactly that. Real interactive
    // sliders only live in the pause/settings menus, which the timeScale check
    // above already covers.
    bool IsPointerOverInteractiveUI()
    {
        if (EventSystem.current == null)
            return false;

        PointerEventData pointer = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };

        uiHits.Clear();
        EventSystem.current.RaycastAll(pointer, uiHits);

        for (int i = 0; i < uiHits.Count; i++)
        {
            if (ExecuteEvents.GetEventHandler<IPointerClickHandler>(uiHits[i].gameObject) != null)
                return true;
        }

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