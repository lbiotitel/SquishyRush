using System;


public class ArtifactUpgradeData
{
    public ArtifactUpgradeData(int requiredSoulStone, int requiredGold)
    {
        RequiredSoulStone = requiredSoulStone;
        RequiredGold = requiredGold;
    }
    
    public int RequiredSoulStone { get; private set; }
    public int RequiredGold { get; private set; }
}


//아티팩트 레벨업에 관한 차트 데이터 입니다.
[Serializable]
public class ArtifactUpgradeChartData : IChartData, IDomainConvertible<ArtifactUpgradeData>
{
    public string Key { get; set; }
    public string RequiredSoulStone { get; set; }
    public string RequiredGold { get; set; }
    
    public ArtifactUpgradeData ToDomainData()
    {
        int requiredSoulStone = int.TryParse(RequiredSoulStone, out int val) ? val : 0;
        int requiredGold = int.TryParse(RequiredGold, out val) ? val : 0;
        ArtifactUpgradeData artifactUpgradeData = new ArtifactUpgradeData(requiredSoulStone,requiredGold);
        return artifactUpgradeData;
    }
}
