using TMPro;
using UnityEngine;

public class DamagePopup : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI text;
    [SerializeField] private float lifeTime = 0.6f;
    [SerializeField] private float floatSpeed = 40f;   // rough pixels/sec
    [SerializeField] private float fadeOutTime = 0.3f;

    private float timer;
    private RectTransform rect;
    private Camera cam;
    private Vector3 worldPos;

    public void Setup(int amount, Vector3 spawnWorldPos)
    {
        if (rect == null)
            rect = GetComponent<RectTransform>();

        cam = Camera.main;
        worldPos = spawnWorldPos;

        if (text != null)
            text.text = amount.ToString();

        UpdateScreenPos();
    }

    private void Update()
    {
        timer += Time.deltaTime;

        // Float upward a bit
        worldPos += Vector3.up * (floatSpeed * 0.01f) * Time.deltaTime;
        UpdateScreenPos();

        // Fade out near the end
        if (text != null && timer > lifeTime - fadeOutTime)
        {
            float t = (timer - (lifeTime - fadeOutTime)) / fadeOutTime;
            Color c = text.color;
            c.a = Mathf.Lerp(1f, 0f, t);
            text.color = c;
        }

        if (timer >= lifeTime)
            Destroy(gameObject);
    }

    private void UpdateScreenPos()
    {
        if (cam == null || rect == null) return;

        Vector3 screen = cam.WorldToScreenPoint(worldPos);
        rect.position = screen;
    }
}