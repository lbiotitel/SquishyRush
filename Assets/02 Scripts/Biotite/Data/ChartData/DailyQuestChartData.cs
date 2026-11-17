
using System;

public enum EQuestType
{
    GameStart,
    AttendanceCheck,
    Gacha,
    ShowRanking,
    UpgradeUnit,
    UpgradeArtifact
}


public class DailyQuestData
{
    public DailyQuestData(string questTitleKey, EQuestType questType, int goal,
        RewardData rewardData)
    {
        QuestTitleKey = questTitleKey;
        QuestType = questType;
        Goal = goal;
        RewardData = rewardData;
    }
    
    public string QuestTitleKey { get; private set; }
    public EQuestType QuestType { get; private set; }
    public int Goal { get; private set; }
    public RewardData RewardData  { get; private set; }
}

//일일 퀘스트에 대한 차트데이터 정의입니다.
[Serializable]
public class DailyQuestChartData : IChartData, IDomainConvertible<DailyQuestData>
{
    public string Key { get; set; }
    public string QuestTitleKey { get; set; }
    public string QuestType { get; set; }
    public string Goal { get; set; }
    public string RewardType { get; set; }
    public string RewardID { get; set; }
    public string RewardAmount { get; set; }
    
    
    
    public DailyQuestData ToDomainData()
    {
        int goal = int.TryParse(Goal, out int g) ? g : 0;
        EQuestType questType = Enum.Parse<EQuestType>(QuestType);
        
        ERewardType rewardType = Enum.Parse<ERewardType>(RewardType);
        int rewardAmount = int.TryParse(RewardAmount, out int r) ? r : 0;
        
        RewardData rewardData = new RewardData(rewardType, RewardID, rewardAmount);
        
        DailyQuestData dailyQuestData = new DailyQuestData(
            QuestTitleKey,questType,goal,rewardData);
        
        return  dailyQuestData;
    }
}