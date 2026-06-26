using TMPro;
using UnityEngine;

public class CurrencyBarUI : MonoBehaviour
{
    [SerializeField] private ResourceManager resourceManager;

    [Header("Text")]
    [SerializeField] private TextMeshProUGUI coinsText;
    [SerializeField] private TextMeshProUGUI gemsText;

    private void Awake()
    {
        if (resourceManager == null)
        {
            resourceManager = FindObjectOfType<ResourceManager>();
        }
    }

    private void OnEnable()
    {
        if (resourceManager == null)
        {
            return;
        }

        resourceManager.OnCoinsChanged += UpdateCoinsText;
        resourceManager.OnGemsChanged += UpdateGemsText;

        UpdateCoinsText(resourceManager.coins);
        UpdateGemsText(resourceManager.gems);
    }

    private void OnDisable()
    {
        if (resourceManager == null)
        {
            return;
        }

        resourceManager.OnCoinsChanged -= UpdateCoinsText;
        resourceManager.OnGemsChanged -= UpdateGemsText;
    }

    private void UpdateCoinsText(int amount)
    {
        if (coinsText != null)
        {
            coinsText.text = amount.ToString();
        }
    }

    private void UpdateGemsText(int amount)
    {
        if (gemsText != null)
        {
            gemsText.text = amount.ToString();
        }
    }
}