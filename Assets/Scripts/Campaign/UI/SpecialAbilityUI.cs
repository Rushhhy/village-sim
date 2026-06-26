using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class SpecialAbilityUI : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject root;

    [Header("Controls")]
    [SerializeField] private Button specialButton;
    [SerializeField] private TMP_Text specialButtonText;      // AbilityNameText
    [SerializeField] private TMP_Text cooldownText;           // hidden unless on cooldown
    [SerializeField] private TMP_Text abilityDescriptionText; // hidden unless special mode is active

    [SerializeField] private Transform buttonTransform;
    [SerializeField] private float selectedScale = 1.08f;
    [SerializeField] private float normalScale = 1f;

    [SerializeField] private Image specialButtonImage;
    [SerializeField] private float selectedBrightness = 1.15f;
    [SerializeField] private float normalBrightness = 1f;

    private Unit currentUnit;
    private System.Action onSpecialPressed;
    private bool specialModeActive = false;

    private void Awake()
    {
        if (root != null)
            root.SetActive(false);

        if (cooldownText != null)
            cooldownText.gameObject.SetActive(false);

        if (abilityDescriptionText != null)
            abilityDescriptionText.gameObject.SetActive(false);

        if (specialButton != null)
        {
            specialButton.onClick.AddListener(() =>
            {
                if (currentUnit == null) return;
                if (!currentUnit.HasSpecialAttack) return;

                // Allow click to either enter or cancel special mode
                if (!currentUnit.CanUseSpecial && !specialModeActive) return;

                onSpecialPressed?.Invoke();
            });
        }

        ApplyButtonVisualState(false);
    }

    public void Show(Unit unit, System.Action specialPressedAction, bool specialMode)
    {
        currentUnit = unit;
        onSpecialPressed = specialPressedAction;
        specialModeActive = specialMode;

        Refresh();
    }

    public void Hide()
    {
        if (root != null)
            root.SetActive(false);

        if (cooldownText != null)
            cooldownText.gameObject.SetActive(false);

        if (abilityDescriptionText != null)
            abilityDescriptionText.gameObject.SetActive(false);

        currentUnit = null;
        onSpecialPressed = null;
        specialModeActive = false;

        ApplyButtonVisualState(false);
    }

    public void Refresh()
    {
        if (currentUnit == null || root == null)
        {
            if (root != null)
                root.SetActive(false);

            if (cooldownText != null)
                cooldownText.gameObject.SetActive(false);

            if (abilityDescriptionText != null)
                abilityDescriptionText.gameObject.SetActive(false);

            ApplyButtonVisualState(false);
            return;
        }

        bool shouldShow =
            currentUnit.team == Unit.Team.Player &&
            currentUnit.IsAlive &&
            currentUnit.CurrentTile != null &&
            !currentUnit.HasActed &&
            currentUnit.HasSpecialAttack;

        root.SetActive(shouldShow);

        if (!shouldShow)
        {
            if (cooldownText != null)
                cooldownText.gameObject.SetActive(false);

            if (abilityDescriptionText != null)
                abilityDescriptionText.gameObject.SetActive(false);

            ApplyButtonVisualState(false);
            return;
        }

        bool canUse = currentUnit.CanUseSpecial;
        bool canClick = canUse || specialModeActive;

        if (specialButton != null)
            specialButton.interactable = canClick;

        if (specialButtonText != null)
            specialButtonText.text = "Special";

        if (cooldownText != null)
        {
            bool showCooldown = !canUse;
            cooldownText.gameObject.SetActive(showCooldown);

            if (showCooldown)
                cooldownText.text = $"{currentUnit.CurrentSpecialCooldown}";
        }

        if (abilityDescriptionText != null)
        {
            bool showDescription = specialModeActive && canUse;
            abilityDescriptionText.gameObject.SetActive(showDescription);

            if (showDescription)
                abilityDescriptionText.text = "Select an enemy target";
        }

        ApplyButtonVisualState(specialModeActive && canUse);
    }

    private void ApplyButtonVisualState(bool selected)
    {
        // Scale (main signal)
        if (buttonTransform != null)
        {
            buttonTransform.localScale = selected
                ? Vector3.one * selectedScale
                : Vector3.one * normalScale;
        }

        // Subtle brightness (not color shift)
        if (specialButtonImage != null)
        {
            float b = selected ? selectedBrightness : normalBrightness;
            specialButtonImage.color = new Color(b, b, b, 1f);
        }
    }

    public void PlayButtonPop()
    {
        if (buttonTransform != null)
            StartCoroutine(ButtonPop());
    }

    private IEnumerator ButtonPop()
    {
        float t = 0f;
        float duration = 0.08f;

        float start = 1f;
        float peak = selectedScale * 1.1f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float s = Mathf.Lerp(start, peak, t / duration);
            buttonTransform.localScale = Vector3.one * s;
            yield return null;
        }

        t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float s = Mathf.Lerp(peak, selectedScale, t / duration);
            buttonTransform.localScale = Vector3.one * s;
            yield return null;
        }
    }
}