using System.Collections.Generic;
using UnityEngine;

public class VillagerInventoryManager : MonoBehaviour
{
    private static readonly Vector3 VillagerCardScale = new Vector3(0.75f, 0.75f, 0.75f);

    private const int ItemsPerRow = 4;
    private const float ExtraRowHeight = 95f;
    private static readonly Color UnemployedColor = new Color(1f, 0.6f, 0f);

    private System.Action<int> onPartyVillagerSelected;
    private bool isPartySelectionMode = false;

    private class OwnedVillagerUIEntry
    {
        public GameObject AllEntryObject;
        public VillagerCardUI AllEntryUI;
        public GameObject TierEntryObject;
        public VillagerCardUI TierEntryUI;

        public OwnedVillagerUIEntry(GameObject allEntryObject, VillagerCardUI allEntryUI, GameObject tierEntryObject, VillagerCardUI tierEntryUI)
        {
            AllEntryObject = allEntryObject;
            AllEntryUI = allEntryUI;
            TierEntryObject = tierEntryObject;
            TierEntryUI = tierEntryUI;
        }
    }

    private class AvailableVillagerUIEntry
    {
        public GameObject AvailableEntryObject;
        public VillagerCardUI AvailableEntryUI;
        public GameObject UnemployedEntryObject;
        public VillagerCardUI UnemployedEntryUI;

        public AvailableVillagerUIEntry(GameObject availableEntryObject, VillagerCardUI availableEntryUI, GameObject unemployedEntryObject, VillagerCardUI unemployedEntryUI)
        {
            AvailableEntryObject = availableEntryObject;
            AvailableEntryUI = availableEntryUI;
            UnemployedEntryObject = unemployedEntryObject;
            UnemployedEntryUI = unemployedEntryUI;
        }
    }

    private class PartyVillagerUIEntry
    {
        public int VillagerIndex;

        public GameObject AllEntryObject;
        public VillagerCardUI AllEntryUI;

        public GameObject TierEntryObject;
        public VillagerCardUI TierEntryUI;

        public PartyVillagerUIEntry(
            int villagerIndex,
            GameObject allEntryObject,
            VillagerCardUI allEntryUI,
            GameObject tierEntryObject,
            VillagerCardUI tierEntryUI)
        {
            VillagerIndex = villagerIndex;
            AllEntryObject = allEntryObject;
            AllEntryUI = allEntryUI;
            TierEntryObject = tierEntryObject;
            TierEntryUI = tierEntryUI;
        }
    }

    private class TierInventoryGroup
    {
        public GameObject Container;
        public List<GameObject> Items;
        public List<GameObject> Rows;

        public TierInventoryGroup(GameObject container, List<GameObject> items, List<GameObject> rows)
        {
            Container = container;
            Items = items;
            Rows = rows;
        }
    }

    [Header("Data")]
    [SerializeField] private VillagersDataSO villagerData;
    [SerializeField] private VillagerManager villagerManager;

    [Header("Prefabs")]
    [SerializeField] private GameObject villagerPrefab;
    [SerializeField] private GameObject availableVillagerPrefab;
    [SerializeField] private GameObject villagerRow;

    [Header("All Villagers Inventory Containers")]
    [SerializeField] private GameObject allVillagers;
    [SerializeField] private GameObject tierOneVillagers;
    [SerializeField] private GameObject tierTwoVillagers;
    [SerializeField] private GameObject tierThreeVillagers;

    [Header("All Villagers Inventory UI")]
    [SerializeField] private GameObject allDisplay;
    [SerializeField] private GameObject tierOneDisplay;
    [SerializeField] private GameObject tierTwoDisplay;
    [SerializeField] private GameObject tierThreeDisplay;
    [SerializeField] private GameObject allSelected;
    [SerializeField] private GameObject oneSelected;
    [SerializeField] private GameObject twoSelected;
    [SerializeField] private GameObject threeSelected;
    [SerializeField] private GameObject noVillagers;
    [SerializeField] private GameObject allInventory;

    [Header("Available Villagers Inventory UI")]
    [SerializeField] private GameObject availableInventory;
    [SerializeField] private GameObject allAvailableDisplay;
    [SerializeField] private GameObject unemployedAvailableDisplay;
    [SerializeField] private GameObject allAvailableSelected;
    [SerializeField] private GameObject unemployedSelected;
    [SerializeField] private GameObject noVillagersTwo;
    [SerializeField] private GameObject unemployedScroller;

