using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BrushStrokeManager : MonoBehaviour
{
    [Header("Brush")]
    public GameObject brushStampPrefab;
    public float spacing = 0.05f;

    [Header("Burn")]
    public float burnDelay = 0.02f;


    private Camera cam;
    private Vector3 lastPosition;

    private List<BrushStamp> currentStroke = new List<BrushStamp>();

    private bool drawing;



    void Awake()
    {
        cam = Camera.main;
    }



    void Update()
    {
        Vector3 mousePosition = cam.ScreenToWorldPoint(Input.mousePosition);
        mousePosition.z = 0;



        if (Input.GetMouseButtonDown(0))
        {
            drawing = true;

            lastPosition = mousePosition;

            SpawnStamp(mousePosition);
        }



        if (Input.GetMouseButton(0) && drawing)
        {
            float distance = Vector3.Distance(
                lastPosition,
                mousePosition
            );


            if (distance >= spacing)
            {
                SpawnStamp(mousePosition);

                lastPosition = mousePosition;
            }
        }



        if (Input.GetMouseButtonUp(0))
        {
            drawing = false;

            StartCoroutine(Burn());
        }
    }



    void SpawnStamp(Vector3 position)
    {
        GameObject obj = Instantiate(
            brushStampPrefab,
            position,
            Quaternion.identity
        );


        BrushStamp stamp = obj.GetComponent<BrushStamp>();


        if (stamp != null)
        {
            currentStroke.Add(stamp);
        }
    }



    IEnumerator Burn()
    {
        List<BrushStamp> copy = new List<BrushStamp>(currentStroke);


        foreach (BrushStamp stamp in copy)
        {
            if (stamp != null)
            {
                stamp.StartFade();
            }


            // эффект распространения огня
            yield return new WaitForSeconds(burnDelay);
        }


        currentStroke.Clear();
    }
}