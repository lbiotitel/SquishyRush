using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;


//일일 퀘스트에 대한 로직 정의입니다.
public class DailyQuestService : IService
{
    private RewardService rewardService;
    private ProfileService profileService;
    private UserServerDailyQuestData  userServerDailyQuestData;
    private IReadOnlyDictionary<string, DailyQuestData> dailyQuestDatas;
    
    public void Initialize()
    {
        rewardService = GlobalManagers.Service.GetService<RewardService>();
        profileService = GlobalManagers.Service.GetService<ProfileService>();
       
        userServerDailyQuestData = GlobalManagers.UserServerData.GetData<UserServerDailyQuestData>();
        dailyQuestDatas = GlobalManagers.Chart.GetDailyQuestDatas();

        CheckDay();
        BindEvent();
    }

    public void Release()
    {
        GlobalManagers.Event?.RemoveEvent<OnDoDailyQuestEventData>(OnDoDailyQuestEventHandler);
    }

    private void BindEvent()
    {
        GlobalManagers.Event?.AddEvent<OnDoDailyQuestEventData>(OnDoDailyQuestEventHandler);
    }

    
    
    /// <summary>
    /// 하루가 지났는지 체크하고 하루가 지나면 퀘스트 데이터를 초기화 시킵니다.
    /// </summary>
    private void CheckDay()
    {
        DateTime now = DateTime.UtcNow;
        DateTime lastLoginTime = profileService.LastSavedTime;
        
        if (now.Day != lastLoginTime.Day)
        {
            userServerDailyQuestData.SetDefaultData();
        }
    }


    /// <summary>
    /// 특정 퀘스트 타입을 진행할 시 퀘스트 dto 데이터를 갱신합니다.
    /// </summary>
    /// <param name="e"></param>
    private void OnDoDailyQuestEventHandler(OnDoDailyQuestEventData e)
    {
        foreach (var userDailyQuest in userServerDailyQuestData.UserDailyQuest.ToList())
        {
            UserDailyQuestDTO dailyQuestDto = userDailyQuest.Value;
            if (dailyQuestDto.QuestState == EQuestState.Doing)
            {
                DailyQuestData dailyQuestData = dailyQuestDatas[userDailyQuest.Key];
                
                if (dailyQuestData.QuestType == e.QuestType)
                {
                    EQuestState questState = EQuestState.Doing;
                    
                    if (dailyQuestDto.Progress + 1 >= dailyQuestData.Goal)
                        questState = EQuestState.Completed;
                    
                    UserDailyQuestDTO newDailyQuestDto = new  UserDailyQuestDTO(questState, dailyQuestDto.Progress + 1);
                    
                    userServerDailyQuestData.UpdateQuest(userDailyQuest.Key, newDailyQuestDto);
                    
                    GlobalManagers.Event.NotifyEvent<OnDailyQuestStateChangedEventData>(new OnDailyQuestStateChangedEventData()
                    {
                        QuestID = userDailyQuest.Key,
                        QuestState = questState
                    });
                }
            }
        }
    }



    /// <summary>
    /// 퀘스트가 완료됬는지 확인 한 후 퀘스트 완료와 동시에 보상을 지급합니다.
    /// </summary>
    /// <param name="questID"></param>
    public async UniTaskVoid ClaimQuestReward(string questID)
    {
        UserDailyQuestDTO dailyQuestDto = GetDailyQuestDto(questID);
        if (dailyQuestDto == null) return;
        
        if (dailyQuestDatas.TryGetValue(questID, out DailyQuestData dailyQuestData))
        {
            if (dailyQuestDto.QuestState != EQuestState.Completed) return;
            
            //보상 지급 후 dto 데이터 업데이트
            UserDailyQuestDTO backUpDailyQuestDto = userServerDailyQuestData.GetQuestDto(questID);
            UserDailyQuestDTO newDailyQuestDto = new UserDailyQuestDTO(EQuestState.Claimed, dailyQuestDto.Progress);
            
            userServerDailyQuestData.UpdateQuest(questID, newDailyQuestDto);
            rewardService.GiveReward(dailyQuestData.RewardData);

            bool isSuccess = await userServerDailyQuestData.SaveDataAsync();
            
            if (!isSuccess)
            {
                Logger.LogError("퀘스트 데이터 저장 실패");
                userServerDailyQuestData.UpdateQuest(questID, backUpDailyQuestDto);
                rewardService.RollBackReward(dailyQuestData.RewardData);
                
                GlobalManagers.Event.NotifyEvent<OnBackEndErrorEventData>(new OnBackEndErrorEventData()
                {
                    ErrorType = EBackEndErrorType.RequestFailed
                });
                return;
            }
            
            
            GlobalManagers.Event.NotifyEvent<OnDailyQuestStateChangedEventData>(new OnDailyQuestStateChangedEventData()
            {
                QuestID = questID
            });
            Logger.Log("퀘스트 데이터 저장 완료");
        }
    }


    /// <summary>
    /// QuestID에 해당하는 DTO 데이터를 가져옵니다.
    /// </summary>
    /// <param name="questID"></param>
    /// <returns></returns>
    public UserDailyQuestDTO GetDailyQuestDto(string questID)
    {
        if (userServerDailyQuestData.UserDailyQuest.TryGetValue(questID, out UserDailyQuestDTO dailyQuestDto))
        {
            return dailyQuestDto;
        }

        return null;
    }
}
