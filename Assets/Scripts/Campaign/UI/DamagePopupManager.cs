using UnityEngine;

public class DamagePopupManager : MonoBehaviour
{
    public static DamagePopupManager Instance { get; private set; }

    [SerializeField] private DamagePopup popupPrefab;
    [SerializeField] private Canvas popupCanvas;

    [Header("Random Offset Settings")]
    [Tooltip("Horizontal range: popup X offset will be in [-xRange, +xRange].")]
    [SerializeField] private float xRange = 0.3f;

    [Tooltip("Vertical offset range above the unit.")]
    [SerializeField] private float minY = 0.2f;
    [SerializeField] private float maxY = 0.5f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // Fallback to first Canvas in scene if not assigned
        if (popupCanvas == null)
            popupCanvas = FindObjectOfType<Canvas>();
    }

    public void Spawn(int amount, Vector3 worldPos)
    {
        if (popupPrefab == null || popupCanvas == null) return;

        // Random horizontal offset (slightly left/right)
        float offX = Random.Range(-xRange, xRange);

        // Random vertical offset, but always ABOVE (positive Y)
        float offY = Random.Range(minY, maxY);

        Vector3 offset = new Vector3(offX, offY, 0f);

        DamagePopup popup = Instantiate(popupPrefab, popupCanvas.transform);
        popup.Setup(amount, worldPos + offset);
    }
}