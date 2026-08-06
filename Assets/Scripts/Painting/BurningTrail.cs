using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BurningTrail : MonoBehaviour
{
    public TrailRenderer trail;
    public LineRenderer line;

    public float burnSpeed = 0.05f;


    private List<Vector3> points = new List<Vector3>();


    public void StartBurn()
    {
        StartCoroutine(Burn());
    }


    IEnumerator Burn()
    {
        // Получаем точки из TrailRenderer
        Vector3[] positions = new Vector3[trail.positionCount];

        trail.GetPositions(positions);


        points.AddRange(positions);


        // выключаем оригинальный Trail
        trail.emitting = false;
        trail.Clear();


        // переносим в LineRenderer
        line.positionCount = points.Count;

        for (int i = 0; i < points.Count; i++)
        {
            line.SetPosition(i, points[i]);
        }



        // удаляем начало постепенно
        while (points.Count > 0)
        {
            points.RemoveAt(0);


            line.positionCount = points.Count;


            for (int i = 0; i < points.Count; i++)
            {
                line.SetPosition(i, points[i]);
            }


            yield return new WaitForSeconds(burnSpeed);
        }


        Destroy(gameObject);
    }
}