    [Header("Party Villagers Inventory Containers")]
    [SerializeField] private GameObject partyAllVillagers;
    [SerializeField] private GameObject partyTierOneVillagers;
    [SerializeField] private GameObject partyTierTwoVillagers;
    [SerializeField] private GameObject partyTierThreeVillagers;

    [Header("Party Villagers Inventory UI")]
    [SerializeField] private GameObject partyInventory;
    [SerializeField] private GameObject partyAllDisplay;
    [SerializeField] private GameObject partyTierOneDisplay;
    [SerializeField] private GameObject partyTierTwoDisplay;
    [SerializeField] private GameObject partyTierThreeDisplay;
    [SerializeField] private GameObject partyAllSelected;
    [SerializeField] private GameObject partyOneSelected;
    [SerializeField] private GameObject partyTwoSelected;
    [SerializeField] private GameObject partyThreeSelected;
    [SerializeField] private GameObject partyNoVillagers;

    private readonly List<OwnedVillagerUIEntry> ownedVillagerEntries = new();
    private readonly List<AvailableVillagerUIEntry> availableVillagerEntries = new();
    private readonly List<PartyVillagerUIEntry> partyVillagerEntries = new();

    private readonly List<GameObject> allVillagerItems = new();
    private readonly List<GameObject> allVillagerRows = new();

    private readonly List<GameObject> tierOneVillagerItems = new();
    private readonly List<GameObject> tierOneVillagerRows = new();

    private readonly List<GameObject> tierTwoVillagerItems = new();
    private readonly List<GameObject> tierTwoVillagerRows = new();

    private readonly List<GameObject> tierThreeVillagerItems = new();
    private readonly List<GameObject> tierThreeVillagerRows = new();

    private readonly List<GameObject> allAvailableVillagers = new();
    private readonly List<GameObject> allAvailableVillagerRows = new();

    private readonly List<GameObject> unemployedItems = new();
    private readonly List<GameObject> unemployedRows = new();

    private readonly List<GameObject> partyAllVillagerItems = new();
    private readonly List<GameObject> partyAllVillagerRows = new();

    private readonly List<GameObject> partyTierOneVillagerItems = new();
    private readonly List<GameObject> partyTierOneVillagerRows = new();

    private readonly List<GameObject> partyTierTwoVillagerItems = new();
    private readonly List<GameObject> partyTierTwoVillagerRows = new();

    private readonly List<GameObject> partyTierThreeVillagerItems = new();
    private readonly List<GameObject> partyTierThreeVillagerRows = new();

    private readonly Dictionary<RectTransform, float> baseContainerHeights = new();

    private void Awake()
    {
        CacheBaseHeight(allVillagers);
        CacheBaseHeight(tierOneVillagers);
        CacheBaseHeight(tierTwoVillagers);
        CacheBaseHeight(tierThreeVillagers);

        CacheBaseHeight(allAvailableDisplay);
        CacheBaseHeight(unemployedAvailableDisplay);

        CacheBaseHeight(partyAllVillagers);
        CacheBaseHeight(partyTierOneVillagers);
        CacheBaseHeight(partyTierTwoVillagers);
        CacheBaseHeight(partyTierThreeVillagers);
    }

    private void Start()
    {
        villagerManager.OnVillagerBought += AddVillagerToInventory;
        villagerManager.OnVillagerHoused += AddToAvailableInventory;
        villagerManager.OnVillagerRemoved += RemoveFromAvailableInventory;
        villagerManager.OnVillagerEmployed += UpdateAvailableInventory;
        villagerManager.OnVillagerRemovedWithoutReplacement += AddToUnemployedInventoryNoReturn;
    }

    private void OnDestroy()
    {
        if (villagerManager == null)
            return;

        villagerManager.OnVillagerBought -= AddVillagerToInventory;
        villagerManager.OnVillagerHoused -= AddToAvailableInventory;
        villagerManager.OnVillagerRemoved -= RemoveFromAvailableInventory;
        villagerManager.OnVillagerEmployed -= UpdateAvailableInventory;
        villagerManager.OnVillagerRemovedWithoutReplacement -= AddToUnemployedInventoryNoReturn;
    }

