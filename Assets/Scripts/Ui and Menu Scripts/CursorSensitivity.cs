using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class CursorSensitivity : MonoBehaviour
{
    public RectTransform cursorTransform;

    private Vector2 virtualMousePosition;

    private GameObject currentObject;

    private PointerEventData pointerData;

    void Start()
    {
        Cursor.visible = false;

        virtualMousePosition = Input.mousePosition;

        cursorTransform.position = virtualMousePosition;
    }

    void Update()
    {
        float sensitivity = MenuManager.mouseSensitivity;

        float mouseX = Input.GetAxis("Mouse X") * sensitivity * 25f;
        float mouseY = Input.GetAxis("Mouse Y") * sensitivity * 25f;

        virtualMousePosition += new Vector2(mouseX, mouseY);

        virtualMousePosition.x = Mathf.Clamp(virtualMousePosition.x, 0, Screen.width);
        virtualMousePosition.y = Mathf.Clamp(virtualMousePosition.y, 0, Screen.height);

        cursorTransform.position = virtualMousePosition;

        HandleUI();
    }

    void HandleUI()
    {
        pointerData = new PointerEventData(EventSystem.current);

        pointerData.position = virtualMousePosition;

        List<RaycastResult> results = new List<RaycastResult>();

        EventSystem.current.RaycastAll(pointerData, results);

        if (results.Count > 0)
        {
            currentObject = results[0].gameObject;

            if (Input.GetMouseButtonDown(0))
            {
                ExecuteEvents.Execute(
                    currentObject,
                    pointerData,
                    ExecuteEvents.pointerDownHandler
                );
            }

            if (Input.GetMouseButtonUp(0))
            {
                ExecuteEvents.Execute(
                    currentObject,
                    pointerData,
                    ExecuteEvents.pointerUpHandler
                );

                ExecuteEvents.Execute(
                    currentObject,
                    pointerData,
                    ExecuteEvents.pointerClickHandler
                );
            }

            if (Input.GetMouseButton(0))
            {
                ExecuteEvents.Execute(
                    currentObject,
                    pointerData,
                    ExecuteEvents.beginDragHandler
                );

                ExecuteEvents.Execute(
                    currentObject,
                    pointerData,
                    ExecuteEvents.dragHandler
                );
            }
        }
    }
}