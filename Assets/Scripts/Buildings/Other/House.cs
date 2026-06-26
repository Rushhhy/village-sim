public class House : Building
{
    protected override bool IsHouse => true;

    protected override void Awake()
    {
        base.Awake();
        width = 2;
    }

    public override void UpgradeBuilding()
    {
        base.UpgradeBuilding();
        SetBuildingNameByLevel("House");
    }
}