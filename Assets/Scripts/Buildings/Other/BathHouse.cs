using System.Collections.Generic;
using UnityEngine;
public class BathHouse : Building
{
    protected override void Awake()
    {
        base.Awake();
        workPositionsOne = new List<Vector3>() { new Vector3(1.08f, 0.83f, 0) };
        workPositionsTwo = new List<Vector3>() { new Vector3(0.6f, 0.83f, 0), new Vector3(2.395f, 0.83f, 0) };
        workPositionsThree = new List<Vector3>() { new Vector3(0.58f, 0.9f, 0), new Vector3(2.38f, 0.9f, 0), new Vector3(0.58f, 0.4f, 0), new Vector3(2.38f, 0.4f, 0) };
        workPositions = workPositionsOne;
        width = 3;
    }
    public override void UpgradeBuilding()
    {
        base.UpgradeBuilding();
        SetBuildingNameByLevel("Bath House");
    }
}
