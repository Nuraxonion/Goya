using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class GestureRecorder : MonoBehaviour
{
    public List<Vector2> currentPoints = new List<Vector2>();
    public bool isRecording;

    void Update()
    {
        if (!isRecording) return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            currentPoints.Clear();
        }

        if (Mouse.current.leftButton.isPressed)
        {
            Vector2 p = Mouse.current.position.ReadValue();

            currentPoints.Add(p);
        }
    }

    public void StartRecording()
    {
        currentPoints.Clear();
        isRecording = true;
    }

    public List<Vector2> StopRecording()
    {
        isRecording = false;
        return new List<Vector2>(currentPoints);
    }

    public List<Vector2> GetSnapshot()
    {
        return new List<Vector2>(currentPoints);
    }
}