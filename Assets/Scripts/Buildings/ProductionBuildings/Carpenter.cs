using System.Collections.Generic;
using UnityEngine;

public class Carpenter : ProductionBuilding
{
    protected override void Awake()
    {
        base.Awake();

        workPositionsOne = new List<Vector3>() { new Vector3(1.134f, 1.02f, 0)};
        workPositionsTwo = new List<Vector3>() { new Vector3(0.94f, 1.022f, 0)};
        workPositionsThree = new List<Vector3>() { new Vector3(0.95f, 1f, 0f), new Vector3(0.95f, 0.59f, 0f) };
        workPositions = workPositionsOne;

        inputMethodBase = new int[] { 0, 0 };
        inputMethodOne = new int[] { 1, 1 };
        inputMethodTwo = new int[] { 2, 2 };
        inputMethodThree = new int[] { 3, 3 };

        outputMethodBase = new int[] { 0 };
        outputMethodOne = new int[] { 1 };
        outputMethodTwo = new int[] { 2 };
        outputMethodThree = new int[] { 3 };

        NeededResourcesID = new int[] { 5, 2 };
        ProducedResourcesID = new int[] { 11};
        ResourceProductionTime = new float[] { 10f };

        ProductionType = 2;
        width = 3;
    }

    public override void UpgradeBuilding()
    {
        base.UpgradeBuilding();
        SetBuildingNameByLevel("Carpenter");
    }
}
