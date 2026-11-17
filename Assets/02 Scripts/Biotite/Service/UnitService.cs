using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BackEnd;
using UnityEditor.Experimental;
using UnityEngine;


//유닛에 대한 처리 서비스 로직 입니다.
public class UnitService : IService
{
    private UserServerUnitData userServerUnitData;
    private CurrencyService currencyService;
    
    public void Initialize()
    {
        userServerUnitData = GlobalManagers.UserServerData.GetData<UserServerUnitData>();
        currencyService = GlobalManagers.Service.GetService<CurrencyService>();
        BindEvents();
    }

    public void Release()
    {
        GlobalManagers.Event.RemoveEvent<OnGachaFinishedEventData>(OnGachaFinishedEventHandler);
    }


    private void BindEvents()
    {
        GlobalManagers.Event.AddEvent<OnGachaFinishedEventData>(OnGachaFinishedEventHandler);
    }

    
    private void OnGachaFinishedEventHandler(OnGachaFinishedEventData e)
    {
        bool res = userServerUnitData.SaveData();
    }
    
    
    
   
    /// <summary>
    /// 유저가 가지고 있는 아티팩트 데이터의 정보를 가져옵니다.
    /// </summary>
    /// <returns></returns>
    public IReadOnlyList<KeyValuePair<string, UnitDTO>> GetUserUnits()
    {
        return userServerUnitData.UserUnits.ToList(); 
    }



        
    /// <summary>
    /// 소유하고있는 유닛인지 확인 합니다.
    /// </summary>
    /// <param name="unitID"></param>
    /// <returns></returns>
    public bool IsOwned(string unitID)
        => userServerUnitData.UserUnits.ContainsKey(unitID);



    /// <summary>
    /// id에 해당하는 유저데이터를 불러옵니다.
    /// </summary>
    /// <param name="unitID"></param>
    /// <returns></returns>
    public UnitDTO GetUserUnitData(string unitID)
    {
        if (userServerUnitData.UserUnits.TryGetValue(unitID, out UnitDTO unitDTO))
            return unitDTO;

        return null;
    }


    /// <summary>
    /// 유닛 레벨을 업그레이드 하고 서버에 저장합니다.
    /// </summary>
    /// <param name="unitID"></param>
    /// <returns></returns>
    public async UniTask<bool> UpgradeUnit(string unitID)
    {
        if (!IsOwned(unitID)) return false;

        UnitDTO unitDto = GetUserUnitData(unitID);

        if (unitDto.Level > GlobalDefines.MAX_VALUE.UNIT_MAX_LEVEL)
            return false;
        
        int prevGold = currencyService.Gold;
        UnitUpgradeData unitUpgradeData = GlobalManagers.Chart.GetUnitLevelChartData(unitDto.Level);
        int costGold = unitUpgradeData.RequiredGold;

        int soulStone = unitDto.SoulStoneCount;
        int costStone = unitUpgradeData.RequiredSoulStone;

        if (GlobalDefines.MAX_VALUE.UNIT_MAX_LEVEL <= unitDto.Level) return false;
        if (prevGold < costGold || soulStone < costStone)
            return false;

        currencyService.SpendGold(costGold);

        UnitDTO prevDto = new UnitDTO(unitDto.Level, soulStone);
        UnitDTO newUnitDto = new UnitDTO(unitDto.Level + 1, Mathf.Max(soulStone - costStone, 0));
        userServerUnitData.UpdateUnit(unitID, newUnitDto);

        bool isSuccess = await userServerUnitData.SaveDataAsync();

        if (isSuccess)
        {
            Logger.Log("승급 성공");
            GlobalManagers.Event.NotifyEvent(new OnUnitUpgradeEventData
            {
                UnitID = unitID
            });
            
            GlobalManagers.Event.NotifyEvent<OnDoDailyQuestEventData>(new OnDoDailyQuestEventData()
            {
                QuestType = EQuestType.UpgradeUnit
            });
            
            GlobalManagers.Audio.PlaySfx(GlobalDefines.ResourceKey.Audio.ARTIFACT_UPGRADE_SFX);
            return true;
        }
        
        GlobalManagers.Event.NotifyEvent<OnBackEndErrorEventData>(new OnBackEndErrorEventData()
        {
            ErrorType = EBackEndErrorType.RequestFailed
        });
        
        Logger.LogError("승급 실패");
        currencyService.AddGold(costGold);
        userServerUnitData.UpdateUnit(unitID, prevDto);
        return false;
    }
    
    
    /// <summary>
    /// 특정 id의 유닛의 정보를 갱신합니다.
    /// </summary>
    /// <param name="unitID"></param>
    /// <param name="unitDto"></param>
    public void UpdateUnit(string unitID, UnitDTO unitDto)
        => userServerUnitData.UpdateUnit(unitID, unitDto);

    /// <summary>
    /// 새로운 유닛을 추가합니다.
    /// </summary>
    /// <param name="unitID"></param>
    /// <param name="unitDto"></param>
    public void AddNewUnit(string unitID, UnitDTO unitDto)
        => userServerUnitData.AddUnit(unitID, unitDto);


    /// <summary>
    /// 현재 데이터를 저장합니다.
    /// </summary>
    public async UniTask<bool> SaveData()
    {
        bool res = await userServerUnitData.SaveDataAsync();
        return res;
    }
    
    
    /// <summary>
    /// 스냅샷을 만들어 놓습니다.
    /// </summary>
    /// <returns></returns>
    public Dictionary<string, UnitDTO> CreateSnapshot() 
        => new Dictionary<string, UnitDTO>(userServerUnitData.UserUnits);
    
    
    /// <summary>
    /// 스냅샷으로 롤백 합니다.
    /// </summary>
    /// <param name="snapshot"></param>
    public void RestoreSnapshot(Dictionary<string, UnitDTO> snapshot)
    {
        foreach (var snap in snapshot)
        {
            UpdateUnit(snap.Key,  snap.Value);
        }
    }


    /// <summary>
    /// 파라마터 값을 반환합니다.
    /// </summary>
    /// <returns></returns>
    public Param GetParam()
        => userServerUnitData.ToParam();

    /// <summary>
    /// 유닛 데이터의 indate를 가져옵니다.
    /// </summary>
    /// <returns></returns>
    public string GetInDate()
        => userServerUnitData.InDate;
}
