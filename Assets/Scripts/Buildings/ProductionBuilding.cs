using UnityEngine;

public class ProductionBuilding : Building
{
    public int[] ProductionSpeedLevel { get; protected set; }
    public int ProductionType;

    public int[] NeededResourcesID { get; protected set; }
    public int[] NeededResourcesAmount { get; protected set; }
    public int[] ProducedResourcesID { get; protected set; }
    public int[] ProducedResourcesAmount { get; protected set; }

    public float[] ResourceProductionTime { get; protected set; }

    public float[] ProductionPerSec { get; protected set; }
    public float[] ConsumptionPerSec { get; protected set; }

    public float[] individualTimers { get; protected set; }

    protected ResourceManager resourceManager;
    protected ProductionRegistryManager productionRegistryManager;

    protected int[] inputMethodBase;
    protected int[] inputMethodOne;
    protected int[] inputMethodTwo;
    protected int[] inputMethodThree;

    protected int[] outputMethodBase;
    protected int[] outputMethodOne;
    protected int[] outputMethodTwo;
    protected int[] outputMethodThree;

    private int numVillagersAssigned = 0;

    [SerializeField] private ResourceSO resources;
    protected override void Awake()
    {
        base.Awake();

        ProductionSpeedLevel = new[] { 1, 1 };

        resourceManager = GameObject.Find("ResourceManager").GetComponent<ResourceManager>();
        productionRegistryManager = GameObject.Find("ProductionRegistryManager").GetComponent<ProductionRegistryManager>();
    }

    protected void Start()
    {
        InitializeProduction();
        EnsureTimerArrayMatchesOutputs();
    }

    protected override void Update()
    {
        base.Update();
        ProduceResource();
    }


    public void InitializeProduction()
    {
        if (NeededResourcesAmount == null)
        {
            NeededResourcesAmount = inputMethodBase;
        }

        if (ProducedResourcesAmount == null)
        {
            ProducedResourcesAmount = outputMethodBase;
        }
    }

    protected void ProduceResource()
    {
        if (ProducedResourcesID == null || ProducedResourcesAmount == null || ResourceProductionTime == null)
        {
            return;
        }

        if (State != BuildingState.Active)
        {
            return;
        }

        if (numVillagersAssigned == 0)
        {
            return;
        }

        EnsureTimerArrayMatchesOutputs();

        for (int i = 0; i < ProducedResourcesID.Length; i++)
        {
            individualTimers[i] += Time.deltaTime;

            float productionTime = GetProductionTimeForIndex(i);

            if (individualTimers[i] < productionTime)
            {
                continue;
            }

            if (CanProduce(i))
            {
                ConsumeNeededResourcesIfAny();
                ProduceOutputResource(i);
            }

            individualTimers[i] = 0f;
        }
    }

    public override void ClearVillagers()
    {
        base.ClearVillagers();
        numVillagersAssigned = 0;
        UpdateProductionMethod();
    }

    public void UpdateProductionMethod()
    {
        ResetProductionRates();

        switch (numVillagersAssigned)
        {
            case 0:
                NeededResourcesAmount = inputMethodBase;
                ProducedResourcesAmount = outputMethodBase;
                break;
            case 1:
                NeededResourcesAmount = inputMethodOne;
                ProducedResourcesAmount = outputMethodOne;
                break;
            case 2:
                NeededResourcesAmount = inputMethodTwo;
                ProducedResourcesAmount = outputMethodTwo;
                break;
            case 3:
                NeededResourcesAmount = inputMethodThree;
                ProducedResourcesAmount = outputMethodThree;
                break;
        }

        EnsureTimerArrayMatchesOutputs();
        UpdateProductionMethodRates();
        buildingRegistryManager.UpdateProductionMethodsUI(this);
    }

