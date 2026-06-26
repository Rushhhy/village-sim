public class Bakery : ProductionBuilding
{
    protected override void Awake()
    {
        base.Awake();

        inputMethodBase = new int[] { 0 };
        inputMethodOne = new int[] { 1 };
        inputMethodTwo = new int[] { 2 };
        inputMethodThree = new int[] { 3 };

        outputMethodBase = new int[] { 0 };
        outputMethodOne = new int[] { 1 };
        outputMethodTwo = new int[] { 2 };
        outputMethodThree = new int[] { 3 };

        NeededResourcesID = new int[] { 8 };
        ProducedResourcesID = new int[] { 9 };
        ResourceProductionTime = new float[] { 10f };

        ProductionType = 1;
        width = 2;
    }

    public override void UpgradeBuilding()
    {
        base.UpgradeBuilding();
        SetBuildingNameByLevel("Bakery");
    }
}
