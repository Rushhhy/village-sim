using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class VillagerCardUI : MonoBehaviour
{
    [Header("Core")]
    public TextMeshProUGUI NameText;
    public Image VillagerImage;
    public Button SelectButton;

    [Header("Optional")]
    public GameObject AddedBadge;
    public TextMeshProUGUI WorkStatusText;
    public TextMeshProUGUI DescriptionText;

    public void Setup(string villagerName, Sprite villagerIcon)
    {
        if (NameText != null)
            NameText.text = villagerName;

        if (VillagerImage != null)
        {
            VillagerImage.sprite = villagerIcon;
            VillagerImage.SetNativeSize();
        }
    }

    public void SetAdded(bool active)
    {
        if (AddedBadge != null)
            AddedBadge.SetActive(active);
    }

    public void SetWorkStatus(string statusText, Color statusColor)
    {
        if (WorkStatusText == null)
            return;

        WorkStatusText.text = statusText;
        WorkStatusText.color = statusColor;
    }

    public void SetDescription(string description)
    {
        if (DescriptionText != null)
            DescriptionText.text = description;
    }

    public void BindButton(Action onClick)
    {
        if (SelectButton == null)
            return;

        SelectButton.onClick.RemoveAllListeners();
        SelectButton.onClick.AddListener(() => onClick?.Invoke());
    }

    public void SetInteractable(bool interactable)
    {
        if (SelectButton != null)
            SelectButton.interactable = interactable;
    }
}