    public void UpgradeProductionSpeed(int productionIndex)
    {
        if (ResourceProductionTime == null || productionIndex < 0 || productionIndex >= ResourceProductionTime.Length)
        {
            return;
        }

        ResetProductionRates();
        ProductionSpeedLevel[productionIndex]++;
        ResourceProductionTime[productionIndex] *= 0.99f;
        UpdateProductionMethodRates();
    }

    public override void UpgradeBuilding()
    {
        base.UpgradeBuilding();
    }

    public override void AssignVillagerToSlot(int slot, Villager villager)
    {
        base.AssignVillagerToSlot(slot, villager);
        numVillagersAssigned++;
        UpdateProductionMethod();
    }

    public override void RemoveVillagerFromSlot(int slot)
    {
        base.RemoveVillagerFromSlot(slot);
        numVillagersAssigned--;
        UpdateProductionMethod();
    }

    private bool CanProduce(int outputIndex)
    {
        bool enoughInputs = HasEnoughInputs();
        bool enoughCapacity = HasEnoughCapacityForOutput(outputIndex);

        Debug.Log($"[TEST] CanProduce check on {gameObject.name} | enoughInputs={enoughInputs} | enoughCapacity={enoughCapacity}");

        if (!enoughInputs)
        {
            return false;
        }

        return enoughCapacity;
    }

    private bool HasEnoughInputs()
    {
        if (NeededResourcesID == null || NeededResourcesAmount == null || NeededResourcesID.Length == 0)
        {
            return true;
        }

        for (int i = 0; i < NeededResourcesID.Length; i++)
        {
            if (NeededResourcesAmount[i] > resourceManager.resourceTotals[NeededResourcesID[i]])
            {
                return false;
            }
        }

        return true;
    }

    private bool HasEnoughCapacityForOutput(int outputIndex)
    {
        if (ProducedResourcesID == null || ProducedResourcesAmount == null)
        {
            Debug.Log("[TEST] Produced resource arrays are null");
            return false;
        }

        if (outputIndex < 0 || outputIndex >= ProducedResourcesID.Length || outputIndex >= ProducedResourcesAmount.Length)
        {
            Debug.Log("[TEST] Output index out of range");
            return false;
        }

        int producedAmount = ProducedResourcesAmount[outputIndex];
        bool canStore = resourceManager.CanStoreAmount(producedAmount);

        Debug.Log($"[TEST] Capacity check on {gameObject.name} | amount={producedAmount} | used={resourceManager.GetUsedCapacity()} | max={resourceManager.GetMaxCapacity()} | canStore={canStore}");

        return canStore;
    }

    private void ConsumeNeededResourcesIfAny()
    {
        if (NeededResourcesID == null || NeededResourcesAmount == null || NeededResourcesID.Length == 0)
        {
            return;
        }

        for (int i = 0; i < NeededResourcesID.Length; i++)
        {
            resourceManager.resourceTotals[NeededResourcesID[i]] -= NeededResourcesAmount[i];
            productionRegistryManager.UpdateTotalOfResourceWithID(NeededResourcesID[i]);
        }

        if (ProductionType == 3 && NeededResourcesID.Length > 0)
        {
            resourceManager.resourceTotals[NeededResourcesID[0]] += 1;
            productionRegistryManager.UpdateTotalOfResourceWithID(NeededResourcesID[0]);
        }

        buildingRegistryManager.RefreshAllWarehouseOverviews();
    }

