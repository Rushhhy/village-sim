public class Stables : Building
{
    protected override void Awake()
    {
        base.Awake();
        width = 3;
    }
    public override void UpgradeBuilding()
    {
        base.UpgradeBuilding();
        SetBuildingNameByLevel("Stables");
    }
}
