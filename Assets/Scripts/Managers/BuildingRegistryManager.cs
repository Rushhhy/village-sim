using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BuildingRegistryManager : MonoBehaviour
{
    [SerializeField] private GridData gridData;
    [SerializeField] private Grid grid;

    public List<GameObject> buildingRegistryList = new();
    private readonly List<GameObject> buildingOverviewList = new();
    private readonly List<GameObject> buildingToggleList = new();
    private readonly List<GameObject> decorationToggleList = new();

    private readonly Dictionary<int, GameObject> buildingRegistryByIndex = new();
    private readonly Dictionary<int, GameObject> buildingOverviewByIndex = new();

    [SerializeField] private PlacementSystem placementManager;
    [SerializeField] private VillagerManager villagerManager;
    [SerializeField] private InputManager inputManager;

    [SerializeField] private Transform uiRoot;

    [SerializeField] private GameObject buildingRegistryElement;
    [SerializeField] private RectTransform buildingRegistryColumn;

    [SerializeField]
    private Sprite alchemistOne, alchemistTwo, alchemistThree, armoryOne, armoryTwo, armoryThree, bakeryOne, bakeryTwo, bakeryThree, barnOne, barnTwo, barnThree, barracksOne,
        barracksTwo, barracksThree, bathHouseOne, bathHouseTwo, blacksmithOne, blacksmithTwo, blacksmithThree, carpenterOne, carpenterTwo, carpenterThree, farmOne, farmTwo,
        farmThree, hospitalOne, hospitalTwo, hospitalThree, houseOne, houseTwo, houseThree, loggerOne, loggerTwo, loggerThree, lumberMillOne, lumberMillTwo, lumberMillThree,
        mine, mill, schoolOne, schoolTwo, schoolThree, stablesOne, stablesTwo, refinery, tailorOne, tailorTwo, tailorThree, warehouseOne, warehouseTwo, warehouseThree, workshopOne,
        workshopTwo, workshopThree, orchardOne, orchardTwo, orchardThree, mineOne, marketOne, refineryOne;

    [SerializeField] private ResourceSO resources;
    [SerializeField] private StructuresDatabaseSO structures;

    [SerializeField] private GameObject buildingOverviewScreen;
    [SerializeField] private GameObject buildingOverviewScreenTwo;
    [SerializeField] private GameObject marketOverviewScreen;
    [SerializeField] private GameObject warehouseOverviewScreen;

    [SerializeField] private GameObject buildingToggleUI;
    [SerializeField] private GameObject fixedBuildingToggleUI;
    [SerializeField] private GameObject decorationToggleUI;
    [SerializeField] private Transform buildingToggleParent;

    private int openToggleIndex = -1;
    private int openToggleType = -1;

    private readonly Vector3 scale = new(0.12f, 0.12f, 1f);

    public event Action<Vector3> buildingSelected;

    [SerializeField] private Sprite lockedFrameSprite;

    [SerializeField] private Sprite addVillagerIcon;

    private void Start()
    {
        placementManager.OnStructureBuilt += OnBuilt;
        placementManager.OnStructureRemoved += OnRemoved;
        placementManager.OnMoved += OnBuildingMoved;
        inputManager.OnMouseTapped += OnTapped;
        villagerManager.OnVillagerAssigned += ChangeOverviewVillagerSlot;
        villagerManager.OnVillagerTotallyRemoved += ClearBuildingSlot;
    }

    private void Update()
    {
        for (int i = 0; i < placementManager.placedGameObjects.Count; i++)
        {
            if (placementManager.placedGameObjects[i] != null &&
                placementManager.placedGameObjects[i].GetComponent<Market>() != null)
            {
                UpdateMarketRegistryUI(i);
            }
        }
    }

    private void OnDestroy()
    {
        if (placementManager != null)
        {
            placementManager.OnStructureBuilt -= OnBuilt;
            placementManager.OnStructureRemoved -= OnRemoved;
            placementManager.OnMoved -= OnBuildingMoved;
        }

        if (inputManager != null)
        {
            inputManager.OnMouseTapped -= OnTapped;
        }

        if (villagerManager != null)
        {
            villagerManager.OnVillagerAssigned -= ChangeOverviewVillagerSlot;
            villagerManager.OnVillagerTotallyRemoved -= ClearBuildingSlot;
        }
    }

    private void ClearBuildingSlot(Villager villager, bool isHouse)
    {
        int buildingIndex = isHouse ? villager.assignedHouseIndex : villager.assignedBuildingIndex;
        int buildingSlot = isHouse ? villager.assignedHouseSlot : villager.assignedBuildingSlot;

        if (buildingIndex == -1)
        {
            return;
        }

        if (!TryGetWorkerSlotObjects(buildingIndex, buildingSlot, out GameObject overviewSlotObj, out GameObject registrySlotObj))
        {
            Debug.LogWarning("Invalid villager slot: " + buildingSlot);
            return;
        }

        ResetSlotToDefault(overviewSlotObj);
        ResetSlotToDefault(registrySlotObj);
    }

    public void ActivateBuildingOverview(int structureIndex)
    {
        if (buildingOverviewByIndex.TryGetValue(structureIndex, out GameObject overview))
        {
            RefreshSpecialOverviewUI(structureIndex, overview);
            overview.SetActive(true);
        }
    }

    public void ActivateBuildingOverviewThroughRegistry(int structureIndex)
    {
        if (buildingOverviewByIndex.TryGetValue(structureIndex, out GameObject overview))
        {
            RefreshSpecialOverviewUI(structureIndex, overview);
            overview.SetActive(true);
        }
    }

    public void CloseBuildingOverview(int buildingIndex)
    {
        if (buildingOverviewByIndex.TryGetValue(buildingIndex, out GameObject overview))
        {
            overview.SetActive(false);
        }
    }

    private void OnBuilt(int structureID)
    {
        if (IsDecorationOrRoad(structureID))
        {
            HandleDecorationBuilt(structureID);
            return;
        }

        GameObject registryEntry = Instantiate(buildingRegistryElement, buildingRegistryColumn);
        registryEntry.SetActive(false);
        buildingRegistryList.Add(registryEntry);
        AssignRegistryButton(registryEntry);

        TextMeshProUGUI text = registryEntry.transform.Find("Image/BuildingName").GetComponent<TextMeshProUGUI>();
        Image image = registryEntry.transform.Find("Image/BuildingImage").GetComponent<Image>();

        GameObject building = placementManager.placedGameObjects[placementManager.placedGameObjects.Count - 1];
        Building buildingData = building.GetComponent<Building>();

        int buildingIndex = placementManager.placedGameObjects.Count - 1;
        buildingData.Index = buildingIndex;
        buildingData.Level = IsFixedBuilding(structureID) ? 0 : 1;

        buildingRegistryByIndex[buildingIndex] = registryEntry;

        int productionType = -1;
        if (buildingData is ProductionBuilding productionBuilding)
        {
            productionType = productionBuilding.ProductionType;
        }

        ConfigureBuiltStructure(
            structureID,
            registryEntry,
            text,
            image,
            building,
            buildingData,
            buildingIndex,
            productionType
        );

        CreateBuildingToggle(structureID);
    }

    private void HandleDecorationBuilt(int structureID)
    {
        int decorationWidth = structures.objectsData[structureID].Size.x;
        Vector3 decorationPosition = placementManager.placedDecorations[placementManager.placedDecorations.Count - 1].transform.position;
        Vector3Int gridPosition = Vector3Int.RoundToInt(decorationPosition);

        Vector3 togglePosition = GetTogglePosition(gridPosition, decorationWidth);
        GameObject decorationToggleButtons = Instantiate(decorationToggleUI, togglePosition, Quaternion.identity, buildingToggleParent);
        decorationToggleList.Add(decorationToggleButtons);

        Transform editButtonTransform = decorationToggleButtons.transform.Find("EditButton");
        Button editButton = editButtonTransform != null ? editButtonTransform.GetComponent<Button>() : null;
        if (editButton != null)
        {
            editButton.onClick.AddListener(() => placementManager.SelectStructure(gridPosition));
            editButton.onClick.AddListener(TurnOffBuildingToggleUI);
        }
    }

    private void AssignVillagerSlotButtons(GameObject buildingOverview, bool isHouse)
    {
        int buildingIndex = placementManager.placedGameObjects.Count - 1;

        AssignSlotButton(buildingOverview, "Workers/WorkerOneFrame/Worker", () => villagerManager.selectBuildingSlot(buildingIndex, 1, isHouse));
        AssignSlotButton(buildingOverview, "Workers/WorkerTwoFrame/Worker", () => villagerManager.selectBuildingSlot(buildingIndex, 2, isHouse));
        AssignSlotButton(buildingOverview, "Workers/WorkerThreeFrame/Worker", () => villagerManager.selectBuildingSlot(buildingIndex, 3, isHouse));

        GameObject buildingRegistryEntry = buildingRegistryByIndex[buildingIndex];
        AssignSlotButton(buildingRegistryEntry, "Image/Workers/WorkerOne/WorkerButton", () => villagerManager.selectBuildingSlot(buildingIndex, 1, isHouse));
        AssignSlotButton(buildingRegistryEntry, "Image/Workers/WorkerTwo/WorkerButton", () => villagerManager.selectBuildingSlot(buildingIndex, 2, isHouse));
        AssignSlotButton(buildingRegistryEntry, "Image/Workers/WorkerThree/WorkerButton", () => villagerManager.selectBuildingSlot(buildingIndex, 3, isHouse));
    }

    private void AssignSlotButton(GameObject root, string path, UnityEngine.Events.UnityAction action)
    {
        GameObject target = root.transform.Find(path)?.gameObject;
        if (target == null)
        {
            return;
        }

        Button button = target.GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(action);
        }
    }

    public void TurnOffBuildingToggleUI()
    {
        SetAllActive(buildingToggleList, false);
        SetAllActive(decorationToggleList, false);
        openToggleIndex = -1;
        openToggleType = -1;
    }

    public void ChangeBuildingToggleUIPlacement(int buildingIndex, Vector2Int buildingSize, Vector3Int gridPosition, int buildingType)
    {
        GameObject toggleButtons = buildingType == 0 ? buildingToggleList[buildingIndex] : decorationToggleList[buildingIndex];
        toggleButtons.transform.position = GetTogglePosition(gridPosition, buildingSize.x);
    }

    private void InitializeProductionBuildingOverview(GameObject buildingOverview, string buildingName, int productionType, ProductionBuilding buildingData)
    {
        ChangeBuildingOverviewName(buildingOverview, buildingName);
        buildingData.InitializeProduction();

        Transform productionTypeObject = buildingOverview.transform.Find(productionType == 3 ? "1" : productionType.ToString());
        if (productionTypeObject != null)
        {
            productionTypeObject.gameObject.SetActive(true);
        }

        switch (productionType)
        {
            case 0:
                SetOutputOnlyUI(
                    buildingOverview,
                    "0/OutputFrame/Image",
                    "0/OutputFrame/OutputAmount",
                    buildingData.ProducedResourcesID[0],
                    buildingData.ProducedResourcesAmount[0]);
                break;

            case 1:
                SetInputOutputUI(
                    buildingOverview,
                    "1/InputFrame/Image",
                    "1/InputFrame/InputAmount",
                    "1/OutputFrame/Image",
                    "1/OutputFrame/OutputAmount",
                    buildingData.NeededResourcesID[0],
                    buildingData.NeededResourcesAmount[0],
                    buildingData.ProducedResourcesID[0],
                    buildingData.ProducedResourcesAmount[0]);
                break;

            case 2:
                SetTwoInputOneOutputUI(
                    buildingOverview,
                    buildingData.NeededResourcesID[0],
                    buildingData.NeededResourcesAmount[0],
                    buildingData.NeededResourcesID[1],
                    buildingData.NeededResourcesAmount[1],
                    buildingData.ProducedResourcesID[0],
                    buildingData.ProducedResourcesAmount[0]);
                break;

            case 3:
                SetDualProductionUI(buildingOverview, buildingData);
                Transform dualProductionToggleButtons = buildingOverview.transform.Find("ToggleProductionButtons");
                if (dualProductionToggleButtons != null)
                {
                    dualProductionToggleButtons.gameObject.SetActive(true);
                }
                break;

            case 4:
                SetTwoOutputOnlyUI(
                    buildingOverview,
                    buildingData.ProducedResourcesID[0],
                    buildingData.ProducedResourcesAmount[0],
                    buildingData.ProducedResourcesID[1],
                    buildingData.ProducedResourcesAmount[1]);
                break;
        }

        int buildingIndex = placementManager.placedGameObjects.Count - 1;

        InitializeProductionProgressBar(buildingOverview, "UpgradeTime/ProgressBar", buildingData, buildingIndex, 0);

        if (productionType == 3)
        {
            InitializeProductionProgressBar(buildingOverview, "UpgradeTimeTwo/ProgressBar", buildingData, buildingIndex, 1);

            Button speedUpgradeButtonTwo = buildingOverview.transform.Find("UpgradeTimeTwo/SpeedButtonCanBuy")?.GetComponent<Button>();
            if (speedUpgradeButtonTwo != null)
            {
                speedUpgradeButtonTwo.onClick.AddListener(() => UpgradeProductionSpeedForBuildingWithIndex(buildingIndex, 1));
            }
        }

        Button speedUpgradeButton = buildingOverview.transform.Find("UpgradeTime/SpeedButtonCanBuy")?.GetComponent<Button>();
        if (speedUpgradeButton != null)
        {
            speedUpgradeButton.onClick.AddListener(() => UpgradeProductionSpeedForBuildingWithIndex(buildingIndex, 0));
        }

        InitializeUpgradeButtonForProductionBuilding(buildingOverview, buildingData, buildingIndex);
    }

    private void InitializeProductionProgressBar(GameObject buildingOverview, string path, ProductionBuilding buildingData, int buildingIndex, int resourceIndex)
    {
        Transform progressBarTransform = buildingOverview.transform.Find(path);
        if (progressBarTransform == null)
        {
            return;
        }

        ProductionProgressBar progressBar = progressBarTransform.GetComponent<ProductionProgressBar>();
        if (progressBar == null)
        {
            return;
        }

        progressBar.structureIndex = buildingIndex;
        progressBar.Initialize(buildingData, resourceIndex);
        progressBar.SetProductionTime(resourceIndex);
    }

    private void SetOutputOnlyUI(GameObject root, string outputIconPath, string outputAmountPath, int outputResourceId, int outputAmount)
    {
        SetResourceIconAndAmount(root, outputIconPath, outputAmountPath, outputResourceId, outputAmount);
    }

    private void SetInputOutputUI(
        GameObject root,
        string inputIconPath,
        string inputAmountPath,
        string outputIconPath,
        string outputAmountPath,
        int inputResourceId,
        int inputAmount,
        int outputResourceId,
        int outputAmount)
    {
        SetResourceIconAndAmount(root, inputIconPath, inputAmountPath, inputResourceId, inputAmount);
        SetResourceIconAndAmount(root, outputIconPath, outputAmountPath, outputResourceId, outputAmount);
    }

    private void SetTwoInputOneOutputUI(
        GameObject root,
        int inputResourceIdOne,
        int inputAmountOne,
        int inputResourceIdTwo,
        int inputAmountTwo,
        int outputResourceId,
        int outputAmount)
    {
        SetResourceIconAndAmount(root, "2/InputFrameOne/Image", "2/InputFrameOne/InputAmount", inputResourceIdOne, inputAmountOne);
        SetResourceIconAndAmount(root, "2/InputFrameTwo/Image", "2/InputFrameTwo/InputAmount", inputResourceIdTwo, inputAmountTwo);
        SetResourceIconAndAmount(root, "2/OutputFrame/Image", "2/OutputFrame/OutputAmount", outputResourceId, outputAmount);
    }

    private void SetDualProductionUI(GameObject root, ProductionBuilding buildingData)
    {
        bool hasInput = buildingData.NeededResourcesID.Length > 0 &&
                        buildingData.NeededResourcesAmount.Length > 0;

        Transform inputOne = root.transform.Find("1/InputFrame");
        Transform inputTwo = root.transform.Find("1 (Two)/InputFrame");

        if (hasInput)
        {
            int inputResourceId = buildingData.NeededResourcesID[0];
            int inputAmount = buildingData.NeededResourcesAmount[0];

            SetResourceIconAndAmount(root, "1/InputFrame/Image", "1/InputFrame/InputAmount", inputResourceId, inputAmount);
            SetResourceIconAndAmount(root, "1 (Two)/InputFrame/Image", "1 (Two)/InputFrame/InputAmount", inputResourceId, inputAmount);

            if (inputOne != null) inputOne.gameObject.SetActive(true);
            if (inputTwo != null) inputTwo.gameObject.SetActive(true);
        }
        else
        {
            SetLockedInputFrame(root, "1/InputFrame");
            SetLockedInputFrame(root, "1 (Two)/InputFrame");
        }

        SetResourceIconAndAmount(
            root,
            "1/OutputFrame/Image",
            "1/OutputFrame/OutputAmount",
            buildingData.ProducedResourcesID[0],
            buildingData.ProducedResourcesAmount[0]
        );

        SetResourceIconAndAmount(
            root,
            "1 (Two)/OutputFrame/Image",
            "1 (Two)/OutputFrame/OutputAmount",
            buildingData.ProducedResourcesID[1],
            buildingData.ProducedResourcesAmount[1]
        );
    }

    private void SetResourceIconAndAmount(GameObject root, string iconPath, string amountPath, int resourceId, int amount)
    {
        Transform iconTransform = root.transform.Find(iconPath);
        Transform amountTransform = root.transform.Find(amountPath);

        if (amountTransform != null)
        {
            TextMeshProUGUI amountText = amountTransform.GetComponent<TextMeshProUGUI>();
            if (amountText != null)
            {
                amountText.text = amount + "x";
            }
        }

        if (iconTransform != null)
        {
            Image iconImage = iconTransform.GetComponent<Image>();
            if (iconImage != null)
            {
                iconImage.sprite = resources.resourcesData[resourceId].Icon;
                iconImage.SetNativeSize();
                SetResourceScale(resourceId, iconImage);
            }
        }
    }

    private static void SetResourceScale(int resourceID, Image resourceIcon)
    {
        if (resourceID == 11)
        {
            resourceIcon.transform.localScale = new Vector3(0.75f, 0.75f, 1f);
        }
        else if (resourceID == 0 || resourceID == 5 || resourceID == 2)
        {
            resourceIcon.transform.localScale = Vector3.one;
        }
        else
        {
            resourceIcon.transform.localScale = new Vector3(1.2f, 1.2f, 1f);
        }
    }

    public void UpdateProductionMethodsUI(ProductionBuilding buildingData)
    {
        int buildingIndex = buildingData.Index;
        GameObject buildingOverview = buildingOverviewByIndex[buildingIndex];
        int productionType = buildingData.ProductionType;

        if (productionType == 0)
        {
            buildingOverview.transform.Find("0/OutputFrame/OutputAmount").GetComponent<TextMeshProUGUI>().text =
                buildingData.ProducedResourcesAmount[0] + "x";
        }
        else if (productionType == 1)
        {
            buildingOverview.transform.Find("1/InputFrame/InputAmount").GetComponent<TextMeshProUGUI>().text =
                buildingData.NeededResourcesAmount[0] + "x";
            buildingOverview.transform.Find("1/OutputFrame/OutputAmount").GetComponent<TextMeshProUGUI>().text =
                buildingData.ProducedResourcesAmount[0] + "x";
        }
        else if (productionType == 2)
        {
            buildingOverview.transform.Find("2/InputFrameOne/InputAmount").GetComponent<TextMeshProUGUI>().text =
                buildingData.NeededResourcesAmount[0] + "x";
            buildingOverview.transform.Find("2/InputFrameTwo/InputAmount").GetComponent<TextMeshProUGUI>().text =
                buildingData.NeededResourcesAmount[1] + "x";
            buildingOverview.transform.Find("2/OutputFrame/OutputAmount").GetComponent<TextMeshProUGUI>().text =
                buildingData.ProducedResourcesAmount[0] + "x";
        }
        else if (productionType == 3)
        {
            bool hasInput = buildingData.NeededResourcesID != null &&
                            buildingData.NeededResourcesID.Length > 0 &&
                            buildingData.NeededResourcesAmount != null &&
                            buildingData.NeededResourcesAmount.Length > 0;

            if (hasInput)
            {
                buildingOverview.transform.Find("1/InputFrame/InputAmount").GetComponent<TextMeshProUGUI>().text =
                    buildingData.NeededResourcesAmount[0] + "x";

                buildingOverview.transform.Find("1 (Two)/InputFrame/InputAmount").GetComponent<TextMeshProUGUI>().text =
                    buildingData.NeededResourcesAmount[0] + "x";
            }

            buildingOverview.transform.Find("1/OutputFrame/OutputAmount").GetComponent<TextMeshProUGUI>().text =
                buildingData.ProducedResourcesAmount[0] + "x";

            buildingOverview.transform.Find("1 (Two)/OutputFrame/OutputAmount").GetComponent<TextMeshProUGUI>().text =
                buildingData.ProducedResourcesAmount[1] + "x";
        }
        else if (productionType == 4)
        {
            buildingOverview.transform.Find("4/OutputFrameOne/OutputAmount").GetComponent<TextMeshProUGUI>().text =
                buildingData.ProducedResourcesAmount[0] + "x";
            buildingOverview.transform.Find("4/OutputFrameTwo/OutputAmount").GetComponent<TextMeshProUGUI>().text =
                buildingData.ProducedResourcesAmount[1] + "x";
        }
    }

    private static void ChangeBuildingOverviewName(GameObject buildingOverview, string buildingName)
    {
        Transform buildingNameObject = buildingOverview.transform.Find("BuildingName");
        TextMeshProUGUI buildingNameText = buildingNameObject.GetComponent<TextMeshProUGUI>();
        buildingNameText.text = buildingName;
    }

    private void InitializeUpgradeButtonForProductionBuilding(GameObject buildingOverview, Building buildingData, int buildingIndex)
    {
        GameObject upgradeButtonObject = buildingOverview.transform.Find("Upgrade/ButtonCanBuy").gameObject;
        Button upgradeButton = upgradeButtonObject.GetComponent<Button>();
        upgradeButton.onClick.AddListener(() => UpgradeProductionBuildingWithIndex(buildingIndex));
        UpdateBuildingOverviewUpgradeButton(buildingData, buildingOverview);
    }

    private void InitializeUpgradeButtonForBuilding(GameObject buildingOverview, Building buildingData, int buildingIndex, int structureID)
    {
        GameObject upgradeButtonObject = buildingOverview.transform.Find("Upgrade/ButtonCanBuy").gameObject;
        Button upgradeButton = upgradeButtonObject.GetComponent<Button>();
        upgradeButton.onClick.AddListener(() => UpgradeBuildingWithIndex(buildingIndex, structureID));
        UpdateBuildingOverviewUpgradeButton(buildingData, buildingOverview);
    }

    public void UpgradeBuildingWithIndex(int buildingIndex, int structureID)
    {
        buildingOverviewByIndex[buildingIndex].SetActive(false);

        GameObject building = placementManager.placedGameObjects[buildingIndex];
        Building buildingData = building.GetComponent<Building>();

        if (buildingIndex == 0)
        {
            buildingData.constructionDurationsPerLevel[0] = 0;
        }

        buildingData.UpgradeBuilding();

        GameObject buildingOverview = buildingOverviewByIndex[buildingIndex];
        GameObject buildingRegistry = buildingRegistryByIndex[buildingIndex];

        SyncBuildingNameUI(buildingOverview, buildingRegistry, buildingData.BuildingName);

        Image buildingRegistryImage = buildingRegistry.transform.Find("Image/BuildingImage").GetComponent<Image>();
        UpdateRegistrySpriteForBuildingLevel(buildingRegistry, buildingOverview, buildingRegistryImage, buildingData, structureID);

        buildingRegistryImage.SetNativeSize();

        if ((buildingIndex == 0 || buildingIndex == 1) && buildingData.Level > 1)
        {
            buildingRegistryImage.rectTransform.localScale = new Vector3(0.1f, 0.1f, 1f);
        }

        UpdateBuildingOverviewUpgradeButton(buildingData, buildingOverview);
        RefreshSpecialOverviewUI(buildingIndex, buildingOverview);
    }

    public void UpgradeProductionBuildingWithIndex(int index)
    {
        buildingOverviewByIndex[index].SetActive(false);

        GameObject building = placementManager.placedGameObjects[index];
        ProductionBuilding buildingData = building.GetComponent<ProductionBuilding>();

        buildingData.UpgradeBuilding();

        GameObject buildingOverview = buildingOverviewByIndex[index];
        GameObject buildingRegistry = buildingRegistryByIndex[index];

        SyncBuildingNameUI(buildingOverview, buildingRegistry, buildingData.BuildingName);

        Image buildingRegistryImage = buildingRegistry.transform.Find("Image/BuildingImage").GetComponent<Image>();
        UpdateRegistrySpriteForProductionBuilding(buildingRegistry, buildingOverview, buildingRegistryImage, buildingData);
        buildingRegistryImage.SetNativeSize();

        UpdateProductionAmountsAfterUpgrade(buildingOverview, buildingData);
        ResetProductionProgressBars(buildingOverview, buildingData);
        UpdateBuildingOverviewUpgradeButton(buildingData, buildingOverview);
    }

    private void SetUpBuildingOverview(GameObject buildingOverview, int buildingIndex)
    {
        buildingOverview.transform.SetParent(uiRoot, false);
        RectTransform buildingOverviewRectTransform = buildingOverview.GetComponent<RectTransform>();
        buildingOverviewRectTransform.anchoredPosition = Vector2.zero;

        buildingOverviewList.Add(buildingOverview);
        buildingOverviewByIndex[buildingIndex] = buildingOverview;

        AssignExitButtonFunctionality(buildingOverview, buildingIndex);
    }

    private void AssignRegistryButton(GameObject registryEntry)
    {
        Transform registryButtonObject = registryEntry.transform.Find("Image");
        Button registryButton = registryButtonObject.GetComponent<Button>();
        int structureIndex = buildingRegistryList.Count - 1;
        registryButton.onClick.AddListener(() => ActivateBuildingOverviewThroughRegistry(structureIndex));
    }

    private void AssignExitButtonFunctionality(GameObject buildingOverview, int buildingIndex)
    {
        Transform exitButtonObject = buildingOverview.transform.Find("Exit");
        if (exitButtonObject == null)
        {
            Debug.LogError("Exit button object not found!");
            return;
        }

        Button exitButton = exitButtonObject.GetComponent<Button>();
        if (exitButton == null)
        {
            Debug.LogError("No Button component found on the prefab!");
            return;
        }

        exitButton.onClick.AddListener(() => CloseBuildingOverview(buildingIndex));
    }

    private static void EditRegistryEntry(TextMeshProUGUI text, Image image, Sprite buildingSprite, Vector3 imageScale, string buildingName)
    {
        image.sprite = buildingSprite;
        image.SetNativeSize();
        image.transform.localScale = imageScale;
        text.text = buildingName;
    }

    private void OnRemoved(int index, int type)
    {
        if (type == 1)
        {
            GameObject decorationToggleButtons = decorationToggleList[index];
            decorationToggleList.RemoveAt(index);
            Destroy(decorationToggleButtons);
            return;
        }

        GameObject registryEntry = buildingRegistryByIndex.ContainsKey(index) ? buildingRegistryByIndex[index] : null;
        GameObject buildingOverview = buildingOverviewByIndex.ContainsKey(index) ? buildingOverviewByIndex[index] : null;
        GameObject buildingToggleButtons = buildingToggleList[index];

        if (registryEntry != null)
        {
            buildingRegistryList.Remove(registryEntry);
            Destroy(registryEntry);
        }

        if (buildingOverview != null)
        {
            buildingOverviewList.Remove(buildingOverview);
            Destroy(buildingOverview);
        }

        buildingRegistryByIndex.Remove(index);
        buildingOverviewByIndex.Remove(index);

        buildingToggleList.RemoveAt(index);
        Destroy(buildingToggleButtons);

        openToggleIndex = -1;
        openToggleType = -1;
    }

    private void UpdateBuildingOverviewUpgradeButton(Building buildingData, GameObject buildingOverview)
    {
        Transform upgradeButtonObject = buildingOverview.transform.Find("Upgrade/Button");
        Transform upgradeButtonCanBuyObject = buildingOverview.transform.Find("Upgrade/ButtonCanBuy");

        TextMeshProUGUI upgradeButtonText = buildingOverview.transform.Find("Upgrade/Button/ButtonText").GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI upgradeButtonCanBuyText = buildingOverview.transform.Find("Upgrade/ButtonCanBuy/ButtonCanBuyText").GetComponent<TextMeshProUGUI>();

        if (buildingData.Level == 0)
        {
            upgradeButtonText.text = "Build";
            upgradeButtonCanBuyText.text = "Build";
            isUpgradeAvailable();
        }
        else if (buildingData.Level == 1)
        {
            upgradeButtonText.text = "Upgrade to Level 2";
            upgradeButtonCanBuyText.text = "Upgrade to Level 2";
            isUpgradeAvailable();
        }
        else if (buildingData.Level == 2)
        {
            upgradeButtonText.text = "Upgrade to Level 3";
            upgradeButtonCanBuyText.text = "Upgrade to Level 3";
            isUpgradeAvailable();
        }
        else if (buildingData.Level == 3)
        {
            upgradeButtonText.text = "MAXED OUT";
            upgradeButtonCanBuyText.text = "MAXED OUT";
            upgradeButtonCanBuyObject.gameObject.SetActive(false);
        }
    }

    private bool isUpgradeAvailable()
    {
        return true;
    }

    private bool isProductionSpeedUpgradeAvailable()
    {
        return true;
    }

    private void OnTapped()
    {
        if (placementManager.isBuilding)
        {
            return;
        }

        Vector3 mapPosition = inputManager.GetSelectedMapPosition();
        Vector3Int gridPosition = grid.WorldToCell(mapPosition);
        gridPosition.z = 0;

        PlacementData placementData = placementManager.gridData.GetPlacementDataAt(gridPosition);
        int buildingIndex = placementData?.PlaceObjectIndex ?? -1;
        placementManager.selectedObjectIndex = buildingIndex;
        ObjectType? buildingType = placementData?.Type;

        if (buildingIndex != -1 && buildingType.HasValue)
        {
            if (buildingIndex == openToggleIndex && buildingType == (ObjectType)openToggleType)
            {
                CloseToggle(buildingIndex, buildingType.Value);
                openToggleIndex = -1;
                openToggleType = -1;
                return;
            }

            if (buildingType == ObjectType.Object)
            {
                GameObject buildingObject = placementManager.placedGameObjects[buildingIndex];

                ClickSquashEffect effect = buildingObject.GetComponent<ClickSquashEffect>();
                if (effect != null)
                {
                    effect.Play();
                }
            }
            else if (buildingType == ObjectType.Decoration)
            {
                GameObject decorationObject = placementManager.placedDecorations[buildingIndex];

                ClickSquashEffect effect = decorationObject.GetComponent<ClickSquashEffect>();
                if (effect != null)
                {
                    effect.Play();
                }
            }

            Vector3 cameraFocusPosition = GetBuildingCameraFocusPosition(placementData);
            buildingSelected?.Invoke(cameraFocusPosition);

            if (openToggleIndex != -1)
            {
                CloseToggle(openToggleIndex, (ObjectType)openToggleType);
            }

            if (buildingType == ObjectType.Object)
            {
                BuildingState state = placementManager.placedGameObjects[buildingIndex].GetComponent<Building>().State;
                if (state != BuildingState.UnderConstruction)
                {
                    buildingToggleList[buildingIndex].SetActive(true);
                }
            }
            else
            {
                decorationToggleList[buildingIndex].SetActive(true);
            }

            openToggleIndex = buildingIndex;
            openToggleType = (int)buildingType.Value;
        }
        else if (openToggleIndex != -1)
        {
            CloseToggle(openToggleIndex, (ObjectType)openToggleType);
            openToggleIndex = -1;
            openToggleType = -1;
        }
    }

    private void OnBuildingMoved(Vector3Int newPosition, int buildingIndex, int buildingType)
    {
        if (buildingType == 1)
        {
            RebindEditButton(decorationToggleList[buildingIndex], newPosition);
        }
        else if (buildingType == 0)
        {
            RebindEditButton(buildingToggleList[buildingIndex], newPosition);
        }

        openToggleIndex = -1;
        openToggleType = -1;
    }

    private bool IsFixedBuilding(int structureID)
    {
        return structureID == 28 || structureID == 31 || structureID == 38;
    }

    private void SetupButton(Transform parent, string buttonName, UnityEngine.Events.UnityAction action)
    {
        Transform buttonTransform = parent.Find(buttonName);
        if (buttonTransform == null) return;

        Button button = buttonTransform.GetComponent<Button>();
        if (button == null) return;

        button.onClick.AddListener(action);
    }

    private void ChangeOverviewVillagerSlot(int buildingIndex, int villagerSlot, Villager villager)
    {
        if (!TryGetWorkerSlotObjects(buildingIndex, villagerSlot, out GameObject overviewSlotImage, out GameObject registrySlotImage))
        {
            Debug.LogWarning($"Invalid worker UI mapping. buildingIndex={buildingIndex}, villagerSlot={villagerSlot}");
            return;
        }

        Image overviewImageComponent = overviewSlotImage.GetComponent<Image>();
        overviewImageComponent.sprite = villager.villagerData.villagerIcon;
        overviewImageComponent.SetNativeSize();
        overviewSlotImage.transform.localScale = new Vector3(0.9f, 0.9f, 1f);

        Vector3 pos = overviewSlotImage.transform.localPosition;
        overviewSlotImage.transform.localPosition = new Vector3(-1f, pos.y, pos.z);

        Image registryImageComponent = registrySlotImage.GetComponent<Image>();
        registryImageComponent.sprite = villager.villagerData.villagerIcon;
        registryImageComponent.SetNativeSize();
        registrySlotImage.transform.localScale = new Vector3(0.4f, 0.4f, 1f);

        Vector3 registryPos = registrySlotImage.transform.localPosition;
        registrySlotImage.transform.localPosition = new Vector3(-1f, registryPos.y, registryPos.z);
    }

    private bool TryGetWorkerSlotObjects(int buildingIndex, int slot, out GameObject overviewSlotObj, out GameObject registrySlotObj)
    {
        overviewSlotObj = null;
        registrySlotObj = null;

        if (!buildingOverviewByIndex.TryGetValue(buildingIndex, out GameObject buildingOverview))
        {
            return false;
        }

        if (!buildingRegistryByIndex.TryGetValue(buildingIndex, out GameObject buildingRegistry))
        {
            return false;
        }

        string overviewPath = GetOverviewWorkerPath(slot);
        string registryPath = GetRegistryWorkerPath(slot);

        if (overviewPath == null || registryPath == null)
        {
            return false;
        }

        overviewSlotObj = buildingOverview.transform.Find(overviewPath)?.gameObject;
        registrySlotObj = buildingRegistry.transform.Find(registryPath)?.gameObject;

        return overviewSlotObj != null && registrySlotObj != null;
    }

    private string GetOverviewWorkerPath(int slot)
    {
        return slot switch
        {
            1 => "Workers/WorkerOneFrame/Worker",
            2 => "Workers/WorkerTwoFrame/Worker",
            3 => "Workers/WorkerThreeFrame/Worker",
            _ => null
        };
    }

    private string GetRegistryWorkerPath(int slot)
    {
        return slot switch
        {
            1 => "Image/Workers/WorkerOne/WorkerButton",
            2 => "Image/Workers/WorkerTwo/WorkerButton",
            3 => "Image/Workers/WorkerThree/WorkerButton",
            _ => null
        };
    }

    private void ResetSlotToDefault(GameObject slotObject)
    {
        Image image = slotObject.GetComponent<Image>();
        image.sprite = addVillagerIcon;
        image.SetNativeSize();
    }

    private bool IsValidBuildingIndex(int index)
    {
        return buildingOverviewByIndex.ContainsKey(index) && buildingRegistryByIndex.ContainsKey(index);
    }

    private bool IsDecorationOrRoad(int structureID)
    {
        return (structureID >= 0 && structureID < 16) || structureID > 38;
    }

    private void SetRegistryWorkersLabelToVillagers(GameObject registryEntry)
    {
        registryEntry.transform.Find("Image/WorkerFrame/WorkersText").GetComponent<TextMeshProUGUI>().text = "Villagers";
    }

    private void HideHouseProgressBars(GameObject houseOverview)
    {
        houseOverview.transform.Find("Workers/WorkerOneFrame/ProgressBar").gameObject.SetActive(false);
        houseOverview.transform.Find("Workers/WorkerTwoFrame/ProgressBar").gameObject.SetActive(false);
        houseOverview.transform.Find("Workers/WorkerThreeFrame/ProgressBar").gameObject.SetActive(false);
    }

    private void CreateBuildingToggle(int structureID)
    {
        int buildingWidth = structures.objectsData[structureID].Size.x;
        Vector3 buildingPosition = placementManager.placedGameObjects[placementManager.placedGameObjects.Count - 1].transform.position;
        Vector3Int cellPosition = Vector3Int.RoundToInt(buildingPosition);

        Vector3 togglePosition = GetTogglePosition(buildingPosition, buildingWidth, structureID);

        GameObject prefab = IsFixedBuilding(structureID) ? fixedBuildingToggleUI : buildingToggleUI;
        GameObject buildingToggleButtons = Instantiate(prefab, togglePosition, Quaternion.identity, buildingToggleParent);

        buildingToggleList.Add(buildingToggleButtons);

        SetupButton(buildingToggleButtons.transform, "InfoButton", () =>
        {
            ActivateBuildingOverview(placementManager.selectedObjectIndex);
            TurnOffBuildingToggleUI();
        });

        if (!IsFixedBuilding(structureID))
        {
            SetupButton(buildingToggleButtons.transform, "EditButton", () =>
            {
                placementManager.SelectStructure(cellPosition);
                TurnOffBuildingToggleUI();
            });
        }
    }

    private Vector3 GetTogglePosition(Vector3 position, int width, int structureID = -1)
    {
        if (width == 2)
        {
            return new Vector3(position.x + 1f, position.y, position.z);
        }

        if (width == 1)
        {
            return new Vector3(position.x + 0.5f, position.y, position.z);
        }

        if (structureID == 38)
        {
            return new Vector3(position.x + 0.4f, position.y, position.z);
        }

        if (structureID == 28)
        {
            return new Vector3(position.x + 1.5f, position.y + 0.7f, position.z);
        }

        return new Vector3(position.x + 1.5f, position.y, position.z);
    }

    private void SetAllActive(List<GameObject> objects, bool active)
    {
        foreach (GameObject obj in objects)
        {
            obj.SetActive(active);
        }
    }

    private void CloseToggle(int index, ObjectType type)
    {
        if (type == ObjectType.Object)
        {
            buildingToggleList[index].SetActive(false);
        }
        else
        {
            decorationToggleList[index].SetActive(false);
        }
    }

    private void RebindEditButton(GameObject toggleRoot, Vector3Int newPosition)
    {
        Transform editButtonObject = toggleRoot.transform.Find("EditButton");
        Button editButton = editButtonObject != null ? editButtonObject.GetComponent<Button>() : null;
        if (editButton == null)
        {
            return;
        }

        editButton.onClick.RemoveAllListeners();
        editButton.onClick.AddListener(() => placementManager.SelectStructure(newPosition));
        editButton.onClick.AddListener(TurnOffBuildingToggleUI);
    }

    private void ConfigureBuiltStructure(
        int structureID,
        GameObject registryEntry,
        TextMeshProUGUI text,
        Image image,
        GameObject building,
        Building buildingData,
        int buildingIndex,
        int productionType)
    {
        switch (structureID)
        {
            case 16:
                SetupBasicBuilding(text, image, alchemistOne, "Alchemist LVL1", buildingData, 16);
                break;

            case 17:
                SetupBasicBuilding(text, image, armoryOne, "Armory LVL1", buildingData, 17);
                break;

            case 18:
                SetupProductionBuilding<Bakery>(text, image, bakeryOne, "Bakery LVL1", building, buildingData, buildingIndex, productionType, 18);
                break;

            case 19:
                SetupVillagerBuilding(registryEntry, text, image, barracksOne, "Barracks LVL1", buildingData, buildingIndex, 19, false);
                break;

            case 20:
                SetupVillagerBuilding(registryEntry, text, image, bathHouseOne, "Bath House LVL1", buildingData, buildingIndex, 20, false);
                break;

            case 21:
                SetupBasicBuilding(text, image, blacksmithOne, "Blacksmith LVL1", buildingData, 21);
                break;

            case 22:
                SetupProductionBuilding<Carpenter>(text, image, carpenterOne, "Carpenter LVL1", building, buildingData, buildingIndex, productionType, 22);
                break;

            case 23:
                SetupProductionBuilding<Farm>(text, image, farmOne, "Farm LVL1", building, buildingData, buildingIndex, productionType, 23);
                break;

            case 24:
                SetupVillagerBuilding(registryEntry, text, image, hospitalOne, "Hospital LVL1", buildingData, buildingIndex, 24, false);
                break;

            case 25:
                SetupHouseBuilding(registryEntry, text, image, buildingData, buildingIndex);
                break;

            case 26:
                SetupProductionBuilding<Logger>(text, image, loggerOne, "Logger LVL1", building, buildingData, buildingIndex, productionType, 26);
                break;

            case 27:
                SetupProductionBuilding<LumberMill>(text, image, lumberMillOne, "Lumber Mill LVL1", building, buildingData, buildingIndex, productionType, 27);
                break;

            case 28:
                SetupFixedProductionBuilding<Mine>(registryEntry, text, image, mineOne, "Mine (Not Built)", building, buildingData, buildingIndex, productionType, 28);
                break;

            case 29:
                SetupBasicBuilding(text, image, orchardOne, "Orchard LVL1", buildingData, 29);
                break;

            case 30:
                SetupProductionBuilding<Barn>(text, image, barnOne, "Ranch LVL1", building, buildingData, buildingIndex, productionType, 30);
                break;

            case 31:
                SetupFixedProductionBuilding<Refinery>(registryEntry, text, image, refineryOne, "Refinery (Not Built)", building, buildingData, buildingIndex, productionType, 31);
                break;

            case 32:
                SetupVillagerBuilding(registryEntry, text, image, schoolOne, "School LVL1", buildingData, buildingIndex, 32, false);
                break;

            case 33:
                EditRegistryEntry(text, image, stablesOne, scale, "Stables LVL1");
                SetRegistryWorkersLabelToVillagers(registryEntry);
                buildingData.BuildingName = "Stables LVL1";
                buildingData.ID = 33;
                break;

            case 34:
                SetupProductionBuilding<Tailor>(text, image, tailorOne, "Tailor LVL1", building, buildingData, buildingIndex, productionType, 34);
                break;

            case 35:
                SetupWarehouseBuilding(registryEntry, text, image, buildingData, buildingIndex);
                break;

            case 36:
                SetupProductionBuilding<Mill>(text, image, mill, "Windmill LVL1", building, buildingData, buildingIndex, productionType, 36);
                break;

            case 37:
                SetupProductionBuilding<Workshop>(text, image, workshopOne, "Workshop LVL1", building, buildingData, buildingIndex, productionType, 37);
                break;

            case 38:
                SetupMarketBuilding(registryEntry, text, image, buildingData, buildingIndex);
                break;
        }
    }

    private void SetupBasicBuilding(
        TextMeshProUGUI text,
        Image image,
        Sprite sprite,
        string buildingName,
        Building buildingData,
        int structureID)
    {
        EditRegistryEntry(text, image, sprite, scale, buildingName);
        buildingData.BuildingName = buildingName;
        buildingData.ID = structureID;
    }

    private void SetupVillagerBuilding(
        GameObject registryEntry,
        TextMeshProUGUI text,
        Image image,
        Sprite sprite,
        string buildingName,
        Building buildingData,
        int buildingIndex,
        int structureID,
        bool isHouse)
    {
        EditRegistryEntry(text, image, sprite, scale, buildingName);
        SetRegistryWorkersLabelToVillagers(registryEntry);

        GameObject overview = Instantiate(buildingOverviewScreenTwo);
        SetUpBuildingOverview(overview, buildingIndex);
        InitializeUpgradeButtonForBuilding(overview, buildingData, buildingIndex, structureID);
        ChangeBuildingOverviewName(overview, buildingName);

        buildingData.BuildingName = buildingName;
        buildingData.ID = structureID;

        AssignVillagerSlotButtons(overview, isHouse);
    }

    private void SetupHouseBuilding(
        GameObject registryEntry,
        TextMeshProUGUI text,
        Image image,
        Building buildingData,
        int buildingIndex)
    {
        EditRegistryEntry(text, image, houseOne, scale, "House LVL1");
        SetRegistryWorkersLabelToVillagers(registryEntry);

        GameObject overview = Instantiate(buildingOverviewScreenTwo);
        SetUpBuildingOverview(overview, buildingIndex);
        HideHouseProgressBars(overview);
        InitializeUpgradeButtonForBuilding(overview, buildingData, buildingIndex, 25);
        ChangeBuildingOverviewName(overview, "House LVL1");

        buildingData.BuildingName = "House LVL1";
        buildingData.ID = 25;

        AssignVillagerSlotButtons(overview, true);
    }

    private void SetupMarketBuilding(
        GameObject registryEntry,
        TextMeshProUGUI text,
        Image image,
        Building buildingData,
        int buildingIndex)
    {
        string marketName = buildingData.Level <= 0
            ? "Market (Not Built)"
            : $"Market LVL{buildingData.Level}";

        buildingData.BuildingName = marketName;
        buildingData.ID = 38;

        EditRegistryEntry(text, image, marketOne, new Vector3(0.2f, 0.2f, 1f), buildingData.BuildingName);

        registryEntry.transform.Find("Image/WorkerFrame").gameObject.SetActive(true);
        registryEntry.transform.Find("Image/Workers").gameObject.SetActive(false);
        registryEntry.transform.Find("Image/FrameTwo").gameObject.SetActive(true);
        registryEntry.SetActive(false);

        GameObject overview = Instantiate(marketOverviewScreen);
        SetUpBuildingOverview(overview, buildingIndex);
        InitializeUpgradeButtonForBuilding(overview, buildingData, buildingIndex, 38);

        text.text = buildingData.BuildingName;
        SyncBuildingNameUI(overview, registryEntry, buildingData.BuildingName);

        Market market = buildingData as Market;
        if (market != null)
        {
            market.ConnectMarketOverview(overview);
        }

        UpdateMarketRegistryUI(buildingIndex);

        TextMeshProUGUI registryTitle = registryEntry.transform
            .Find("Image/BuildingName")
            ?.GetComponent<TextMeshProUGUI>();

        if (registryTitle != null)
        {
            registryTitle.text = buildingData.BuildingName;
        }
    }

    private void SetupFixedProductionBuilding<T>(
        GameObject registryEntry,
        TextMeshProUGUI text,
        Image image,
        Sprite sprite,
        string buildingName,
        GameObject building,
        Building buildingData,
        int buildingIndex,
        int productionType,
        int structureID) where T : ProductionBuilding
    {
        EditRegistryEntry(text, image, sprite, scale, buildingName);
        registryEntry.SetActive(false);

        GameObject overview = Instantiate(buildingOverviewScreen);
        SetUpBuildingOverview(overview, buildingIndex);

        ProductionBuilding data = building.GetComponent<T>();
        InitializeProductionBuildingOverview(overview, buildingName, productionType, data);

        buildingData.BuildingName = buildingName;
        buildingData.ID = structureID;

        overview.transform.Find("Workers/WorkerOneDisabled").gameObject.SetActive(true);
        overview.transform.Find("Workers/WorkerOneFrame").gameObject.SetActive(false);

        AssignVillagerSlotButtons(overview, false);
    }

    private void SetupProductionBuilding<T>(
        TextMeshProUGUI text,
        Image image,
        Sprite sprite,
        string buildingName,
        GameObject building,
        Building buildingData,
        int buildingIndex,
        int productionType,
        int structureID) where T : ProductionBuilding
    {
        EditRegistryEntry(text, image, sprite, scale, buildingName);

        GameObject overview = Instantiate(buildingOverviewScreen);
        SetUpBuildingOverview(overview, buildingIndex);

        ProductionBuilding data = building.GetComponent<T>();
        InitializeProductionBuildingOverview(overview, buildingName, productionType, data);

        buildingData.BuildingName = buildingName;
        buildingData.ID = structureID;

        AssignVillagerSlotButtons(overview, false);
    }

    private void SyncBuildingNameUI(GameObject buildingOverview, GameObject buildingRegistry, string buildingName)
    {
        Transform overviewNameTransform = buildingOverview.transform.Find("BuildingName");

        if (overviewNameTransform == null)
        {
            overviewNameTransform = buildingOverview.transform.Find("TitleBoard/BuildingName");
        }

        if (overviewNameTransform == null)
        {
            overviewNameTransform = buildingOverview.transform.Find("TitleBoard/WarehouseTitleText");
        }

        if (overviewNameTransform != null)
        {
            TextMeshProUGUI overviewNameText = overviewNameTransform.GetComponent<TextMeshProUGUI>();

            if (overviewNameText != null)
            {
                overviewNameText.text = buildingName;
            }
        }

        Transform registryNameTransform = buildingRegistry.transform.Find("Image/BuildingName");

        if (registryNameTransform != null)
        {
            TextMeshProUGUI registryNameText = registryNameTransform.GetComponent<TextMeshProUGUI>();

            if (registryNameText != null)
            {
                registryNameText.text = buildingName;
            }
        }
    }

    private void UpdateRegistrySpriteForBuildingLevel(
        GameObject buildingRegistry,
        GameObject buildingOverview,
        Image buildingRegistryImage,
        Building buildingData,
        int structureID)
    {
        int[] villagersBuilding = { 19, 20, 24, 25, 32 };

        switch (buildingData.Level)
        {
            case 1:
                buildingRegistryImage.sprite = buildingData.BuildingLevelOne;

                if (buildingData.State != BuildingState.UnderConstruction)
                {
                    buildingRegistry.SetActive(true);
                }

                break;

            case 2:
                buildingRegistryImage.sprite = buildingData.BuildingLevelTwo;

                if (Array.Exists(villagersBuilding, element => element == structureID))
                {
                    ShowWorkerSlot(buildingOverview, buildingRegistry, 2);
                }

                break;

            case 3:
                buildingRegistryImage.sprite = buildingData.BuildingLevelThree;

                if (Array.Exists(villagersBuilding, element => element == structureID))
                {
                    ShowWorkerSlot(buildingOverview, buildingRegistry, 3);
                }

                break;
        }
    }

    private void UpdateRegistrySpriteForProductionBuilding(
        GameObject buildingRegistry,
        GameObject buildingOverview,
        Image buildingRegistryImage,
        ProductionBuilding buildingData)
    {
        switch (buildingData.Level)
        {
            case 1:
                buildingRegistryImage.sprite = buildingData.BuildingLevelOne;

                if (buildingData.State != BuildingState.UnderConstruction)
                {
                    buildingRegistry.SetActive(true);
                }

                ShowWorkerSlot(buildingOverview, buildingRegistry, 1);
                break;

            case 2:
                buildingRegistryImage.sprite = buildingData.BuildingLevelTwo;
                ShowWorkerSlot(buildingOverview, buildingRegistry, 2);
                break;

            case 3:
                buildingRegistryImage.sprite = buildingData.BuildingLevelThree;
                ShowWorkerSlot(buildingOverview, buildingRegistry, 3);
                break;
        }
    }

    private void ShowWorkerSlot(GameObject buildingOverview, GameObject buildingRegistry, int slot)
    {
        switch (slot)
        {
            case 1:
                buildingOverview.transform.Find("Workers/WorkerOneDisabled").gameObject.SetActive(false);
                buildingOverview.transform.Find("Workers/WorkerOneFrame").gameObject.SetActive(true);
                break;

            case 2:
                buildingOverview.transform.Find("Workers/WorkerTwoDisabled").gameObject.SetActive(false);
                buildingOverview.transform.Find("Workers/WorkerTwoFrame").gameObject.SetActive(true);
                buildingRegistry.transform.Find("Image/Workers/HideTwo").gameObject.SetActive(false);
                buildingRegistry.transform.Find("Image/Workers/WorkerTwo").gameObject.SetActive(true);
                break;

            case 3:
                buildingOverview.transform.Find("Workers/WorkerThreeDisabled").gameObject.SetActive(false);
                buildingOverview.transform.Find("Workers/WorkerThreeFrame").gameObject.SetActive(true);
                buildingRegistry.transform.Find("Image/Workers/HideThree").gameObject.SetActive(false);
                buildingRegistry.transform.Find("Image/Workers/WorkerThree").gameObject.SetActive(true);
                break;
        }
    }

    private void UpdateProductionAmountsAfterUpgrade(GameObject buildingOverview, ProductionBuilding buildingData)
    {
        if (buildingData.ProductionType == 0)
        {
            buildingOverview.transform.Find("0/OutputFrame/OutputAmount").GetComponent<TextMeshProUGUI>().text =
                buildingData.ProducedResourcesAmount[0] + "x";
        }
        else if (buildingData.ProductionType == 1)
        {
            buildingOverview.transform.Find("1/InputFrame/InputAmount").GetComponent<TextMeshProUGUI>().text =
                buildingData.NeededResourcesAmount[0] + "x";
            buildingOverview.transform.Find("1/OutputFrame/OutputAmount").GetComponent<TextMeshProUGUI>().text =
                buildingData.ProducedResourcesAmount[0] + "x";
        }
        else if (buildingData.ProductionType == 2)
        {
            buildingOverview.transform.Find("2/InputFrameOne/InputAmount").GetComponent<TextMeshProUGUI>().text =
                buildingData.NeededResourcesAmount[0] + "x";
            buildingOverview.transform.Find("2/InputFrameTwo/InputAmount").GetComponent<TextMeshProUGUI>().text =
                buildingData.NeededResourcesAmount[1] + "x";
            buildingOverview.transform.Find("2/OutputFrame/OutputAmount").GetComponent<TextMeshProUGUI>().text =
                buildingData.ProducedResourcesAmount[0] + "x";
        }
        else if (buildingData.ProductionType == 3)
        {
            bool hasInput = buildingData.NeededResourcesID != null &&
                            buildingData.NeededResourcesID.Length > 0 &&
                            buildingData.NeededResourcesAmount != null &&
                            buildingData.NeededResourcesAmount.Length > 0;

            if (hasInput)
            {
                buildingOverview.transform.Find("1/InputFrame/InputAmount").GetComponent<TextMeshProUGUI>().text =
                    buildingData.NeededResourcesAmount[0] + "x";

                buildingOverview.transform.Find("1 (Two)/InputFrame/InputAmount").GetComponent<TextMeshProUGUI>().text =
                    buildingData.NeededResourcesAmount[0] + "x";
            }

            buildingOverview.transform.Find("1/OutputFrame/OutputAmount").GetComponent<TextMeshProUGUI>().text =
                buildingData.ProducedResourcesAmount[0] + "x";

            buildingOverview.transform.Find("1 (Two)/OutputFrame/OutputAmount").GetComponent<TextMeshProUGUI>().text =
                buildingData.ProducedResourcesAmount[1] + "x";
        }
        else if (buildingData.ProductionType == 4)
        {
            buildingOverview.transform.Find("4/OutputFrameOne/OutputAmount").GetComponent<TextMeshProUGUI>().text =
                buildingData.ProducedResourcesAmount[0] + "x";
            buildingOverview.transform.Find("4/OutputFrameTwo/OutputAmount").GetComponent<TextMeshProUGUI>().text =
                buildingData.ProducedResourcesAmount[1] + "x";
        }
    }

    private void ResetProductionProgressBars(GameObject buildingOverview, ProductionBuilding buildingData)
    {
        Transform progressBar = buildingOverview.transform.Find("UpgradeTime/ProgressBar");
        ProductionProgressBar productionProgressBar = progressBar.GetComponent<ProductionProgressBar>();
        productionProgressBar.timer = 0;

        if (buildingData.ProductionType == 3)
        {
            Transform progressBarTwo = buildingOverview.transform.Find("UpgradeTimeTwo/ProgressBar");
            ProductionProgressBar productionProgressBarTwo = progressBarTwo.GetComponent<ProductionProgressBar>();
            productionProgressBarTwo.timer = 0;
        }
    }

    public void UpgradeProductionSpeedForBuildingWithIndex(int index, int resourceIndex)
    {
        GameObject building = placementManager.placedGameObjects[index];
        ProductionBuilding buildingData = building.GetComponent<ProductionBuilding>();
        buildingData.UpgradeProductionSpeed(resourceIndex);

        GameObject buildingOverview = buildingOverviewByIndex[index];

        if (resourceIndex == 0)
        {
            Transform progressBarObject = buildingOverview.transform.Find("UpgradeTime/ProgressBar");
            ProductionProgressBar progressBar = progressBarObject.GetComponent<ProductionProgressBar>();
            progressBar.productionTime = buildingData.ResourceProductionTime[resourceIndex];

            Transform productionSpeedLevelDisplay = buildingOverview.transform.Find("UpgradeTime/LevelFrame/ProductionSpeedText");
            TextMeshProUGUI productionSpeedLevelText = productionSpeedLevelDisplay.GetComponent<TextMeshProUGUI>();
            productionSpeedLevelText.text = "LVL" + buildingData.ProductionSpeedLevel[0];
        }
        else
        {
            Transform progressBarObject = buildingOverview.transform.Find("UpgradeTimeTwo/ProgressBar");
            ProductionProgressBar progressBar = progressBarObject.GetComponent<ProductionProgressBar>();
            progressBar.productionTime = buildingData.ResourceProductionTime[resourceIndex];

            Transform productionSpeedLevelDisplay = buildingOverview.transform.Find("UpgradeTimeTwo/LevelFrame/ProductionSpeedText");
            TextMeshProUGUI productionSpeedLevelText = productionSpeedLevelDisplay.GetComponent<TextMeshProUGUI>();
            productionSpeedLevelText.text = "LVL" + buildingData.ProductionSpeedLevel[1];
        }

        isProductionSpeedUpgradeAvailable();
    }
    private void SetTwoOutputOnlyUI(
    GameObject root,
    int outputResourceIdOne,
    int outputAmountOne,
    int outputResourceIdTwo,
    int outputAmountTwo)
    {
        SetResourceIconAndAmount(root, "4/OutputFrameOne/Image", "4/OutputFrameOne/OutputAmount", outputResourceIdOne, outputAmountOne);
        SetResourceIconAndAmount(root, "4/OutputFrameTwo/Image", "4/OutputFrameTwo/OutputAmount", outputResourceIdTwo, outputAmountTwo);
    }

    private void SetupWarehouseBuilding(
        GameObject registryEntry,
        TextMeshProUGUI text,
        Image image,
        Building buildingData,
        int buildingIndex)
    {
        EditRegistryEntry(text, image, warehouseOne, scale, "Warehouse LVL1");

        registryEntry.transform.Find("Image/Workers").gameObject.SetActive(false);
        Transform workersRoot = registryEntry.transform.Find("Image/WorkerFrame");

        if (workersRoot != null)
        {
            Transform workersText = workersRoot.Find("WorkersText");
            if (workersText != null)
            {
                workersText.GetComponent<TextMeshProUGUI>().text = "Cap : 0/100";
            }
        }

        GameObject overview = Instantiate(warehouseOverviewScreen);
        SetUpBuildingOverview(overview, buildingIndex);

        InitializeUpgradeButtonForBuilding(overview, buildingData, buildingIndex, 35);

        buildingData.BuildingName = "Warehouse LVL1";
        buildingData.ID = 35;

        UpdateWarehouseUI(overview);
    }

    private void UpdateWarehouseUI(GameObject overview)
    {
        ResourceManager resourceManager = GameObject.Find("ResourceManager").GetComponent<ResourceManager>();

        int buildingIndex = -1;

        foreach (var kvp in buildingOverviewByIndex)
        {
            if (kvp.Value == overview)
            {
                buildingIndex = kvp.Key;
                break;
            }
        }

        if (buildingIndex >= 0 && buildingIndex < placementManager.placedGameObjects.Count)
        {
            Building buildingData = placementManager.placedGameObjects[buildingIndex].GetComponent<Building>();

            TextMeshProUGUI titleText = overview.transform
                .Find("TitleBoard/WarehouseTitleText")
                ?.GetComponent<TextMeshProUGUI>();

            if (titleText != null)
            {
                titleText.text = buildingData.BuildingName;
            }

            UpdateWarehouseRegistryCapacityText(buildingIndex);
        }

        TextMeshProUGUI capacityText = overview.transform
            .Find("CapacityBoard/CapacityText")
            ?.GetComponent<TextMeshProUGUI>();

        if (capacityText != null)
        {
            int used = resourceManager.GetUsedCapacity();
            int max = resourceManager.GetMaxCapacity();
            capacityText.text = $"Cap : {used}/{max}";
        }

        UpdateResourceText(overview, "Entries/EntryList/RowOne/WheatFrame/AmountText", 0);
        UpdateResourceText(overview, "Entries/EntryList/RowOne/WoodFrame/AmountText", 1);
        UpdateResourceText(overview, "Entries/EntryList/RowOne/StoneFrame/AmountText", 3);
        UpdateResourceText(overview, "Entries/EntryList/RowOne/IronFrame/AmountText", 4);

        UpdateResourceText(overview, "Entries/EntryList/RowTwo/LeatherFrame/AmountText", 6);
        UpdateResourceText(overview, "Entries/EntryList/RowTwo/MeatFrame/AmountText", 7);
        UpdateResourceText(overview, "Entries/EntryList/RowTwo/FlourFrame/AmountText", 8);
        UpdateResourceText(overview, "Entries/EntryList/RowTwo/ToolsFrame/AmountText", 2);

        UpdateResourceText(overview, "Entries/EntryList/RowThree/BreadFrame/AmountText", 9);
        UpdateResourceText(overview, "Entries/EntryList/RowThree/HardwoodFrame/AmountText", 5);
        UpdateResourceText(overview, "Entries/EntryList/RowThree/ClothingFrame/AmountText", 10);
        UpdateResourceText(overview, "Entries/EntryList/RowThree/FurnitureFrame/AmountText", 11);
    }

    private void UpdateResourceText(GameObject root, string path, int resourceID)
    {
        Transform t = root.transform.Find(path);

        if (t == null) return;

        TextMeshProUGUI text = t.GetComponent<TextMeshProUGUI>();

        ResourceManager resourceManager = GameObject.Find("ResourceManager").GetComponent<ResourceManager>();

        text.text = resourceManager.resourceTotals[resourceID].ToString();
    }

    private void RefreshSpecialOverviewUI(int buildingIndex, GameObject overview)
    {
        if (buildingIndex < 0 || buildingIndex >= placementManager.placedGameObjects.Count)
        {
            return;
        }

        GameObject building = placementManager.placedGameObjects[buildingIndex];
        if (building == null)
        {
            return;
        }

        Warehouse warehouse = building.GetComponent<Warehouse>();
        if (warehouse != null)
        {
            UpdateWarehouseUI(overview);
        }

        Market market = building.GetComponent<Market>();
        if (market != null)
        {
            market.RefreshAllOrderButtons();
        }
    }

    public void RefreshAllWarehouseOverviews()
    {
        foreach (var kvp in buildingOverviewByIndex)
        {
            int buildingIndex = kvp.Key;
            GameObject overview = kvp.Value;

            if (buildingIndex < 0 || buildingIndex >= placementManager.placedGameObjects.Count)
            {
                continue;
            }

            GameObject building = placementManager.placedGameObjects[buildingIndex];
            if (building == null)
            {
                continue;
            }

            Warehouse warehouse = building.GetComponent<Warehouse>();
            if (warehouse != null)
            {
                UpdateWarehouseUI(overview);
            }
        }
    }
    private void UpdateWarehouseRegistryCapacityText(int buildingIndex)
    {
        if (!buildingRegistryByIndex.TryGetValue(buildingIndex, out GameObject registryEntry))
        {
            return;
        }

        ResourceManager resourceManager = GameObject.Find("ResourceManager").GetComponent<ResourceManager>();

        TextMeshProUGUI capacityText = registryEntry
            .transform
            .Find("Image/WorkerFrame/WorkersText")
            ?.GetComponent<TextMeshProUGUI>();

        if (capacityText != null)
        {
            capacityText.text = $"Cap : {resourceManager.GetUsedCapacity()}/{resourceManager.GetMaxCapacity()}";
        }
    }
    private Vector3 GetBuildingCameraFocusPosition(PlacementData placementData)
    {
        if (placementData == null)
        {
            return Vector3.zero;
        }

        Vector3Int origin = placementData.occupiedPositions[0];

        int databaseIndex = structures.objectsData.FindIndex(data => data.ID == placementData.ID);

        if (databaseIndex < 0)
        {
            return grid.CellToWorld(origin);
        }

        Vector2Int size = structures.objectsData[databaseIndex].Size;

        Vector3 worldOrigin = grid.CellToWorld(origin);

        Vector3 focusPosition = worldOrigin + new Vector3(size.x * 0.5f, size.y * 0.5f, 0f);

        int placedIndex = placementData.PlaceObjectIndex;

        if (placementData.Type == ObjectType.Object &&
            placedIndex >= 0 &&
            placedIndex < placementManager.placedGameObjects.Count)
        {
            GameObject buildingObject = placementManager.placedGameObjects[placedIndex];

            if (buildingObject != null && buildingObject.GetComponent<Market>() != null)
            {
                focusPosition.y -= 2f;
            }
            else if (buildingObject.GetComponent<Mine>() != null)
            {
                focusPosition.y -= 1f; // tweak this value
            }
        }

        return focusPosition;
    }
    private void SetLockedInputFrame(GameObject root, string framePath)
    {
        Transform frame = root.transform.Find(framePath);
        if (frame == null)
        {
            return;
        }

        Image frameImage = frame.GetComponent<Image>();
        if (frameImage != null && lockedFrameSprite != null)
        {
            frameImage.sprite = lockedFrameSprite;
        }

        Transform icon = frame.Find("Image");
        if (icon != null)
        {
            icon.gameObject.SetActive(false);
        }

        Transform amount = frame.Find("InputAmount");
        if (amount != null)
        {
            amount.gameObject.SetActive(false);
        }
    }
    private void UpdateMarketRegistryUI(int buildingIndex)
    {
        if (!buildingRegistryByIndex.TryGetValue(buildingIndex, out GameObject registryEntry))
        {
            return;
        }

        if (buildingIndex < 0 || buildingIndex >= placementManager.placedGameObjects.Count)
        {
            return;
        }

        Market market = placementManager.placedGameObjects[buildingIndex].GetComponent<Market>();
        if (market == null)
        {
            return;
        }

        TextMeshProUGUI capText = registryEntry.transform
            .Find("Image/WorkerFrame/WorkersText")
            ?.GetComponent<TextMeshProUGUI>();

        if (capText != null)
        {
            capText.text = market.GetOrderCapText();
        }

        TextMeshProUGUI timerText = registryEntry.transform
            .Find("Image/FrameTwo/WorkersText")
            ?.GetComponent<TextMeshProUGUI>();

        if (timerText != null)
        {
            string text = market.GetNextOrderText();
            timerText.text = text;

            if (text == "Orders Full")
            {
                timerText.color = new Color(1f, 0.6f, 0f); // orange
            }
            else
            {
                timerText.color = Color.white;
            }
        }
    }
    public void HideBuildingAccessDuringConstruction(int buildingIndex)
    {
        CloseBuildingOverview(buildingIndex);

        if (buildingRegistryByIndex.TryGetValue(buildingIndex, out GameObject registryEntry))
        {
            registryEntry.SetActive(false);
        }

        if (buildingIndex >= 0 && buildingIndex < buildingToggleList.Count)
        {
            buildingToggleList[buildingIndex].SetActive(false);
        }

        if (openToggleIndex == buildingIndex && openToggleType == (int)ObjectType.Object)
        {
            openToggleIndex = -1;
            openToggleType = -1;
        }
    }

    public void ShowBuildingAccessAfterConstruction(int buildingIndex)
    {
        if (buildingRegistryByIndex.TryGetValue(buildingIndex, out GameObject registryEntry))
        {
            registryEntry.SetActive(true);
        }
    }
}