using System.Collections.Generic;
using UnityEngine;

public class Farm : ProductionBuilding
{
    protected override void Awake()
    {
        base.Awake();

        workPositionsOne = new List<Vector3> { new Vector3(1.03f, 0.75f, 0f) };
        workPositionsTwo = new List<Vector3> { new Vector3(0.571f, 0.75f, 0f), new Vector3(2.45f, 0.75f, 0f) }; 
        workPositionsThree = new List<Vector3> { new Vector3(0.6f, 1f, 0f), new Vector3(2.55f, 1f, 0f), new Vector3(0.6f, 0.455f, 0f), new Vector3(2.55f, 0.455f, 0f) };
        workPositions = workPositionsOne;

        inputMethodBase = new int[] { };
        inputMethodOne = new int[] { };
        inputMethodTwo = new int[] { };
        inputMethodThree = new int[] { };

        outputMethodBase = new int[] { 0 };
        outputMethodOne = new int[] { 2 };
        outputMethodTwo = new int[] { 4 };
        outputMethodThree = new int[] { 6 };

        NeededResourcesID = new int[] { };
        ProducedResourcesID = new int[] { 0 };
        ResourceProductionTime = new float[] { 10f };

        ProductionType = 0;
        width = 3;
    }

    protected override void Update()
    {
        base.Update();
        SwitchHarvestSprite();
    }

    public override void UpgradeBuilding()
    {
        base.UpgradeBuilding();
        SetBuildingNameByLevel("Farm");

        if (animator != null)
        {
            animator.SetInteger("Growth", 1);
        }
    }

    private void SwitchHarvestSprite()
    {
        if (animator == null || individualTimers == null || individualTimers.Length == 0 ||
            ResourceProductionTime == null || ResourceProductionTime.Length == 0)
        {
            return;
        }

        float timer = individualTimers[0];
        float productionTime = ResourceProductionTime[0];

        if (timer <= productionTime / 3f)
        {
            animator.SetInteger("Growth", 1);
        }
        else if (timer <= productionTime * 2f / 3f)
        {
            animator.SetInteger("Growth", 2);
        }
        else
        {
            animator.SetInteger("Growth", 3);
        }
    }
}