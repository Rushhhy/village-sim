using System.Collections.Generic;
using UnityEngine;

public class Workshop : ProductionBuilding
{
    protected override void Awake()
    {
        base.Awake();

        workPositionsOne = new List<Vector3>() { new Vector3(0.95f, 0.625f, 0) };
        workPositionsTwo = new List<Vector3>() { new Vector3(0.77f, 0.714f, 0) };
        workPositionsThree = new List<Vector3>() { new Vector3(0.655f, 1.1f, 0f), new Vector3(0.655f, 0.53f, 0f) };
        workPositions = workPositionsOne;

        inputMethodBase = new int[] { 0, 0 };
        inputMethodOne = new int[] { 1, 1 };
        inputMethodTwo = new int[] { 2, 2 };
        inputMethodThree = new int[] { 3, 3 };

        outputMethodBase = new int[] { 0 };
        outputMethodOne = new int[] { 2 };
        outputMethodTwo = new int[] { 3 };
        outputMethodThree = new int[] { 4 };

        NeededResourcesID = new int[] { 1, 4 };
        ProducedResourcesID = new int[] { 2 };
        ResourceProductionTime = new float[] { 10f };

        ProductionType = 2;
        width = 3;
    }

    public override void UpgradeBuilding()
    {
        base.UpgradeBuilding();
        SetBuildingNameByLevel("Workshop");
    }
}
