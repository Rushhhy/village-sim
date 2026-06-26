using UnityEngine;

public class DeathSkull : MonoBehaviour
{
    [SerializeField] private float duration = 0.6f;
    [SerializeField] private float riseDistance = 0.7f;

    private float elapsed;
    private Vector3 startPos;
    private SpriteRenderer sr;

    private void Awake()
    {
        startPos = transform.position;
        sr = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / duration);

        // Move upward over time
        transform.position = startPos + Vector3.up * (riseDistance * t);

        // Fade out
        if (sr != null)
        {
            Color c = sr.color;
            c.a = 1f - t;
            sr.color = c;
        }

        if (elapsed >= duration)
        {
            Destroy(gameObject);
        }
    }
}