using System.Collections;
using UnityEngine;

public class CombatCameraController : MonoBehaviour
{
    public static CombatCameraController Instance { get; private set; }

    [Header("Mouse Drag Camera")]
    [SerializeField] private bool allowMouseDrag = true;
    [SerializeField] private float zoomSpeed = 8f;
    [SerializeField] private float minZoom = 3f;
    [SerializeField] private float maxZoom = 8f;

    private bool cameraLocked;
    private Vector3 lastMouseWorldPos;

    [Header("Camera")]
    [SerializeField] private Camera cam;

    [Header("Zoom Settings")]
    [Tooltip("Default orthographic size when not focused.")]
    public float defaultSize = 5f;
    [Tooltip("Zoomed-in size while focusing on a unit.")]
    public float focusSize = 3.5f;

    [Tooltip("Seconds to zoom in.")]
    public float zoomInDuration = 0.3f;
    [Tooltip("Seconds to zoom back out.")]
    public float zoomOutDuration = 0.3f;

    [Header("Pan Settings")]
    [Tooltip("Seconds to initially pan camera to target on focus.")]
    public float panInDuration = 0.3f;
    [Tooltip("Seconds to pan camera back to original position.")]
    public float panOutDuration = 0.3f;

    [Header("Follow Settings")]
    [Tooltip("How quickly the camera follows the moving target while focused.")]
    public float followSpeed = 5f;

    private Coroutine currentRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (cam == null)
            cam = GetComponent<Camera>();

        if (cam == null)
            cam = Camera.main;

        if (cam != null)
            defaultSize = cam.orthographicSize;
    }

    private void Update()
    {
        if (!allowMouseDrag) return;
        if (cameraLocked) return;
        if (cam == null) return;

        if (Input.GetMouseButtonDown(1)) // right click drag
        {
            lastMouseWorldPos = cam.ScreenToWorldPoint(Input.mousePosition);
        }

        if (Input.GetMouseButton(1))
        {
            Vector3 currentMouseWorldPos = cam.ScreenToWorldPoint(Input.mousePosition);
            Vector3 difference = lastMouseWorldPos - currentMouseWorldPos;

            cam.transform.position += difference;
        }

        float scroll = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scroll) > 0.01f)
        {
            cam.orthographicSize -= scroll * zoomSpeed * Time.deltaTime;
            cam.orthographicSize = Mathf.Clamp(cam.orthographicSize, minZoom, maxZoom);
            defaultSize = cam.orthographicSize;
        }
    }

    /// <summary>
    /// Runs the given action coroutine while zoomed in and centered on the target.
    /// After the action finishes, zooms back out. While the action runs, the
    /// camera follows the target as it moves.
    /// </summary>
    public IEnumerator RunWithFocus(Transform target, IEnumerator action)
    {
        if (cam == null || target == null || action == null)
        {
            // Fallback: just run the action without camera effects
            if (action != null)
                yield return StartCoroutine(action);
            yield break;
        }

        // If another focus is running, stop it and reset camera
        if (currentRoutine != null)
        {
            StopCoroutine(currentRoutine);
            currentRoutine = null;
        }

        currentRoutine = StartCoroutine(FocusRoutine(target, action));
        yield return currentRoutine;
        currentRoutine = null;
    }

    private IEnumerator FocusRoutine(Transform target, IEnumerator action)
    {
        if (cam == null || target == null || action == null)
            yield break;

        cameraLocked = true;

        Vector3 originalPos = cam.transform.position;
        float originalSize = cam.orthographicSize;

        // Target camera position (keep original Z)
        Vector3 focusPos = new Vector3(target.position.x, target.position.y, originalPos.z);

        // --- Zoom & pan in to initial focus position ---
        float t = 0f;
        float durationIn = Mathf.Max(0.01f, Mathf.Max(zoomInDuration, panInDuration));
        while (t < 1f)
        {
            t += Time.deltaTime / durationIn;
            float alpha = Mathf.Clamp01(t);

            cam.transform.position = Vector3.Lerp(originalPos, focusPos, alpha);
            cam.orthographicSize = Mathf.Lerp(originalSize, focusSize, alpha);

            yield return null;
        }

        cam.transform.position = focusPos;
        cam.orthographicSize = focusSize;

        // --- While the action runs, follow the target as it moves ---
        // Use a separate coroutine so we don't have to manually step the action enumerator.
        Coroutine followRoutine = StartCoroutine(FollowTarget(target, originalPos.z));

        // Run the wrapped action (move/attack coroutine)
        yield return StartCoroutine(action);

        // Stop following once action is done
        if (followRoutine != null)
            StopCoroutine(followRoutine);

        // --- Zoom & pan back out to original camera state ---
        t = 0f;
        float durationOut = Mathf.Max(0.01f, Mathf.Max(zoomOutDuration, panOutDuration));
        Vector3 currentPos = cam.transform.position;
        float currentSize = cam.orthographicSize;

        while (t < 1f)
        {
            t += Time.deltaTime / durationOut;
            float alpha = Mathf.Clamp01(t);

            cam.transform.position = Vector3.Lerp(currentPos, originalPos, alpha);
            cam.orthographicSize = Mathf.Lerp(currentSize, originalSize, alpha);

            yield return null;
        }

        cam.transform.position = originalPos;
        cam.orthographicSize = originalSize;

        cameraLocked = false;
    }

    /// <summary>
    /// Smoothly follows the target's position while keeping a fixed Z.
    /// Stops automatically if the target is destroyed (null).
    /// </summary>
    private IEnumerator FollowTarget(Transform target, float fixedZ)
    {
        while (target != null && cam != null)
        {
            Vector3 desired = new Vector3(target.position.x, target.position.y, fixedZ);
            cam.transform.position = Vector3.Lerp(
                cam.transform.position,
                desired,
                Time.deltaTime * followSpeed
            );

            yield return null;
        }
    }

    public void CenterOnWorldPosition(Vector3 worldPos, float size = -1f)
    {
        if (cam == null) return;

        cam.transform.position = new Vector3(worldPos.x, worldPos.y, cam.transform.position.z);

        if (size > 0f)
        {
            cam.orthographicSize = size;
            defaultSize = size;
        }
    }

    public void CenterOnTileArea(GridManager grid, Vector2Int min, Vector2Int max, float size = -1f)
    {
        if (grid == null) return;

        Tile a = grid.GetTile(min);
        Tile b = grid.GetTile(max);

        if (a == null || b == null) return;

        Vector3 center = (a.transform.position + b.transform.position) * 0.5f;
        CenterOnWorldPosition(center, size);
    }
}