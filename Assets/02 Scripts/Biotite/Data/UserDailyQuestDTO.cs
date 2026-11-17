using Newtonsoft.Json;


public enum EQuestState
{
    Doing, //하고 있을 때
    Completed, // 완료 했을 때
    Claimed, //보상을 받았을 때
}


//퀘스트 데이터를 클라우드에 저장하기 위한 데이터 입니다.
public class UserDailyQuestDTO
{
    [JsonProperty("QuestState")]
    public EQuestState QuestState { get; private set; }

    [JsonProperty("Progress")]
    public int Progress { get; private set; }
    
    
    [Newtonsoft.Json.JsonConstructor]
    public UserDailyQuestDTO(EQuestState questState, int progress)
    {
        QuestState = questState;
        Progress = progress;
    }
}

