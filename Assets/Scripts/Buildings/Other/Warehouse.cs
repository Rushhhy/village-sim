using UnityEngine;

public class Warehouse : Building
{
    [Header("Storage Capacity")]
    [SerializeField] private int capacityLevelOne = 100;
    [SerializeField] private int capacityLevelTwo = 200;
    [SerializeField] private int capacityLevelThree = 300;

    private ResourceManager resourceManager;

    protected override void Awake()
    {
        base.Awake();
        width = 2;

        resourceManager = GameObject.Find("ResourceManager").GetComponent<ResourceManager>();
    }

    public int GetCapacityContribution()
    {
        if (State != BuildingState.Active)
        {
            return 0;
        }

        return Level switch
        {
            1 => capacityLevelOne,
            2 => capacityLevelTwo,
            3 => capacityLevelThree,
            _ => 0
        };
    }

    public override void UpgradeBuilding()
    {
        base.UpgradeBuilding();
        SetBuildingNameByLevel("Warehouse");
        RecalculateTotalCapacity();
    }

    public override void FinishConstruction()
    {
        base.FinishConstruction();
        RecalculateTotalCapacity();
    }

    private void OnDestroy()
    {
        if (resourceManager != null)
        {
            RecalculateTotalCapacity();
        }
    }

    public void RecalculateTotalCapacity()
    {
        Warehouse[] warehouses = FindObjectsOfType<Warehouse>();

        int totalCapacity = resourceManager.baseCapacity;

        foreach (Warehouse warehouse in warehouses)
        {
            totalCapacity += warehouse.GetCapacityContribution();
        }

        resourceManager.maxCapacity = totalCapacity;
    }
}