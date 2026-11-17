using System;



//리워드 데이터 입니다
[Serializable]
public class RewardData
{
    public RewardData(ERewardType rewardType, string rewardID, int amount)
    {
        RewardID = rewardID;
        RewardType = rewardType;
        Amount = amount;
    }

    public ERewardType RewardType { get; private set; }
    public string RewardID { get; private set; }
    public int Amount { get; private set; }
}