    private void AddVillagerToInventory(int villagerID)
    {
        VillagerData villagerInfo = villagerData.GetVillagerDataByID(villagerID);
        if (villagerInfo == null)
            return;

        (GameObject allEntryObject, VillagerCardUI allEntryUI) = CreateVillagerCard(
            villagerPrefab,
            allVillagerItems,
            allVillagerRows,
            allVillagers.transform,
            villagerInfo);

        TierInventoryGroup tierGroup = GetTierGroup(villagerInfo.tier);
        if (tierGroup == null)
            return;

        (GameObject tierEntryObject, VillagerCardUI tierEntryUI) = CreateVillagerCard(
            villagerPrefab,
            tierGroup.Items,
            tierGroup.Rows,
            tierGroup.Container.transform,
            villagerInfo);

        ownedVillagerEntries.Add(new OwnedVillagerUIEntry(allEntryObject, allEntryUI, tierEntryObject, tierEntryUI));

        int villagerIndex = allVillagerItems.Count - 1;
        BindSelectButton(allEntryUI, villagerIndex);
        BindSelectButton(tierEntryUI, villagerIndex);
    }

    private void AddToAvailableInventory(int villagerIndex)
    {
        if (!IsValidOwnedIndex(villagerIndex))
            return;

        VillagerData villagerInfo = GetVillagerDataFromHeldIndex(villagerIndex);
        if (villagerInfo == null)
            return;

        (GameObject availableEntryObject, VillagerCardUI availableEntryUI) = CreateVillagerCard(
            availableVillagerPrefab,
            allAvailableVillagers,
            allAvailableVillagerRows,
            allAvailableDisplay.transform,
            villagerInfo);

        ownedVillagerEntries[villagerIndex].AllEntryUI.SetAdded(true);
        ownedVillagerEntries[villagerIndex].TierEntryUI.SetAdded(true);

        (GameObject unemployedEntryObject, VillagerCardUI unemployedEntryUI) = CreateUnemployedVillagerEntry(villagerIndex);

        BindSelectButton(availableEntryUI, villagerIndex);
        BindSelectButton(unemployedEntryUI, villagerIndex);

        availableVillagerEntries.Add(new AvailableVillagerUIEntry(
            availableEntryObject,
            availableEntryUI,
            unemployedEntryObject,
            unemployedEntryUI));

        AddToPartyInventory(villagerIndex);

        allInventory.SetActive(false);
        villagerManager.CancelSelection();
    }

    private void AddToPartyInventory(int villagerIndex)
    {
        VillagerData villagerInfo = GetVillagerDataFromHeldIndex(villagerIndex);
        if (villagerInfo == null)
            return;

        (GameObject allEntryObject, VillagerCardUI allEntryUI) = CreateVillagerCard(
            villagerPrefab,
            partyAllVillagerItems,
            partyAllVillagerRows,
            partyAllVillagers.transform,
            villagerInfo);

        TierInventoryGroup tierGroup = GetPartyTierGroup(villagerInfo.tier);
        if (tierGroup == null)
            return;

        (GameObject tierEntryObject, VillagerCardUI tierEntryUI) = CreateVillagerCard(
            villagerPrefab,
            tierGroup.Items,
            tierGroup.Rows,
            tierGroup.Container.transform,
            villagerInfo);

        partyVillagerEntries.Add(new PartyVillagerUIEntry(
            villagerIndex,
            allEntryObject,
            allEntryUI,
            tierEntryObject,
            tierEntryUI));

        BindSelectButton(allEntryUI, villagerIndex);
        BindSelectButton(tierEntryUI, villagerIndex);
    }

    public void RemoveFromAvailableInventory(int villagerIndex, int inVillageIndex, bool isEmployed)
    {
        if (!IsValidOwnedIndex(villagerIndex) || !IsValidAvailableIndex(inVillageIndex))
            return;

        ownedVillagerEntries[villagerIndex].AllEntryUI.SetAdded(false);
        ownedVillagerEntries[villagerIndex].TierEntryUI.SetAdded(false);

        AvailableVillagerUIEntry entry = availableVillagerEntries[inVillageIndex];

        if (!isEmployed && entry.UnemployedEntryObject != null)
        {
            unemployedItems.Remove(entry.UnemployedEntryObject);
            Destroy(entry.UnemployedEntryObject);
            ReorganizeItems(unemployedItems, unemployedRows, unemployedAvailableDisplay.transform);
        }

        allAvailableVillagers.RemoveAt(inVillageIndex);
        availableVillagerEntries.RemoveAt(inVillageIndex);
        Destroy(entry.AvailableEntryObject);

        ReorganizeItems(allAvailableVillagers, allAvailableVillagerRows, allAvailableDisplay.transform);

        RemoveFromPartyInventory(villagerIndex);
    }

