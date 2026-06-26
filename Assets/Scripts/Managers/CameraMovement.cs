using System;
using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    [SerializeField]
    private InputManager inputManager;
    [SerializeField]
    private BuildingRegistryManager buildingRegistryManager;

    // Drag and Movement
    [Header("Camera Panning")]
    public float panSpeed = 0.75f; // Base pan speed
    public float smoothSpeed = 5f; // Lerp smoothing factor
    public float inertiaFactor = 0.95f; // Momentum retention
    public float maxPanSpeed = 10f; // Maximum pan speed

    // Zoom Parameters
    [Header("Zoom Settings")]
    public float zoomSpeed = 5f;
    public float minZoom = 2f;
    public float maxZoom = 10f;

    // Camera Bounds
    private Vector2 minBounds = new Vector2(-32, 17); // Minimum x and y coordinates
    private Vector2 maxBounds = new Vector2(22, 59); // Maximum x and y coordinates

    // Internal Variables
    private Camera cam;
    private Vector3 lastMousePosition;
    private bool isDragging = false;
    private Vector3 velocity = Vector3.zero;
    private Vector3 targetPosition;

    // Smoothing Modes
    [Header("Movement Style")]
    public bool useLerpSmoothing = true; // Toggle between smoothing methods
    public bool useInertiaMovement = false;

    private void Start()
    {
        cam = Camera.main;
        inputManager.OnMouseScroll += HandleMouseScroll;
        inputManager.OnMouseClick += HandleMouseClick;
        inputManager.OnMouseUp += HandleMouseUp;
        buildingRegistryManager.buildingSelected += moveToBuilding;

        // Initialize target position to current position
        targetPosition = cam.transform.position;
    }

    private void moveToBuilding(Vector3 buildingPosition)
    {
        Vector3 worldPosition = new Vector3(
            buildingPosition.x,
            buildingPosition.y,
            cam.transform.position.z
        );

        targetPosition = ClampToBounds(worldPosition);
        velocity = Vector3.zero;
    }

    private void LateUpdate()
    {
        if (isDragging)
        {
            HandleCameraDrag();
        }

        // Apply smoothing techniques
        ApplyCameraSmoothing();
    }

    private void HandleMouseClick()
    {
        isDragging = true;
        lastMousePosition = inputManager.GetMousePosition();
    }

    private void HandleMouseUp()
    {
        isDragging = false;
    }

    private void HandleCameraDrag()
    {
        // Calculate the difference between the last and current mouse positions
        Vector3 currentMousePosition = inputManager.GetMousePosition();
        Vector3 difference = lastMousePosition - currentMousePosition;

        if (useInertiaMovement)
        {
            // Inertia-based movement
            velocity += new Vector3(difference.x, difference.y, 0) * panSpeed * Time.deltaTime;
            velocity = Vector3.ClampMagnitude(velocity, maxPanSpeed);
        }
        else
        {
            // Standard movement
            targetPosition += new Vector3(difference.x * panSpeed * Time.deltaTime,
                                          difference.y * panSpeed * Time.deltaTime,
                                          0);
        }

        // Update last mouse position
        lastMousePosition = currentMousePosition;
    }

    private void ApplyCameraSmoothing()
    {
        Vector3 finalPosition;

        if (useLerpSmoothing)
        {
            // Lerp-based smooth movement
            finalPosition = Vector3.Lerp(cam.transform.position, targetPosition, smoothSpeed * Time.deltaTime);
        }
        else if (useInertiaMovement)
        {
            // Apply inertia and damping
            velocity *= inertiaFactor;
            finalPosition = cam.transform.position + velocity * Time.deltaTime;
        }
        else
        {
            // Direct movement
            finalPosition = targetPosition;
        }

        // Clamp the final position
        finalPosition = ClampToBounds(finalPosition);

        // Apply the final position
        cam.transform.position = finalPosition;

        // Reset velocity if it becomes very small
        if (velocity.magnitude < 0.01f)
        {
            velocity = Vector3.zero;
        }
    }

    // Clamp Camera Movement Within Bounds
    private Vector3 ClampToBounds(Vector3 position)
    {
        Vector3 clampedPosition = position;
        Vector3 bounceDirection = Vector3.zero;

        // Check each axis and bounce back if needed
        if (position.x < minBounds.x)
        {
            clampedPosition.x = minBounds.x;
            bounceDirection.x = 1;
        }
        else if (position.x > maxBounds.x)
        {
            clampedPosition.x = maxBounds.x;
            bounceDirection.x = -1;
        }

        if (position.y < minBounds.y)
        {
            clampedPosition.y = minBounds.y;
            bounceDirection.y = 1;
        }
        else if (position.y > maxBounds.y)
        {
            clampedPosition.y = maxBounds.y;
            bounceDirection.y = -1;
        }

        // If bounce is needed, apply it to the target position
        if (bounceDirection != Vector3.zero)
        {
            float bounceAmount = 2f; // Adjust as needed
            targetPosition += bounceDirection.normalized * bounceAmount;
        }

        return clampedPosition;
    }

    // Handle Mouse Scroll
    private void HandleMouseScroll(float scroll)
    {
        var pixelPerfectCamera = cam.GetComponent<UnityEngine.U2D.PixelPerfectCamera>();
        // Adjust the zoom level
        pixelPerfectCamera.assetsPPU = Mathf.Clamp(
            pixelPerfectCamera.assetsPPU + Mathf.RoundToInt(scroll * zoomSpeed),
            Mathf.RoundToInt(minZoom),
            Mathf.RoundToInt(maxZoom)
        );
    }


    // Optional method to immediately set camera position
    public void SetCameraPosition(Vector3 newPosition)
    {
        cam.transform.position = ClampToBounds(newPosition);
        targetPosition = cam.transform.position;
        velocity = Vector3.zero;
    }
}