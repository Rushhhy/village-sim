public class Barn : ProductionBuilding
{
    protected override void Awake()
    {
        base.Awake();

        inputMethodBase = new int[] { 0, 0 };
        inputMethodOne = new int[] { 1, 1 };
        inputMethodTwo = new int[] { 2, 2 };
        inputMethodThree = new int[] { 3, 3 };

        outputMethodBase = new int[] { 0, 0 };
        outputMethodOne = new int[] { 1, 1 };
        outputMethodTwo = new int[] { 2, 2 };
        outputMethodThree = new int[] { 3, 3 };

        NeededResourcesID = new int[] { 0, 0 };
        ProducedResourcesID = new int[] { 6, 7 };
        ResourceProductionTime = new float[] { 10f, 10f };

        ProductionType = 3;
        width = 3;
    }

    public override void UpgradeBuilding()
    {
        base.UpgradeBuilding();
        SetBuildingNameByLevel("Barn");
    }
}