    private void RemoveFromPartyInventory(int villagerIndex)
    {
        int partyIndex = FindPartyEntryIndexByVillagerIndex(villagerIndex);
        if (partyIndex == -1)
            return;

        PartyVillagerUIEntry entry = partyVillagerEntries[partyIndex];

        partyAllVillagerItems.Remove(entry.AllEntryObject);
        RemoveFromTierPartyList(entry.TierEntryObject);

        Destroy(entry.AllEntryObject);
        Destroy(entry.TierEntryObject);

        partyVillagerEntries.RemoveAt(partyIndex);

        ReorganizeItems(partyAllVillagerItems, partyAllVillagerRows, partyAllVillagers.transform);
        ReorganizeItems(partyTierOneVillagerItems, partyTierOneVillagerRows, partyTierOneVillagers.transform);
        ReorganizeItems(partyTierTwoVillagerItems, partyTierTwoVillagerRows, partyTierTwoVillagers.transform);
        ReorganizeItems(partyTierThreeVillagerItems, partyTierThreeVillagerRows, partyTierThreeVillagers.transform);
    }

    private int FindPartyEntryIndexByVillagerIndex(int villagerIndex)
    {
        VillagerData villagerInfo = GetVillagerDataFromHeldIndex(villagerIndex);
        if (villagerInfo == null)
            return -1;

        for (int i = 0; i < partyVillagerEntries.Count; i++)
        {
            VillagerCardUI ui = partyVillagerEntries[i].AllEntryUI;
            if (ui != null && partyAllVillagerItems.Count > i)
            {
                // party entries are added in same order as villagers are housed.
                // This is good enough for your current system.
            }
        }

        return villagerIndex < partyVillagerEntries.Count ? villagerIndex : -1;
    }

    private void RemoveFromTierPartyList(GameObject tierObject)
    {
        partyTierOneVillagerItems.Remove(tierObject);
        partyTierTwoVillagerItems.Remove(tierObject);
        partyTierThreeVillagerItems.Remove(tierObject);
    }

    private void UpdateAvailableInventory(int inVillageIndex, int prevInVillageIndex, int prevVillagerIndex, string buildingName)
    {
        if (prevVillagerIndex != -1 && IsValidAvailableIndex(prevInVillageIndex))
        {
            AvailableVillagerUIEntry previousEntry = availableVillagerEntries[prevInVillageIndex];
            previousEntry.AvailableEntryUI.SetWorkStatus("Unemployed", UnemployedColor);
            previousEntry.AvailableEntryUI.SetDescription("Currently Not Working");

            (GameObject unemployedObj, VillagerCardUI unemployedUI) = CreateUnemployedVillagerEntry(prevVillagerIndex);
            previousEntry.UnemployedEntryObject = unemployedObj;
            previousEntry.UnemployedEntryUI = unemployedUI;
        }

        if (!IsValidAvailableIndex(inVillageIndex))
            return;

        AvailableVillagerUIEntry currentEntry = availableVillagerEntries[inVillageIndex];
        currentEntry.AvailableEntryUI.SetWorkStatus("Employed", Color.white);
        currentEntry.AvailableEntryUI.SetDescription("Currently at " + buildingName);

        if (currentEntry.UnemployedEntryObject != null)
        {
            RemoveFromUnemployedInventory(inVillageIndex);
        }

        availableInventory.SetActive(false);
        villagerManager.CancelSelection();
    }

    public GameObject AddToUnemployedInventory(int villagerIndex)
    {
        return CreateUnemployedVillagerEntry(villagerIndex).CardObject;
    }

