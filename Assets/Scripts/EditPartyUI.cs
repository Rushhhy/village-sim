using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EditPartyUI : MonoBehaviour
{
    [Header("Managers")]
    [SerializeField] private CampaignPartyManager partyManager;
    [SerializeField] private VillagerManager villagerManager;
    [SerializeField] private VillagerInventoryManager villagerInventoryManager;

    [Header("Unit Slots")]
    [SerializeField] private PartySlotUI[] partySlots;

    [Header("Party Buttons")]
    [SerializeField] private Button[] partyButtons;
    [SerializeField] private Image[] partyButtonImages;
    [SerializeField] private Sprite[] partySelectedSprites;
    [SerializeField] private Sprite[] partyUnselectedSprites;

    [Header("Choose Main Button")]
    [SerializeField] private Button chooseMainButton;
    [SerializeField] private Image chooseMainButtonImage;
    [SerializeField] private TMP_Text chooseMainButtonText;
    [SerializeField] private Sprite selectAsMainSprite;
    [SerializeField] private Sprite currentMainSprite;

    [SerializeField] private GameObject editPartyPanel;
    [SerializeField] private GameObject campaignPanel; // campaign menu or whatever you came from

    private HashSet<int> currentPartyVillagers = new HashSet<int>();

    private void Start()
    {
        SetupUnitSlots();
        SetupPartyButtons();

        chooseMainButton.onClick.AddListener(ChooseCurrentPartyAsMain);

        RefreshUI();
    }

    private void SetupUnitSlots()
    {
        for (int i = 0; i < partySlots.Length; i++)
        {
            int slotIndex = i;
            partySlots[i].Setup(slotIndex, OnPartySlotClicked, ClearPartySlot);
        }
    }

    private void SetupPartyButtons()
    {
        for (int i = 0; i < partyButtons.Length; i++)
        {
            int partyIndex = i;
            partyButtons[i].onClick.AddListener(() => SelectParty(partyIndex));
        }
    }

    private void SelectParty(int partyIndex)
    {
        partyManager.selectedPartyIndex = partyIndex;
        RefreshUI();
    }

    private void OnPartySlotClicked(int slotIndex)
    {
        int selectedParty = partyManager.selectedPartyIndex;

        villagerInventoryManager.SetDisabledVillagers(
            partyManager.GetVillagersInParty(selectedParty)
        );

        villagerInventoryManager.OpenPartySelection((villagerIndex) =>
        {
            bool assigned = partyManager.SetVillagerInSlot(selectedParty, slotIndex, villagerIndex);

            if (!assigned)
            {
                Debug.Log("This villager is already in this party.");
                return;
            }

            RefreshUI();
        });
    }

    private void ChooseCurrentPartyAsMain()
    {
        int selectedParty = partyManager.selectedPartyIndex;

        partyManager.mainPartyIndex = selectedParty;
        RefreshUI();
    }

    private void RefreshUI()
    {
        RefreshPartyButtons();
        RefreshChooseMainButton();
        RefreshPartySlots();
    }

    private void RefreshPartyButtons()
    {
        for (int i = 0; i < partyButtonImages.Length; i++)
        {
            bool isSelected = i == partyManager.selectedPartyIndex;
            partyButtonImages[i].sprite = isSelected ? partySelectedSprites[i] : partyUnselectedSprites[i];
        }
    }

    private void RefreshChooseMainButton()
    {
        bool viewingMainParty = partyManager.selectedPartyIndex == partyManager.mainPartyIndex;

        chooseMainButtonText.text = viewingMainParty ? "Current Main" : "Select as Main";
        chooseMainButtonImage.sprite = viewingMainParty ? currentMainSprite : selectAsMainSprite;
    }

    private void RefreshPartySlots()
    {
        CampaignPartyData party = partyManager.parties[partyManager.selectedPartyIndex];

        for (int i = 0; i < partySlots.Length; i++)
        {
            int villagerIndex = party.villagerIndexes[i];

            if (villagerIndex != -1)
                currentPartyVillagers.Add(villagerIndex);

            if (villagerIndex == -1)
            {
                partySlots[i].ShowEmpty();
                continue;
            }

            Villager villager = villagerManager.GetHeldVillagerAt(villagerIndex);

            if (villager == null)
            {
                partySlots[i].ShowEmpty();
                continue;
            }

            partySlots[i].ShowVillager(villager, villager.GetStars());
        }
    }
    public void ExitEditParty()
    {
        editPartyPanel.SetActive(false);

        if (campaignPanel != null)
            campaignPanel.SetActive(true);
    }

    public void OpenEditParty()
    {
        editPartyPanel.SetActive(true);

        if (campaignPanel != null)
            campaignPanel.SetActive(false);

        RefreshUI();
    }
    private void ClearPartySlot(int slotIndex)
    {
        int selectedParty = partyManager.selectedPartyIndex;
        partyManager.ClearVillagerInSlot(selectedParty, slotIndex);
        RefreshUI();
    }
}