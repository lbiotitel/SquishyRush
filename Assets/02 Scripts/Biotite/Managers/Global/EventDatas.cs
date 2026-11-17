public interface IEventData { }


//씬이 변경됬을 때 발생하는 이벤트 데이터 입니다.
public struct OnSceneChangedEventData : IEventData 
{
    public string SceneName { get; set; }
}



//리소스 데이터들을 로드 완료 했을 때 발생하는 이벤트 입니다.
public struct OnGameResourceDataLoadedEventData : IEventData
{
    public string Key { get; set; }
}

//유저 데이터가 변경되었을 때 발생하는 이벤트 데이터 입니다.
public struct OnUserProfileDataChangedEventData : IEventData { }



//유저의 자원 정보가 바뀌었을 때 호출되는 이벤트 입니다.
public struct OnUserResourceValueChangedEventData : IEventData 
{
    public int prevValue;
    public int currentValue;
    public bool showAnimEffect;
    public EResourceType resourceType;
    public string spriteKey;
}



//모든 데이터가 안정적으로 초기화가 성공됬을 때 발생하는 이벤트입니다.
public struct OnInitFinishedEventData : IEventData { }


//세팅 데이터가 바뀌었을 때 발생하는 이벤트 입니다.
public struct OnSettingDataChangedEventData : IEventData { }


//게임이 종료되기전에 호출되는 이벤트 입니다.
public struct OnBeforeApplicationQuit : IEventData { }


//아티팩트가 승급을 성공할시 호출되는 이벤트 입니다
public struct OnArtifactUpgradeEventData : IEventData
{
    public string ArtifactID;
}

//유닛이 레벨업을 성공할 시 호출되는 이벤트 입니다.
public struct OnUnitUpgradeEventData : IEventData
{
    public string UnitID;
}

//가챠가 성공적으로 끝이 났을 때 호출되는 이벤트 입니다.
public struct OnGachaFinishedEventData : IEventData { }

//가챠 연출이 성공적으로 끝났을때 호출되는 이벤트 입니다.
public struct OnGachaResultAnimFinishedEventData : IEventData { }


//백엔드 에러의 종류 입니다.
public enum EBackEndErrorType
{
    MaintenanceError, //점검
    TooManyRequestError, //너무 많은 요청
    DeviceBlockedError, //차단된 디바이스
    TooManyRequestByLocalError, //너무 많은 요청
    FirstLoadFailed, //초기로드 실패
    RequestFailed, //요청실패
    LoginFailed, //로그인 실패
    ChartDataLoadFailed, // 차트 데이터 로드 실패
    UserDataLoadFailed //유저 데이터 로드 실패
}

//백엔드에서 에러가 날때 호출되는 이벤트 입니다.
public struct OnBackEndErrorEventData : IEventData
{
    public EBackEndErrorType ErrorType { get; set; }
}


//일정주기 마다 데이터를 저장해야할 때 발생하는 이벤트 입니다.
public struct OnDataSaveTimeEventData : IEventData
{
}


//새로운 매일이 왔을때 발생하는 이벤트입니다.
public struct OnNewPostCreatedEventData : IEventData {}

//매일이 전부 로드가 끝났을 때 발생하는 이벤트 이빈다.
public struct OnPostMailLoadedEventData : IEventData
{
    public int MailCount { get; set; }
}


//퀘스트의 상태가 변경됬을때 발생하는 이벤트 입니다.
public struct OnDailyQuestStateChangedEventData : IEventData
{
    public string QuestID { get; set; }
    public EQuestState  QuestState { get; set; }
}


//특정 퀘스트가 수행됬을때 발생하는 이벤트 입니다.
public struct OnDoDailyQuestEventData : IEventData
{
    public EQuestType QuestType { get; set; }
}