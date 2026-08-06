using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;


[RequireComponent(typeof(LineRenderer))]
public class BrushStroke2 : MonoBehaviour
{
    public GestureManager gestureManager;
    public GameObject upgradePanel;


    [Header("Brush")]
    public float minDistance = 0.05f;


    [Header("Burn")]
    public float burnDelay = 0.03f;


    private LineRenderer line;
    private Camera cam;


    private List<Vector3> points = new List<Vector3>();


    private int burnIndex = 0;

    private bool drawing = false;

    private Coroutine burnCoroutine;



    void Awake()
    {
        line = GetComponent<LineRenderer>();

        cam = Camera.main;


        line.useWorldSpace = true;
        line.positionCount = 0;


        if (gestureManager == null)
        {
            gestureManager = FindObjectOfType<GestureManager>();
        }
    }





    void Update()
    {
        if (IsBlocked())
            return;



        if (Input.GetMouseButtonDown(0))
        {
            StartDrawing();
        }



        if (Input.GetMouseButton(0))
        {
            Draw();
        }



        if (Input.GetMouseButtonUp(0))
        {
            StopDrawing();
        }
    }





    void StartDrawing()
    {
        if (burnCoroutine != null)
        {
            StopCoroutine(burnCoroutine);
        }


        points.Clear();

        burnIndex = 0;


        line.positionCount = 0;


        drawing = true;


        Vector3 start = MousePosition();


        // первая точка фиксируется
        points.Add(start);


        line.positionCount = 1;

        line.SetPosition(0, start);



        if (gestureManager != null)
        {
            gestureManager.Clear();
        }
    }





    void Draw()
    {
        if (!drawing)
            return;


        Vector3 current = MousePosition();



        // кисть следует за курсором
        transform.position = current;



        Vector3 last = points[points.Count - 1];



        if (Vector3.Distance(last, current) > minDistance)
        {
            points.Add(current);


            line.positionCount = points.Count;


            line.SetPosition(
                points.Count - 1,
                current
            );


            if (gestureManager != null)
            {
                gestureManager.AddPoint(
                    Input.mousePosition
                );
            }
        }
    }





    void StopDrawing()
    {
        drawing = false;


        if (gestureManager != null)
        {
            gestureManager.Recognize();
        }


        burnCoroutine = StartCoroutine(Burn());
    }





    IEnumerator Burn()
    {
        while (burnIndex < points.Count)
        {
            burnIndex++;


            int remaining = points.Count - burnIndex;


            line.positionCount = remaining;


            for (int i = 0; i < remaining; i++)
            {
                line.SetPosition(
                    i,
                    points[i + burnIndex]
                );
            }


            yield return new WaitForSeconds(burnDelay);
        }


        line.positionCount = 0;
    }





    bool IsBlocked()
    {
        if (upgradePanel != null &&
           upgradePanel.activeSelf)
        {
            return true;
        }


        if (EventSystem.current != null &&
           EventSystem.current.IsPointerOverGameObject())
        {
            return true;
        }


        return false;
    }





    Vector3 MousePosition()
    {
        Vector3 pos = Input.mousePosition;


        pos.z = 10f;


        Vector3 world = cam.ScreenToWorldPoint(pos);


        world.z = 0f;


        return world;
    }
}