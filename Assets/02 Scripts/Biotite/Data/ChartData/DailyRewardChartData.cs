using System;


public enum ERewardType
{
    None,
    Currency,
    Unit,
    Artifact,
}



//일일 출석체크에 대한 보상 데이터 정의입니다.
[Serializable]
public class DailyRewardChartData : IChartData,IDomainConvertible<RewardData>
{
    public string Key { get; set; } 
    public string RewardID { get; set; }
    public string RewardType { get; set; }
    public string RewardAmount { get; set; }

    public RewardData ToDomainData()
    {
        int amount = int.TryParse(RewardAmount, out int value) ? value : 0;
        ERewardType rewardType = Enum.Parse<ERewardType>(RewardType);
        RewardData rewardData = new RewardData(rewardType,RewardID,amount);
        return rewardData;
    }
}


