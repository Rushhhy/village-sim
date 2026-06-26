using UnityEngine;
using UnityEngine.UI;

public class ResourceGainPopup : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private CanvasGroup canvasGroup;

    [SerializeField] private float moveSpeed = 1.2f;
    [SerializeField] private float lifetime = 0.8f;
    [SerializeField] private float verticalDistance = 1f;

    private Vector3 startPosition;
    private Vector3 targetPosition;
    private float timer;

    [SerializeField] private float iconScale = 0.025f;
    [SerializeField] private float horizontalOffset = 0.5f;

    public void Initialize(Sprite icon)
    {
        if (iconImage == null || canvasGroup == null || icon == null)
        {
            return;
        }

        iconImage.sprite = icon;
        iconImage.SetNativeSize();

        // 👇 THIS is the important line
        iconImage.rectTransform.localScale = Vector3.one * iconScale;

        startPosition = transform.position + new Vector3(horizontalOffset, 0f, 0f);
        transform.position = startPosition;
        targetPosition = startPosition + new Vector3(0f, verticalDistance, 0f);

        timer = 0f;
        canvasGroup.alpha = 1f;
    }

    private void Update()
    {
        timer += Time.deltaTime;

        float t = Mathf.Clamp01(timer / lifetime);

        transform.position = Vector3.Lerp(startPosition, targetPosition, t);
        canvasGroup.alpha = 1f - t;

        if (timer < 0.05f)
        {
            Debug.Log($"[POPUP] Updating. position={transform.position}, alpha={canvasGroup.alpha}");
        }

        if (t >= 1f)
        {
            Destroy(gameObject);
        }
    }
}