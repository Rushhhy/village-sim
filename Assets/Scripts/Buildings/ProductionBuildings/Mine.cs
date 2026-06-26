using System.Collections.Generic;
using UnityEngine;

public class Mine : ProductionBuilding
{
    protected override void Awake()
    {
        base.Awake();

        workPositions = new List<Vector3>() { new Vector3(0, 1.1f, 0), new Vector3(3.05f, 1.1f, 0) };

        inputMethodBase = new int[] { };
        inputMethodOne = new int[] { };
        inputMethodTwo = new int[] { };
        inputMethodThree = new int[] { };

        outputMethodBase = new int[] { 0, 0 };
        outputMethodOne = new int[] { 1, 1 };
        outputMethodTwo = new int[] { 2, 2 };
        outputMethodThree = new int[] { 3, 3 };

        NeededResourcesID = new int[] { };
        ProducedResourcesID = new int[] { 3, 4 };
        ResourceProductionTime = new float[] { 10f, 10f };

        ProductionType = 3;
        width = 3;
    }

    protected override void StartBuildOrUpgrade(int level)
    {
        base.StartBuildOrUpgrade(level);

    }

    public override void UpgradeBuilding()
    {
        base.UpgradeBuilding();
        SetBuildingNameByLevel("Mine");
    }

    public override void FinishConstruction()
    {
        base.FinishConstruction();
    }
}
