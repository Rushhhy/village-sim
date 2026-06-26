using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Market : Building
{
    [SerializeField] private GameObject marketOne;
    [SerializeField] private GameObject marketTwo;
    [SerializeField] private GameObject marketThree;

    private static readonly Vector3 BuildOffset = new Vector3(0f, 0.7f, 0f);
    private static readonly Vector3 HammerExtraOffset = new Vector3(0.5f, 1.5f, 0f);

    [Header("Market Orders")]
    [SerializeField] private GameObject marketEntryPrefab;
    [SerializeField] private ResourceSO resources;
    [SerializeField] private float newOrderTime = 10f;
    [SerializeField] private int maxOrdersLevelOne = 2;
    [SerializeField] private int maxOrdersLevelTwo = 3;
    [SerializeField] private int maxOrdersLevelThree = 4;

    private readonly List<MarketOrder> activeOrders = new();
    private readonly List<GameObject> activeOrderObjects = new();

    private ResourceManager resourceManager;
    private Transform entryList;
    private TextMeshProUGUI newOrderText;

    private float orderTimer;
    private int nextOrderNumber = 1;

    private TextMeshProUGUI capText;
    [SerializeField] private GameObject noOrdersText;

    protected override void Awake()
    {
        base.Awake();
        width = 4;

        resourceManager = GameObject.Find("ResourceManager").GetComponent<ResourceManager>();

        SetInitialMarketName();
    }

    protected override void Update()
    {
        base.Update();

        if (State != BuildingState.Active)
        {
            return;
        }

        if (Level <= 0)
        {
            if (newOrderText != null)
            {
                newOrderText.text = "Market not built";
            }

            return;
        }

        UpdateOrderTimer();
    }

    public void ConnectMarketOverview(GameObject overview)
    {
        entryList = overview.transform.Find("Entries/EntryList");
        newOrderText = overview.transform.Find("NewOrderText")?.GetComponent<TextMeshProUGUI>();
        capText = overview.transform.Find("CapText")?.GetComponent<TextMeshProUGUI>();
        noOrdersText = overview.transform.Find("Entries/NoOrdersText")?.gameObject;

        RefreshOrderUI();
        UpdateCapText();
        RefreshNoOrdersText();
        RefreshAllOrderButtons();
    }

    private void RefreshNoOrdersText()
    {
        if (noOrdersText == null)
        {
            return;
        }

        noOrdersText.SetActive(activeOrders.Count == 0);
    }

    private void UpdateCapText()
    {
        if (capText == null)
        {
            return;
        }

        if (Level <= 0)
        {
            capText.text = "Order Cap: 0/0";
            return;
        }

        capText.text = $"Order Cap: {activeOrders.Count}/{GetMaxOrders()}";
    }

    private void UpdateOrderTimer()
    {
        if (activeOrders.Count >= GetMaxOrders())
        {
            if (newOrderText != null)
            {
                newOrderText.text = "Orders Full";
            }

            return;
        }

        orderTimer += Time.deltaTime;

        float remaining = Mathf.Max(0f, newOrderTime - orderTimer);

        if (newOrderText != null)
        {
            newOrderText.text = "Next Order: " + Mathf.CeilToInt(remaining) + "s";
        }

        if (orderTimer >= newOrderTime)
        {
            AddNewOrder();
            orderTimer = 0f;
        }

        RefreshAllOrderButtons();
    }

    private void AddNewOrder()
    {
        if (activeOrders.Count >= GetMaxOrders())
        {
            return;
        }

        MarketOrder order = GenerateOrder();
        activeOrders.Add(order);

        CreateOrderUI(order);
        UpdateCapText();
        RefreshNoOrdersText();
    }

    private void CreateOrderUI(MarketOrder order)
    {
        if (entryList == null || marketEntryPrefab == null)
        {
            return;
        }

        GameObject entry = Instantiate(marketEntryPrefab, entryList);
        activeOrderObjects.Add(entry);

        MarketOrderUI ui = entry.GetComponent<MarketOrderUI>();
        ui.Initialize(this, order, resources);
    }

    private void RefreshOrderUI()
    {
        if (entryList == null)
        {
            return;
        }

        foreach (GameObject obj in activeOrderObjects)
        {
            Destroy(obj);
        }

        activeOrderObjects.Clear();

        foreach (MarketOrder order in activeOrders)
        {
            CreateOrderUI(order);
        }

        UpdateCapText();
        RefreshNoOrdersText();
    }

    private MarketOrder GenerateOrder()
    {
        int itemCount = Random.Range(1, GetMaxItemsPerOrder() + 1);

        List<int> resourceIDs = new();
        List<int> amounts = new();

        for (int i = 0; i < itemCount; i++)
        {
            int resourceID = GetWeightedResourceForLevel();

            if (resourceIDs.Contains(resourceID))
            {
                i--;
                continue;
            }

            resourceIDs.Add(resourceID);
            amounts.Add(GetAmountForResource(resourceID));
        }

        int reward = CalculateCoinReward(resourceIDs, amounts);
        int gemReward = Random.Range(0, 100) < 5 ? 1 : 0;

        return new MarketOrder
        {
            orderNumber = nextOrderNumber++,
            resourceIDs = resourceIDs.ToArray(),
            amounts = amounts.ToArray(),
            coinReward = reward,
            gemReward = gemReward
        };
    }

    public bool CanCompleteOrder(MarketOrder order)
    {
        for (int i = 0; i < order.resourceIDs.Length; i++)
        {
            if (!resourceManager.HasEnoughResource(order.resourceIDs[i], order.amounts[i]))
            {
                return false;
            }
        }

        return true;
    }

    public void CompleteOrder(MarketOrder order, GameObject orderObject)
    {
        if (!CanCompleteOrder(order))
        {
            return;
        }

        for (int i = 0; i < order.resourceIDs.Length; i++)
        {
            resourceManager.TryConsumeResource(order.resourceIDs[i], order.amounts[i]);
        }

        resourceManager.AddCoins(order.coinReward);

        if (order.gemReward > 0)
        {
            resourceManager.AddGems(order.gemReward);
        }

        activeOrders.Remove(order);
        activeOrderObjects.Remove(orderObject);
        Destroy(orderObject);

        UpdateCapText();
        RefreshNoOrdersText();
        RefreshAllOrderButtons();
    }

    public void RefreshOrder(MarketOrder oldOrder, GameObject orderObject)
    {
        int index = activeOrders.IndexOf(oldOrder);

        if (index < 0)
        {
            return;
        }

        MarketOrder newOrder = GenerateOrder();
        activeOrders[index] = newOrder;

        MarketOrderUI ui = orderObject.GetComponent<MarketOrderUI>();
        ui.Initialize(this, newOrder, resources);

        UpdateCapText();
        RefreshNoOrdersText();
        RefreshAllOrderButtons();
    }

    private int GetMaxOrders()
    {
        return Level switch
        {
            1 => maxOrdersLevelOne,
            2 => maxOrdersLevelTwo,
            3 => maxOrdersLevelThree,
            _ => 0
        };
    }

    private int GetAmountForResource(int resourceID)
    {
        if (resourceID == 0 || resourceID == 1 || resourceID == 3)
        {
            return Random.Range(6, 12);
        }

        if (resourceID == 2 || resourceID == 4 || resourceID == 5 || resourceID == 6 || resourceID == 7 || resourceID == 8)
        {
            return Random.Range(3, 7);
        }

        return Random.Range(1, 4);
    }

    private int CalculateCoinReward(List<int> resourceIDs, List<int> amounts)
    {
        if (resources == null || resources.resourcesData == null)
        {
            return 10;
        }

        int total = 0;

        for (int i = 0; i < resourceIDs.Count; i++)
        {
            int resourceID = resourceIDs[i];

            if (resourceID < 0 || resourceID >= resources.resourcesData.Count)
            {
                continue;
            }

            total += resources.resourcesData[resourceID].Price * amounts[i];
        }

        return Mathf.RoundToInt(total * GetRewardMultiplier());
    }

    private float GetRewardMultiplier()
    {
        return Level switch
        {
            1 => 1.3f,
            2 => 1.5f,
            3 => 1.8f,
            _ => 1.3f
        };
    }

    public override void SetBuildingSprite()
    {
        // Level 0 = unbuilt broken market should stay visible
        if (Level == 0)
        {
            Transform broken = transform.Find("MarketBroken");
            if (broken != null)
            {
                broken.gameObject.SetActive(true);
            }
            return;
        }

        RemoveMarketVisualForLevel(Level);
        GameObject prefab = GetMarketPrefabForLevel(Level);

        if (prefab != null)
        {
            Instantiate(prefab, transform);
        }
    }

    protected override void StartBuildOrUpgrade(int level)
    {
        base.StartBuildOrUpgrade(level);

        transform.position += BuildOffset;

        if (currentProgressBar != null)
        {
            currentProgressBar.transform.position += BuildOffset;
        }

        if (finishConstructionObj != null)
        {
            finishConstructionObj.transform.position += BuildOffset;
        }

        if (hammerObj != null)
        {
            hammerObj.transform.position += HammerExtraOffset;
        }
    }

    public override void UpgradeBuilding()
    {
        HideMarketVisualForLevel(Level);

        Level++;
        StartBuildOrUpgrade(Level);
        SetBuildingNameByLevel("Market");
    }

    public override void FinishConstruction()
    {
        base.FinishConstruction();

        if (Index == 0 || Index == 1)
        {
            SpriteRenderer.sprite = null;
        }

        transform.position -= BuildOffset;
    }

    private void HideMarketVisualForLevel(int level)
    {
        Transform visual = FindMarketVisualForLevel(level);
        if (visual != null)
        {
            visual.gameObject.SetActive(false);
        }
    }

    private void RemoveMarketVisualForLevel(int level)
    {
        Transform visual = FindMarketVisualForLevel(level);
        if (visual != null)
        {
            Destroy(visual.gameObject);
        }
    }

    private Transform FindMarketVisualForLevel(int level)
    {
        return level switch
        {
            0 => transform.Find("MarketBroken"),
            1 => transform.Find("MarketOne(Clone)"),
            2 => transform.Find("MarketTwo(Clone)"),
            _ => null
        };
    }

    private GameObject GetMarketPrefabForLevel(int level)
    {
        return level switch
        {
            1 => marketOne,
            2 => marketTwo,
            3 => marketThree,
            _ => null
        };
    }

    private void SetInitialMarketName()
    {
        if (Level <= 0)
        {
            BuildingName = "Market (Not Built)";
        }
        else
        {
            SetBuildingNameByLevel("Market");
        }
    }

    public string GetOrderCapText()
    {
        return $"Cap: {activeOrders.Count}/{GetMaxOrders()}";
    }

    public string GetNextOrderText()
    {
        if (Level <= 0)
        {
            return "Not Built";
        }

        if (activeOrders.Count >= GetMaxOrders())
        {
            return "Orders Full";
        }

        float remaining = Mathf.Max(0f, newOrderTime - orderTimer);
        return "Next Order in " + Mathf.CeilToInt(remaining) + "s";
    }

    public void RefreshAllOrderButtons()
    {
        foreach (GameObject orderObject in activeOrderObjects)
        {
            if (orderObject == null)
            {
                continue;
            }

            MarketOrderUI ui = orderObject.GetComponent<MarketOrderUI>();
            if (ui != null)
            {
                ui.RefreshCompleteState();
            }
        }
    }
    private int GetWeightedResourceForLevel()
    {
        int roll = Random.Range(0, 100);

        int[] basic = { 0, 1, 3 };
        // Wheat, Wood, Stone

        int[] intermediate = { 2, 4, 5, 6, 7, 8 };
        // Tools, Iron, Hardwood, Leather, Meat, Flour

        int[] advanced = { 9, 10, 11 };
        // Bread, Clothing, Furniture

        if (Level == 1)
        {
            return basic[Random.Range(0, basic.Length)];
        }

        if (Level == 2)
        {
            if (roll < 70)
            {
                return basic[Random.Range(0, basic.Length)];
            }

            return intermediate[Random.Range(0, intermediate.Length)];
        }

        if (Level == 3)
        {
            if (roll < 50)
            {
                return basic[Random.Range(0, basic.Length)];
            }

            if (roll < 85)
            {
                return intermediate[Random.Range(0, intermediate.Length)];
            }

            return advanced[Random.Range(0, advanced.Length)];
        }

        return basic[Random.Range(0, basic.Length)];
    }

    private int GetMaxItemsPerOrder()
    {
        return Level switch
        {
            1 => 1,
            2 => 2,
            3 => 3,
            _ => 1
        };
    }
}