    public void AddToUnemployedInventoryNoReturn(int villagerIndex, int inVillageIndex)
    {
        (GameObject unemployedEntryObject, VillagerCardUI unemployedEntryUI) = CreateUnemployedVillagerEntry(villagerIndex);
        if (unemployedEntryObject == null || unemployedEntryUI == null)
            return;

        if (!IsValidAvailableIndex(inVillageIndex))
            return;

        AvailableVillagerUIEntry availableEntry = availableVillagerEntries[inVillageIndex];
        availableEntry.AvailableEntryUI.SetWorkStatus("Unemployed", UnemployedColor);
        availableEntry.AvailableEntryUI.SetDescription("Currently Not Working");

        availableEntry.UnemployedEntryObject = unemployedEntryObject;
        availableEntry.UnemployedEntryUI = unemployedEntryUI;
    }

    public void RemoveFromUnemployedInventory(int inVillageIndex)
    {
        if (!IsValidAvailableIndex(inVillageIndex))
            return;

        GameObject unemployedObj = availableVillagerEntries[inVillageIndex].UnemployedEntryObject;
        if (unemployedObj == null)
            return;

        unemployedItems.Remove(unemployedObj);
        Destroy(unemployedObj);
        availableVillagerEntries[inVillageIndex].UnemployedEntryObject = null;
        availableVillagerEntries[inVillageIndex].UnemployedEntryUI = null;

        ReorganizeItems(unemployedItems, unemployedRows, unemployedAvailableDisplay.transform);
    }

    private (GameObject CardObject, VillagerCardUI CardUI) CreateUnemployedVillagerEntry(int villagerIndex)
    {
        VillagerData villagerInfo = GetVillagerDataFromHeldIndex(villagerIndex);
        if (villagerInfo == null)
            return (null, null);

        (GameObject unemployedObject, VillagerCardUI unemployedUI) = CreateVillagerCard(
            availableVillagerPrefab,
            unemployedItems,
            unemployedRows,
            unemployedAvailableDisplay.transform,
            villagerInfo);

        BindSelectButton(unemployedUI, villagerIndex);
        return (unemployedObject, unemployedUI);
    }

    private (GameObject CardObject, VillagerCardUI CardUI) CreateVillagerCard(
        GameObject prefab,
        List<GameObject> items,
        List<GameObject> rows,
        Transform parent,
        VillagerData villagerInfo)
    {
        GameObject row = CreateRowIfNeeded(items, rows, parent);
        GameObject villagerCardObject = Instantiate(prefab, row.transform);
        VillagerCardUI villagerCardUI = villagerCardObject.GetComponent<VillagerCardUI>();

        if (villagerCardUI == null)
        {
            Debug.LogError($"VillagerCardUI missing on prefab: {prefab.name}");
            Destroy(villagerCardObject);
            return (null, null);
        }

        NormalizeUIItemTransform(villagerCardObject);

        items.Add(villagerCardObject);
        villagerCardUI.Setup(villagerInfo.Name, villagerInfo.villagerIcon);

        return (villagerCardObject, villagerCardUI);
    }

    private TierInventoryGroup GetTierGroup(int tier)
    {
        switch (tier)
        {
            case 1:
                return new TierInventoryGroup(tierOneVillagers, tierOneVillagerItems, tierOneVillagerRows);
            case 2:
                return new TierInventoryGroup(tierTwoVillagers, tierTwoVillagerItems, tierTwoVillagerRows);
            case 3:
                return new TierInventoryGroup(tierThreeVillagers, tierThreeVillagerItems, tierThreeVillagerRows);
            default:
                return null;
        }
    }

    private TierInventoryGroup GetPartyTierGroup(int tier)
    {
        switch (tier)
        {
            case 1:
                return new TierInventoryGroup(partyTierOneVillagers, partyTierOneVillagerItems, partyTierOneVillagerRows);
            case 2:
                return new TierInventoryGroup(partyTierTwoVillagers, partyTierTwoVillagerItems, partyTierTwoVillagerRows);
            case 3:
                return new TierInventoryGroup(partyTierThreeVillagers, partyTierThreeVillagerItems, partyTierThreeVillagerRows);
            default:
                return null;
        }
    }

    private VillagerData GetVillagerDataFromHeldIndex(int villagerIndex)
    {
        int villagerID = villagerManager.GetVillagerIDAt(villagerIndex);
        if (villagerID == -1)
            return null;

        return villagerData.GetVillagerDataByID(villagerID);
    }