    private void ProduceOutputResource(int outputIndex)
    {
        int resourceID = ProducedResourcesID[outputIndex];
        int amount = ProducedResourcesAmount[outputIndex];

        bool added = resourceManager.TryAddResource(resourceID, amount);

        if (!added)
        {
            Debug.LogWarning($"[POPUP] Resource add failed on {gameObject.name}. ResourceID={resourceID}, Amount={amount}");
            return;
        }

        productionRegistryManager.UpdateTotalOfResourceWithID(resourceID);
        buildingRegistryManager.RefreshAllWarehouseOverviews();

        if (resources == null || resources.resourcesData == null || resourceID < 0 || resourceID >= resources.resourcesData.Count)
        {
            Debug.LogWarning($"[POPUP] ResourceSO missing or invalid resourceID on {gameObject.name}. ResourceID={resourceID}");
            return;
        }

        Sprite producedIcon = resources.resourcesData[resourceID].Icon;

        if (producedIcon == null)
        {
            Debug.LogWarning($"[POPUP] Produced icon is null on {gameObject.name}. ResourceID={resourceID}");
            return;
        }

        Vector3 popupPosition = GetResourcePopupPosition() + new Vector3(outputIndex * 0.4f, 0f, 0f);

        Debug.Log($"[POPUP] Spawning popup on {gameObject.name} at {popupPosition} for resourceID={resourceID}");

        ShowProducedResourcePopup(producedIcon, popupPosition);
    }

    private void ResetProductionRates()
    {
        if (ResourceProductionTime == null)
        {
            return;
        }

        if (NeededResourcesID != null && NeededResourcesAmount != null)
        {
            for (int i = 0; i < NeededResourcesID.Length; i++)
            {
                float productionTime = GetProductionTimeForIndex(i);
                resourceManager.resourceConsumptionTotals[NeededResourcesID[i]] -=
                    (float)NeededResourcesAmount[i] / productionTime;
            }
        }

        if (ProducedResourcesID != null && ProducedResourcesAmount != null)
        {
            for (int i = 0; i < ProducedResourcesID.Length; i++)
            {
                float productionTime = GetProductionTimeForIndex(i);
                resourceManager.resourceProductionTotals[ProducedResourcesID[i]] -=
                    (float)ProducedResourcesAmount[i] / productionTime;
            }
        }
    }

    private void UpdateProductionMethodRates()
    {
        if (ResourceProductionTime == null)
        {
            return;
        }

        if (NeededResourcesID != null && NeededResourcesAmount != null)
        {
            for (int i = 0; i < NeededResourcesID.Length; i++)
            {
                float productionTime = GetProductionTimeForIndex(i);

                resourceManager.resourceConsumptionTotals[NeededResourcesID[i]] +=
                    (float)NeededResourcesAmount[i] / productionTime;

                productionRegistryManager.UpdateConsumptionRateOfResourceWithID(NeededResourcesID[i]);
            }
        }

        if (ProducedResourcesID != null && ProducedResourcesAmount != null)
        {
            for (int i = 0; i < ProducedResourcesID.Length; i++)
            {
                float productionTime = GetProductionTimeForIndex(i);

                resourceManager.resourceProductionTotals[ProducedResourcesID[i]] +=
                    (float)ProducedResourcesAmount[i] / productionTime;

                productionRegistryManager.UpdateProductionRateOfResourceWithID(ProducedResourcesID[i]);
            }
        }
    }

    private float GetProductionTimeForIndex(int index)
    {
        if (ResourceProductionTime == null || ResourceProductionTime.Length == 0)
        {
            return 1f;
        }

        if (ResourceProductionTime.Length == 1)
        {
            return Mathf.Max(0.01f, ResourceProductionTime[0]);
        }

        int safeIndex = Mathf.Clamp(index, 0, ResourceProductionTime.Length - 1);
        return Mathf.Max(0.01f, ResourceProductionTime[safeIndex]);
    }

    private void EnsureTimerArrayMatchesOutputs()
    {
        int outputCount = ProducedResourcesID != null && ProducedResourcesID.Length > 0
            ? ProducedResourcesID.Length
            : 1;

        if (individualTimers == null || individualTimers.Length != outputCount)
        {
            individualTimers = new float[outputCount];
        }
    }
    private Vector3 GetResourcePopupPosition()
    {
        return width switch
        {
            4 => transform.position + new Vector3(0f, 2f, 0f),
            3 => transform.position + new Vector3(1f, 1.8f, 0f),
            2 => transform.position + new Vector3(0.5f, 1.5f, 0f),
            _ => transform.position + new Vector3(0.5f, 1.5f, 0f)
        };
    }

}