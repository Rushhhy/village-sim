using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ScrollRectZoomHandler : MonoBehaviour, IScrollHandler
{
    [Header("References")]
    [SerializeField] private ScrollRect scrollRect;      // Scroll View (ScrollRect component)
    [SerializeField] private RectTransform viewport;     // Viewport RectTransform
    [SerializeField] private RectTransform content;      // Content RectTransform (map container)

    [Header("Zoom")]
    [SerializeField] private float zoomSpeed = 0.15f;    // Wheel sensitivity
    [SerializeField] private float maxZoom = 2.0f;       // Maximum scale
    [SerializeField] private float extraPadding = 0.00f; // e.g. 0.05 for a little extra cover

    private Canvas canvas;
    private float minZoom = 1f;
    private Vector2 lastViewportSize;

    private void Awake()
    {
        if (!scrollRect) scrollRect = GetComponentInParent<ScrollRect>();
        if (!viewport && scrollRect) viewport = scrollRect.viewport;
        if (!content && scrollRect) content = scrollRect.content;

        canvas = GetComponentInParent<Canvas>();
    }

    private void Start()
    {
        RecalculateMinZoom();
        lastViewportSize = viewport ? viewport.rect.size : Vector2.zero;

        // If we're already too small at start, snap up to min zoom.
        ClampScaleToMinMax();
        Canvas.ForceUpdateCanvases();
        if (scrollRect) scrollRect.StopMovement();
    }

    private void Update()
    {
        // Handles resolution changes, UI scaling changes, window resizing, etc.
        if (!viewport || !content) return;

        Vector2 size = viewport.rect.size;
        if (size != lastViewportSize)
        {
            lastViewportSize = size;

            RecalculateMinZoom();
            ClampScaleToMinMax();

            Canvas.ForceUpdateCanvases();
            if (scrollRect) scrollRect.StopMovement();
        }
    }

    public void OnScroll(PointerEventData eventData)
    {
        if (!scrollRect || !content || !viewport) return;

        // Make wheel zoom instead of ScrollRect scrolling.
        eventData.Use();

        float wheel = eventData.scrollDelta.y;
        if (Mathf.Abs(wheel) < 0.001f) return;

        RecalculateMinZoom(); // in case something changed since last frame

        float current = content.localScale.x;
        float target = Mathf.Clamp(current * (1f + wheel * zoomSpeed), minZoom, maxZoom);
        float factor = target / current;

        if (Mathf.Abs(factor - 1f) < 0.0001f) return;

        // Zoom towards the cursor position
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            content, eventData.position, canvas ? canvas.worldCamera : null, out Vector2 localBefore);

        content.localScale = new Vector3(target, target, 1f);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            content, eventData.position, canvas ? canvas.worldCamera : null, out Vector2 localAfter);

        content.anchoredPosition += (localAfter - localBefore);

        // Rebuild bounds so clamping behaves correctly
        Canvas.ForceUpdateCanvases();
        scrollRect.StopMovement();

        // Optional: If you use Elastic movement, this helps it settle quicker
        // scrollRect.velocity = Vector2.zero;
    }

    private void RecalculateMinZoom()
    {
        if (!viewport || !content) return;

        Vector2 viewSize = viewport.rect.size;
        Vector2 contentSize = content.rect.size;

        // If Content has no valid size yet (e.g., layout not built), try again later
        if (contentSize.x <= 0.01f || contentSize.y <= 0.01f) return;

        float minX = viewSize.x / contentSize.x;
        float minY = viewSize.y / contentSize.y;

        // The min zoom that guarantees the content covers the viewport in both directions
        minZoom = Mathf.Max(minX, minY) + extraPadding;

        // Safety: don't let minZoom exceed maxZoom (would lock scaling)
        if (minZoom > maxZoom) minZoom = maxZoom;
    }

    private void ClampScaleToMinMax()
    {
        if (!content) return;

        float s = Mathf.Clamp(content.localScale.x, minZoom, maxZoom);
        content.localScale = new Vector3(s, s, 1f);
    }
}
