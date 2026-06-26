public class Hospital : Building
{
    protected override void Awake()
    {
        base.Awake();
        width = 2;
    }
    public override void UpgradeBuilding()
    {
        base.UpgradeBuilding();
        SetBuildingNameByLevel("Hospital");
    }
}
