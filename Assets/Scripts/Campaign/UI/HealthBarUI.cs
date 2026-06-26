using UnityEngine;
using UnityEngine.UI;

public class HealthBarUI : MonoBehaviour
{
    [SerializeField] private Image fillImage;

    [Header("Guard (Encirclement)")]
    [SerializeField] private Image[] guardIcons; // assign 3 shield icons in inspector

    [Header("Offset (local space)")]
    [SerializeField] private Vector3 localOffset = new Vector3(0f, 0.25f, 0f);

    [Header("Fill Animation")]
    [SerializeField] private float fillLerpSpeed = 4f;

    private Unit target;

    private float currentFill = 1f;
    private float targetFill = 1f;

    private void Start()
    {
        target = GetComponentInParent<Unit>();

        if (target != null)
        {
            float startRatio = (target.maxHP > 0) ? (float)target.hp / target.maxHP : 0f;
            currentFill = targetFill = Mathf.Clamp01(startRatio);

            if (fillImage != null)
                fillImage.fillAmount = currentFill;

            RefreshGuard(target.CurrentGuardStacks, target.MaxGuardStacks);
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            transform.localPosition = localOffset;
        }
    }
#endif

    private void LateUpdate()
    {
        transform.localPosition = localOffset;

        if (fillImage == null) return;

        currentFill = Mathf.MoveTowards(
            currentFill,
            targetFill,
            fillLerpSpeed * Time.deltaTime
        );

        fillImage.fillAmount = currentFill;
    }

    public void Refresh(int current, int max)
    {
        if (fillImage == null) return;

        float ratio = (max > 0) ? (float)current / max : 0f;
        targetFill = Mathf.Clamp01(ratio);
    }

    // 🔹 NEW
    public void RefreshGuard(int currentGuard, int maxGuard)
    {
        if (guardIcons == null || guardIcons.Length == 0) return;

        for (int i = 0; i < guardIcons.Length; i++)
        {
            if (guardIcons[i] == null) continue;

            if (i >= maxGuard)
            {
                guardIcons[i].gameObject.SetActive(false);
                continue;
            }

            guardIcons[i].gameObject.SetActive(true);

            bool active = i < currentGuard;

            // Full color if stack exists, faded if consumed
            guardIcons[i].color = active
                ? Color.white
                : new Color(1f, 1f, 1f, 0.25f);
        }
    }
}