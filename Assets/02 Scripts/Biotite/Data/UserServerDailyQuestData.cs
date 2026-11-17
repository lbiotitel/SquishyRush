using System;
using System.Collections;
using System.Collections.Generic;
using BackEnd;
using Cysharp.Threading.Tasks;
using LitJson;
using UnityEngine;



//유저의 일일퀘스트 데이터 입니다.
public class UserServerDailyQuestData : UserServerDataBase
{
    private DataDictionary<string, UserDailyQuestDTO> userDailyQuest;
    public IReadOnlyDictionary<string, UserDailyQuestDTO> UserDailyQuest => userDailyQuest.Map;


    public override void Initialize()
    {
        tableId = GlobalDefines.DataTableName.USER_DAILY_QUEST_TABLE;
    }

    /// <summary>
    /// 새로운 퀘스트를 추가합니다.
    /// </summary>
    /// <param name="questId"></param>
    /// <param name="userDailyQuestDto"></param>
    public void AddQuest(string questId, UserDailyQuestDTO userDailyQuestDto)
    {
        if (!userDailyQuest.ContainsKey(questId))
        {
            IsDirty = true;
            userDailyQuest.Add(questId, userDailyQuestDto);
            return;
        }
        
        Logger.LogError($"{questId} 중복된 키를 가진 퀘스트 입니다.");
    }

    /// <summary>
    /// 특정 퀘스트의 데이터를 변경 합니다.
    /// </summary>
    /// <param name="questId"></param>
    /// <param name="dailyQuestDto"></param>
    public void UpdateQuest(string questId, UserDailyQuestDTO newDailyQuestDto)
    {
        if (userDailyQuest.TryGetValue(questId, out UserDailyQuestDTO dailyQuestDto))
        {
            IsDirty = true;
            userDailyQuest.UpdateData(questId, newDailyQuestDto);
            return;
        }
        
        Logger.LogError($"{questId} 에 해당하는 퀘스트 가 없습니다.");
    }
    
    
    /// <summary>
    /// questid에 해당하는 퀘스트를 삭제합니다.
    /// </summary>
    /// <param name="questId"></param>
    public void RemoveQuest(string questId)
    {
        if (userDailyQuest.TryGetValue(questId, out UserDailyQuestDTO dailyQuestDto))
        {
            IsDirty = true;
            userDailyQuest.Remove(questId);
            return;
        }
        
        Logger.LogError($"{questId} 에 해당하는 퀘스트 가 없습니다.");
    }

    
    /// <summary>
    /// 퀘스트 id에 해당하는 퀘스트 dto를 가져옵니다.
    /// </summary>
    /// <param name="questId"></param>
    /// <returns></returns>
    public UserDailyQuestDTO GetQuestDto(string questId)
    {
        if (userDailyQuest.TryGetValue(questId, out UserDailyQuestDTO dailyQuestDto))
            return dailyQuestDto;
        return null;
    }
    
    

    protected override void ParsingData(JsonData data)
    {
        // inDate
        if (data.Keys.Contains("inDate") && data["inDate"] != null)
            InDate = data["inDate"].ToString();

        // DailyQuests
        if (data.Keys.Contains("DailyQuests") && data["DailyQuests"] != null)
        {
            string jsonString = data["DailyQuests"].ToString();
            if (!string.IsNullOrEmpty(jsonString))
            {
                Logger.Log(jsonString);
                userDailyQuest = DataDictionary<string, UserDailyQuestDTO>.DeserializeObject(jsonString);
            }
        }
    }


    public override void SetDefaultData()
    {
        userDailyQuest = new DataDictionary<string, UserDailyQuestDTO>();
        
        //여기서 차트 데이터 가져와서 한번 싹 로드해서 데이터 생성하면 될듯    
        IReadOnlyDictionary<string, DailyQuestData> dailyQuestDatas = GlobalManagers.Chart.GetDailyQuestDatas();

        foreach (var dailyQuest in dailyQuestDatas)
        {
            UserDailyQuestDTO dailyQuestDto = new UserDailyQuestDTO(EQuestState.Doing, 0);
            AddQuest(dailyQuest.Key, dailyQuestDto);
        }
    }


    public override Param ToParam()
    {
        Param param = new Param();
        param.Add("DailyQuests", userDailyQuest.SerializeObject());
        return param;
    }
}
