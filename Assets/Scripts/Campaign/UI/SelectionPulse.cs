using UnityEngine;

public class SelectionPulse : MonoBehaviour
{
    [SerializeField] private float pulseSpeed = 3f;
    [SerializeField] private float scaleAmplitude = 0.1f;

    private Vector3 baseScale;

    private void Awake()
    {
        baseScale = transform.localScale;
    }

    private void Update()
    {
        float s = 1f + Mathf.Sin(Time.time * pulseSpeed) * scaleAmplitude;
        transform.localScale = baseScale * s;
    }
}