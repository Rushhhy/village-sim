using UnityEngine;

public class ArrowProjectile : MonoBehaviour
{
    [SerializeField] private float travelTime = 0.35f;   // total time to reach target
    [SerializeField] private float arcHeight = 0.5f;     // how "high" the arc goes

    private Transform target;
    private Vector3 startPos;
    private Vector3 fixedTargetPos;
    private float elapsed;
    private bool initialized;

    private SpriteRenderer sr;
    private Vector3 lastPos;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        lastPos = transform.position;
    }

    /// <summary>
    /// Call this right after instantiating the arrow.
    /// </summary>
    public void Launch(Transform targetTransform, Sprite arrowSprite = null,
                       float customTravelTime = -1f, float customArcHeight = -1f)
    {
        if (targetTransform == null)
        {
            Destroy(gameObject);
            return;
        }

        target = targetTransform;
        startPos = transform.position;
        fixedTargetPos = target.position;   // freeze target position at launch
        elapsed = 0f;
        initialized = true;

        if (customTravelTime > 0f)
            travelTime = customTravelTime;

        if (customArcHeight >= 0f)
            arcHeight = customArcHeight;

        if (arrowSprite != null && sr != null)
            sr.sprite = arrowSprite;

        lastPos = startPos;
    }

    private void Update()
    {
        if (!initialized)
            return;

        if (target == null)
        {
            Destroy(gameObject);
            return;
        }

        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / travelTime);

        // Straight-line position
        Vector3 straight = Vector3.Lerp(startPos, fixedTargetPos, t);

        // Decide if we use an arc: only for mostly-horizontal shots
        float dx = fixedTargetPos.x - startPos.x;
        float dy = fixedTargetPos.y - startPos.y;
        bool useArc = Mathf.Abs(dx) > Mathf.Abs(dy);   // side shots = true

        float height = 0f;
        if (useArc && arcHeight > 0f)
        {
            // Simple parabola peaking at t = 0.5: 4t(1-t) in [0, 1]
            height = 4f * arcHeight * t * (1f - t);
        }

        Vector3 pos = straight + Vector3.up * height;
        transform.position = pos;

        // Rotation based on movement direction
        Vector3 dir = pos - lastPos;
        if (dir.sqrMagnitude > 0.0001f)
        {
            // Sprite points UP (along +Y), so subtract 90° from the Atan2 based on +X
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
            transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        }

        lastPos = pos;

        if (t >= 1f)
        {
            Destroy(gameObject);
        }
    }
}