    private GameObject CreateRowIfNeeded(List<GameObject> items, List<GameObject> rows, Transform parent)
    {
        if (items.Count % ItemsPerRow == 0)
        {
            GameObject newRow = Instantiate(villagerRow, parent);
            rows.Add(newRow);
            AdjustContainerSize(parent, rows.Count);
        }

        return rows[^1];
    }

    private void BindSelectButton(VillagerCardUI villagerCardUI, int villagerIndex)
    {
        if (villagerCardUI == null)
            return;

        villagerCardUI.BindButton(() =>
        {
            if (isPartySelectionMode)
            {
                onPartyVillagerSelected?.Invoke(villagerIndex);
                isPartySelectionMode = false;
                onPartyVillagerSelected = null;
                ClosePartyInventory();
                return;
            }

            villagerManager.AssignVillagerToBuilding(villagerIndex);
        });
    }

    private void ReorganizeItems(List<GameObject> items, List<GameObject> rows, Transform parentContainer)
    {
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] != null)
                items[i].transform.SetParent(null);
        }

        foreach (GameObject row in rows)
        {
            if (row != null)
                Destroy(row);
        }

        rows.Clear();

        for (int i = 0; i < items.Count; i++)
        {
            if (i % ItemsPerRow == 0)
            {
                GameObject newRow = Instantiate(villagerRow, parentContainer);
                rows.Add(newRow);
            }

            items[i].transform.SetParent(rows[^1].transform, false);
            NormalizeUIItemTransform(items[i]);
        }

        AdjustContainerSize(parentContainer, rows.Count);
    }

    private void CacheBaseHeight(GameObject container)
    {
        if (container == null)
            return;

        RectTransform rect = container.GetComponent<RectTransform>();
        if (rect == null || baseContainerHeights.ContainsKey(rect))
            return;

        baseContainerHeights.Add(rect, rect.sizeDelta.y);
    }

    private void AdjustContainerSize(Transform container, int rowCount)
    {
        RectTransform containerRect = container.GetComponent<RectTransform>();
        if (containerRect == null)
            return;

        if (!baseContainerHeights.TryGetValue(containerRect, out float baseHeight))
        {
            baseHeight = containerRect.sizeDelta.y;
            baseContainerHeights[containerRect] = baseHeight;
        }

        float extraHeight = Mathf.Max(0, rowCount - 2) * ExtraRowHeight;
        containerRect.sizeDelta = new Vector2(containerRect.sizeDelta.x, baseHeight + extraHeight);
    }

    private bool IsValidOwnedIndex(int index)
    {
        return index >= 0 &&
               index < ownedVillagerEntries.Count &&
               villagerManager.HasVillagerIDAt(index);
    }

    private bool IsValidAvailableIndex(int index)
    {
        return index >= 0 &&
               index < availableVillagerEntries.Count &&
               index < allAvailableVillagers.Count;
    }

    private void ShowTierTab(GameObject activeDisplay, GameObject activeSelected, int itemCount)
    {
        allDisplay.SetActive(activeDisplay == allDisplay);
        tierOneDisplay.SetActive(activeDisplay == tierOneDisplay);
        tierTwoDisplay.SetActive(activeDisplay == tierTwoDisplay);
        tierThreeDisplay.SetActive(activeDisplay == tierThreeDisplay);

        allSelected.SetActive(activeSelected == allSelected);
        oneSelected.SetActive(activeSelected == oneSelected);
        twoSelected.SetActive(activeSelected == twoSelected);
        threeSelected.SetActive(activeSelected == threeSelected);

        noVillagers.SetActive(itemCount == 0);
    }

    private void ShowAvailableTab(bool showAll, int itemCount)
    {
        allAvailableDisplay.SetActive(showAll);
        unemployedAvailableDisplay.SetActive(!showAll);

        allAvailableSelected.SetActive(showAll);
        unemployedSelected.SetActive(!showAll);
        unemployedScroller.SetActive(!showAll);

        noVillagersTwo.SetActive(itemCount == 0);
    }

    private void ShowPartyTierTab(GameObject activeDisplay, GameObject activeSelected, int itemCount)
    {
        partyAllDisplay.SetActive(activeDisplay == partyAllDisplay);
        partyTierOneDisplay.SetActive(activeDisplay == partyTierOneDisplay);
        partyTierTwoDisplay.SetActive(activeDisplay == partyTierTwoDisplay);
        partyTierThreeDisplay.SetActive(activeDisplay == partyTierThreeDisplay);

        partyAllSelected.SetActive(activeSelected == partyAllSelected);
        partyOneSelected.SetActive(activeSelected == partyOneSelected);
        partyTwoSelected.SetActive(activeSelected == partyTwoSelected);
        partyThreeSelected.SetActive(activeSelected == partyThreeSelected);

        partyNoVillagers.SetActive(itemCount == 0);
    }

    public void ShowTierAll()
    {
        ShowTierTab(allDisplay, allSelected, allVillagerItems.Count);
    }

    public void ShowTierOne()
    {
        ShowTierTab(tierOneDisplay, oneSelected, tierOneVillagerItems.Count);
    }

    public void ShowTierTwo()
    {
        ShowTierTab(tierTwoDisplay, twoSelected, tierTwoVillagerItems.Count);
    }

    public void ShowTierThree()
    {
        ShowTierTab(tierThreeDisplay, threeSelected, tierThreeVillagerItems.Count);
    }

    public void ShowPartyTierAll()
    {
        ShowPartyTierTab(partyAllDisplay, partyAllSelected, partyAllVillagerItems.Count);
    }

    public void ShowPartyTierOne()
    {
        ShowPartyTierTab(partyTierOneDisplay, partyOneSelected, partyTierOneVillagerItems.Count);
    }

    public void ShowPartyTierTwo()
    {
        ShowPartyTierTab(partyTierTwoDisplay, partyTwoSelected, partyTierTwoVillagerItems.Count);
    }

    public void ShowPartyTierThree()
    {
        ShowPartyTierTab(partyTierThreeDisplay, partyThreeSelected, partyTierThreeVillagerItems.Count);
    }

    public void OpenAllInventory()
    {
        allInventory.SetActive(true);
        noVillagers.SetActive(allVillagerItems.Count == 0);
    }

    public void CloseAllInventory()
    {
        allInventory.SetActive(false);
    }

    public void OpenAvailableInventory()
    {
        availableInventory.SetActive(true);
        noVillagersTwo.SetActive(allAvailableVillagers.Count == 0);
    }

    public void CloseAvailableInventory()
    {
        availableInventory.SetActive(false);
    }

    public void OpenPartyInventory()
    {
        partyInventory.SetActive(true);
        ShowPartyTierAll();
    }

    public void ClosePartyInventory()
    {
        partyInventory.SetActive(false);
        isPartySelectionMode = false;
        onPartyVillagerSelected = null;
    }

    public void OpenPartySelection(System.Action<int> onSelected)
    {
        isPartySelectionMode = true;
        onPartyVillagerSelected = onSelected;
        OpenPartyInventory();
    }

    public void openAllInventory()
    {
        OpenAllInventory();
    }

    public void exitAllInventory()
    {
        villagerManager.CloseAllInventorySelection();
    }

    public void openAvailableInventory()
    {
        OpenAvailableInventory();
    }

    public void exitAvailableInventory()
    {
        villagerManager.CloseAvailableInventorySelection();
    }

    public void exitPartyInventory()
    {
        ClosePartyInventory();
    }

    public void showAllAvailable()
    {
        ShowAvailableTab(true, allAvailableVillagers.Count);
    }

    public void showUnemployedAvailable()
    {
        ShowAvailableTab(false, unemployedItems.Count);
    }

    private void NormalizeUIItemTransform(GameObject item)
    {
        RectTransform rect = item.GetComponent<RectTransform>();
        if (rect == null)
            return;

        rect.localScale = VillagerCardScale;
        rect.localRotation = Quaternion.identity;
        rect.anchoredPosition3D = Vector3.zero;
    }

    public void SetDisabledVillagers(HashSet<int> disabledVillagers)
    {
        foreach (PartyVillagerUIEntry entry in partyVillagerEntries)
        {
            bool disabled = disabledVillagers.Contains(entry.VillagerIndex);

            if (entry.AllEntryUI != null)
                entry.AllEntryUI.SetInteractable(!disabled);

            if (entry.TierEntryUI != null)
                entry.TierEntryUI.SetInteractable(!disabled);
        }
    }
}