using System.Collections.Generic;
using UnityEngine;

public class Barracks : Building
{
    protected override void Awake()
    {
        base.Awake();

        workPositionsOne = new List<Vector3>() { new Vector3(1.03f, 0.625f, 0) };
        workPositionsTwo = new List<Vector3>() { new Vector3(0.575f, 0.635f, 0), new Vector3(2.43f, 0.625f, 0) };
        workPositionsThree = new List<Vector3>() { new Vector3(0.52f, 1.05f, 0f), new Vector3(2.43f, 1.05f, 0f), new Vector3(0.6f, 0.254f, 0f), new Vector3(2.43f, 0.254f, 0f) };
        workPositions = workPositionsOne;
        width = 3;
    }
    public override void UpgradeBuilding()
    {
        base.UpgradeBuilding();
        SetBuildingNameByLevel("Barracks");
    }
}
