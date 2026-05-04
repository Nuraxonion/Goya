using UnityEngine;

public class Enemy : MonoBehaviour
{
    private DrawingSystem drawingSystem;

    void Start()
    {
        drawingSystem =
            GameObject.FindGameObjectWithTag("Finish")
            .GetComponent<DrawingSystem>();
    }

    void Update()
    {
        Debug.Log(drawingSystem.isStraightLine);
    }
}