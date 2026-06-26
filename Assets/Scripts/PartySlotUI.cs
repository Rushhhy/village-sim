using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PartySlotUI : MonoBehaviour
{
    [Header("Main Button")]
    [SerializeField] private Button unitSelectButton;
    [SerializeField] private Image unitSelectImage;
    [SerializeField] private Sprite emptySlotSprite;

    [Header("Texts")]
    [SerializeField] private TMP_Text unitNameText;
    [SerializeField] private TMP_Text hpText;

    [Header("Health")]
    [SerializeField] private Image healthFill;

    [Header("Stars")]
    [SerializeField] private Image[] stars;
    [SerializeField] private Sprite blackStarSprite;
    [SerializeField] private Sprite normalStarSprite;

    private Vector2 emptyIconSize = new Vector2(33.62601f, 33.62601f);
    private Vector2 unitIconSize = new Vector2(50f, 50f);

    private int slotIndex;

    [SerializeField] private Button clearButton;

    public void Setup(int index, Action<int> onClicked, Action<int> onClearClicked)
    {
        slotIndex = index;

        unitSelectButton.onClick.RemoveAllListeners();
        unitSelectButton.onClick.AddListener(() => onClicked?.Invoke(slotIndex));

        clearButton.onClick.RemoveAllListeners();
        clearButton.onClick.AddListener(() => onClearClicked?.Invoke(slotIndex));
    }

    public void ShowEmpty()
    {
        unitNameText.text = "Empty";
        hpText.text = "HP: ---";
        healthFill.fillAmount = 0f;
        unitSelectImage.sprite = emptySlotSprite;

        clearButton.gameObject.SetActive(false);

        unitSelectImage.sprite = emptySlotSprite;
        SetIconSize(emptyIconSize);

        SetStars(0);
    }

    public void ShowVillager(Villager villager, int starsAmount)
    {
        unitNameText.text = villager.villagerData.Name;

        float maxHp = villager.HEALTH;
        float currentHp = villager.HEALTH;

        hpText.text = $"HP: {currentHp:0}/{maxHp:0}";
        healthFill.fillAmount = currentHp / maxHp;

        unitSelectImage.sprite = villager.villagerData.villagerIcon;

        clearButton.gameObject.SetActive(true);

        unitSelectImage.sprite = villager.villagerData.villagerIcon;
        SetIconSize(unitIconSize);

        SetStars(starsAmount);
    }

    private void SetStars(int starsAmount)
    {
        for (int i = 0; i < stars.Length; i++)
        {
            stars[i].sprite = i < starsAmount ? normalStarSprite : blackStarSprite;
        }
    }
    private void SetIconSize(Vector2 size)
    {
        RectTransform rect = unitSelectImage.GetComponent<RectTransform>();
        if (rect != null)
            rect.sizeDelta = size;
    }
}