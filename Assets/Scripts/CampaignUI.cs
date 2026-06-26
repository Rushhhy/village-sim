using UnityEngine;
using UnityEngine.SceneManagement;
public class CampaignUI : MonoBehaviour
{
    [Header("Campaign Menu")]
    [SerializeField] private GameObject campaignMenu;
    [Header("Headers")]
    [SerializeField] private GameObject headerNormal;
    [SerializeField] private GameObject headerHard;

    [Header("Edit Party")]
    [SerializeField] private GameObject editPartyNormal;
    [SerializeField] private GameObject editPartyHard;

    [Header("Navigation")]
    [SerializeField] private GameObject normalModeButton;
    [SerializeField] private GameObject hardModeButton;

    [Header("Animals Campaign")]
    [SerializeField] private GameObject animalsNormal;
    [SerializeField] private GameObject animalsHard;

    [Header("Bandits Campaign")]
    [SerializeField] private GameObject banditsNormal;
    [SerializeField] private GameObject banditsHard;

    [Header("Pirates Campaign")]
    [SerializeField] private GameObject piratesNormal;
    [SerializeField] private GameObject piratesHard;

    [Header("Kingdom Campaign")]
    [SerializeField] private GameObject kingdomNormal;
    [SerializeField] private GameObject kingdomHard;

    [Header("Vikings Campaign")]
    [SerializeField] private GameObject vikingsNormal;
    [SerializeField] private GameObject vikingsHard;

    [Header("Japan Campaign")]
    [SerializeField] private GameObject japanNormal;
    [SerializeField] private GameObject japanHard;

    [Header("Previous and Next Buttons")]
    [SerializeField] private GameObject prevButton;
    [SerializeField] private GameObject nextButton;

    private int currentMenuID = 0;
    private bool isHard = false;

    private GameObject[] normalMaps;
    private GameObject[] hardMaps;

    [SerializeField] private CampaignPartyManager partyManager;
    [SerializeField] private string battleSceneName = "BattleScene";

    [SerializeField] private VillagerManager villagerManager;

    public void StartCampaignLevel(int levelIndex)
    {
        CampaignBattleData.campaignIndex = currentMenuID;
        CampaignBattleData.levelIndex = levelIndex;
        CampaignBattleData.isHard = isHard;

        CampaignPartyData mainParty = partyManager.parties[partyManager.mainPartyIndex];
        CampaignBattleData.partyVillagerData = new VillagerData[mainParty.villagerIndexes.Length];
        CampaignBattleData.partyCavalry = new bool[mainParty.villagerIndexes.Length];

        for (int i = 0; i < mainParty.villagerIndexes.Length; i++)
        {
            int villagerIndex = mainParty.villagerIndexes[i];

            if (villagerIndex == -1)
                continue;

            Villager villager = villagerManager.GetHeldVillagerAt(villagerIndex);

            if (villager == null)
                continue;

            CampaignBattleData.partyVillagerData[i] = villager.villagerData;
            CampaignBattleData.partyCavalry[i] = false;
        }
        CampaignBattleData.partyVillagerIndexes = mainParty.villagerIndexes;

        SceneManager.LoadScene(battleSceneName);
    }

    public void Awake()
    {
        normalMaps = new GameObject[] { animalsNormal, banditsNormal, piratesNormal, kingdomNormal, vikingsNormal, japanNormal };
        hardMaps = new GameObject[] { animalsHard, banditsHard, piratesHard, kingdomHard, vikingsHard, japanHard };
    }

    public void OpenCampaignMenu()
    {
        campaignMenu.SetActive(true);
        OpenCurrentMenu();
        AdjustPrevNextButtons();
    }

    public void CloseCampaignMenu() {
        campaignMenu.SetActive(false);
        switchToNormal();
        CloseCurrentMenu();
    }


    public void NextLevel()
    {
        CloseCurrentMenu();
        currentMenuID++;
        OpenCurrentMenu();
        AdjustPrevNextButtons();
    }

    public void PrevLevel()
    {
        CloseCurrentMenu();
        currentMenuID--;
        OpenCurrentMenu();
        AdjustPrevNextButtons();
    }

    public void OpenCurrentMenu()
    {
        if (isHard)
            hardMaps[currentMenuID].SetActive(true);
        else
            normalMaps[currentMenuID].SetActive(true);
    }
    public void CloseCurrentMenu()
    {
        if (isHard)
            hardMaps[currentMenuID].SetActive(false);
        else
            normalMaps[currentMenuID].SetActive(false);
    }

    public void switchToHard()
    {
        isHard = true;

        headerNormal.SetActive(false);
        headerHard.SetActive(true);

        editPartyNormal.SetActive(false);
        editPartyHard.SetActive(true);

        hardModeButton.SetActive(false);
        normalModeButton.SetActive(true);

        normalMaps[currentMenuID].SetActive(false);
        hardMaps[currentMenuID].SetActive(true);
    }
    public void switchToNormal()
    {
        isHard = false;

        headerNormal.SetActive(true);
        headerHard.SetActive(false);

        editPartyNormal.SetActive(true);
        editPartyHard.SetActive(false);

        hardModeButton.SetActive(true);
        normalModeButton.SetActive(false);

        normalMaps[currentMenuID].SetActive(true);
        hardMaps[currentMenuID].SetActive(false);
    }

    public void AdjustPrevNextButtons()
    {
        if (currentMenuID == 0)
        {
            prevButton.SetActive(false);
        }
        else if (currentMenuID == 5)
        {
            nextButton.SetActive(false);
        }
        else
        {
            prevButton.SetActive(true);
            nextButton.SetActive(true);
        }
    }
}
