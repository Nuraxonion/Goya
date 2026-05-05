using UnityEngine;
using static DrawingSystem;

public class Attack : MonoBehaviour
{
    private DrawingSystem drawingSystem;
    public AttackDirection attackDirection;

    void Start()
    {
        drawingSystem =
            GameObject.FindGameObjectWithTag("Finish")
            .GetComponent<DrawingSystem>();
    }

    void Update()
    {
        
        if (drawingSystem.currentAttackDirection != AttackDirection.None)
        {
            Debug.Log($"Hello {drawingSystem.currentAttackDirection}");
        } else
        {
            Debug.Log("Goodbye");
        } 
    }
}