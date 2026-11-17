using System;


public class UnitUpgradeData
{
    public UnitUpgradeData(int requiredSoulStone,int requiredGold)
    {
        RequiredSoulStone = requiredSoulStone;
        RequiredGold = requiredGold;
    }

    public int RequiredSoulStone { get; private set; }
    public int RequiredGold { get; private set; }
}

//유닛 레벨 업그레이드에 대한 레벨 데이터 입니다.
[Serializable]
public class UnitLevelChartData : IChartData, IDomainConvertible<UnitUpgradeData>
{
    public string Key { get; set; }
    public string RequiredSoulStone { get; set; }
    public string RequiredGold { get; set; }
    
    
    public UnitUpgradeData ToDomainData()
    {
        int requiredSoulStone = int.TryParse(RequiredSoulStone, out int val) ? val : 0;
        int requiredGold = int.TryParse(RequiredGold, out val) ? val : 0;
        UnitUpgradeData unitUpgradeData = new UnitUpgradeData(requiredSoulStone, requiredGold);
        return unitUpgradeData;   
    }
}
