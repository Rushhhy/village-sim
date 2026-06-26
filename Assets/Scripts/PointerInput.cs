using UnityEngine;

public static class PointerInput
{
    /// <summary>
    /// Returns true when a "tap/click" begins this frame.
    /// Works in Editor/PC (mouse) and on mobile (touch).
    /// </summary>
    public static bool PressBegan()
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        return Input.GetMouseButtonDown(0);
#else
        return Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began;
#endif
    }

    /// <summary>
    /// Returns current screen position of the active pointer.
    /// </summary>
    public static Vector2 ScreenPosition()
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        return Input.mousePosition;
#else
        return Input.touchCount > 0 ? Input.GetTouch(0).position : Vector2.zero;
#endif
    }

    /// <summary>
    /// Convenience: pointer world position for a given camera.
    /// </summary>
    public static Vector2 WorldPosition(Camera cam)
    {
        return cam.ScreenToWorldPoint(ScreenPosition());
    }
}