using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(TrailRenderer))]
public class BrushStrokeManager : MonoBehaviour
{
    public GestureManager gestureManager;

    [Tooltip("Drawing is suppressed while this panel is active.")]
    public GameObject upgradePanel;

    public float smooth = 25f;

    [Header("Burn effect")]
    public float fadeDuration = 2f;

    private TrailRenderer trail;
    private Camera cam;

    private Vector3 target;

    private Coroutine burnCoroutine;

    // Reused so the per-frame UI check doesn't allocate.
    private static readonly List<RaycastResult> uiHits = new List<RaycastResult>();


    void Awake()
    {
        trail = GetComponent<TrailRenderer>();

        trail.emitting = false;

        cam = Camera.main;


        // Автоматически ищем GestureManager
        if (gestureManager == null)
        {
            gestureManager = FindObjectOfType<GestureManager>();
        }


        if (gestureManager == null)
        {
            Debug.LogError("GestureManager не найден!");
        }
    }


    void Update()
    {
        if (IsInputBlocked())
        {
            if (trail.emitting)
            {
                trail.emitting = false;

                if (gestureManager != null)
                    gestureManager.Clear();
            }

            return;
        }



        // НАЧАЛО РИСОВАНИЯ
        if (Input.GetMouseButtonDown(0))
        {
            StopBurn();


            Vector3 p = MousePos();

            transform.position = p;
            target = p;


            trail.Clear();

            // Важно:
            // пока рисуем линия не исчезает
            trail.time = 999f;

            trail.emitting = true;


            if (gestureManager != null)
                gestureManager.Clear();
        }



        // РИСОВАНИЕ
        if (Input.GetMouseButton(0))
        {
            target = MousePos();


            if (gestureManager != null)
            {
                gestureManager.AddPoint(Input.mousePosition);
            }
        }



        // Движение кисти
        transform.position = Vector3.MoveTowards(
            transform.position,
            target,
            smooth * 25f * Time.deltaTime
        );



        // ОТПУСКАНИЕ ЛКМ
        if (Input.GetMouseButtonUp(0))
        {
            if (gestureManager != null)
            {
                gestureManager.Recognize();
            }


            trail.emitting = false;


            burnCoroutine = StartCoroutine(BurnTrail());
        }
    }



    IEnumerator BurnTrail()
    {
        float timer = 0f;

        float startTime = trail.time;


        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;


            // линия сгорает от начала к концу
            trail.time = Mathf.Lerp(
                startTime,
                0f,
                timer / fadeDuration
            );


            yield return null;
        }


        trail.Clear();

        // возвращаем для следующего рисования
        trail.time = 999f;
    }



    void StopBurn()
    {
        if (burnCoroutine != null)
        {
            StopCoroutine(burnCoroutine);
            burnCoroutine = null;
        }


        trail.time = 999f;
    }




    bool IsInputBlocked()
    {
        if (upgradePanel != null && upgradePanel.activeSelf)
            return true;


        return IsPointerOverInteractiveUI();
    }


    // Only genuinely clickable UI stops a stroke. The old check used
    // IsPointerOverGameObject(), which blocks on ANY raycast-target graphic - so
    // the Attack Duration bar, shown exactly while an attack is running, carved a
    // dead zone through the middle of the drawing area and wiped every gesture
    // point collected so far whenever a stroke crossed it.
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
            Selectable selectable = uiHits[i].gameObject.GetComponentInParent<Selectable>();

            if (selectable != null && selectable.interactable)
                return true;
        }


        return false;
    }




    Vector3 MousePos()
    {
        Vector3 p = Input.mousePosition;

        p.z = 10f;


        Vector3 world = cam.ScreenToWorldPoint(p);

        world.z = 0f;


        return world;
    }
}