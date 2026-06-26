using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class InputManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera sceneCamera;

    [Header("Tap / Hold Settings")]
    [SerializeField] private float tapThreshold = 0.3f;
    [SerializeField] private float holdThreshold = 1f;
    [SerializeField] private float positionTolerance = 0.1f;

    public event Action OnMouseUp;
    public event Action OnMouseClick;
    public event Action OnMouseTapped;
    public event Action OnMouseHold;
    public event Action<float> OnMouseScroll;

    private float mouseDownTime;
    private Vector3 initialClickPosition;

    private void Update()
    {
        HandleMouseDown();
        HandleMouseScroll();
        HandleMouseHold();
        HandleMouseUp();
    }

    private void HandleMouseDown()
    {
        if (!Input.GetMouseButtonDown(0) || IsPointerOverUI())
        {
            return;
        }

        mouseDownTime = Time.time;
        initialClickPosition = GetSelectedMapPosition();
        OnMouseClick?.Invoke();
    }

    private void HandleMouseScroll()
    {
        float scrollDelta = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scrollDelta) > 0f)
        {
            OnMouseScroll?.Invoke(scrollDelta);
        }
    }

    private void HandleMouseHold()
    {
        if (!Input.GetMouseButton(0))
        {
            return;
        }

        float heldTime = Time.time - mouseDownTime;
        if (heldTime < holdThreshold)
        {
            return;
        }

        Vector3 currentMousePosition = GetSelectedMapPosition();
        if (PositionsAreClose(initialClickPosition, currentMousePosition))
        {
            OnMouseHold?.Invoke();
        }
    }

    private void HandleMouseUp()
    {
        if (!Input.GetMouseButtonUp(0))
        {
            return;
        }

        OnMouseUp?.Invoke();

        float heldTime = Time.time - mouseDownTime;
        Vector3 mouseUpPosition = GetSelectedMapPosition();

        bool isTap = heldTime < tapThreshold &&
                     PositionsAreClose(initialClickPosition, mouseUpPosition);

        if (isTap)
        {
            OnMouseTapped?.Invoke();
        }
    }

    public bool IsPointerOverUI()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }

    public Vector3 GetSelectedMapPosition()
    {
        if (sceneCamera == null)
        {
            Debug.LogError("InputManager: Scene camera is not assigned.");
            return Vector3.zero;
        }

        Vector3 mousePosition = Input.mousePosition;
        mousePosition.z = -sceneCamera.transform.position.z;

        Vector3 worldPosition = sceneCamera.ScreenToWorldPoint(mousePosition);
        worldPosition.z = 0f;

        return worldPosition;
    }

    public Vector3 GetMousePosition()
    {
        return Input.mousePosition;
    }

    private bool PositionsAreClose(Vector3 firstPosition, Vector3 secondPosition)
    {
        return Vector3.Distance(firstPosition, secondPosition) <= positionTolerance;
    }
}