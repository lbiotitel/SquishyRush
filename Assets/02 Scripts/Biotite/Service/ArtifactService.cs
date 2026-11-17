using System.Collections.Generic;
using System.Linq;
using BackEnd;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;



//아티팩트에 필요한 로직 클래스 입니다.
public class ArtifactService : IService
{
    private UserServerArtifactData userServerArtifactData;
    private CurrencyService currencyService;
    
    public void Initialize()
    {
        userServerArtifactData = GlobalManagers.UserServerData.GetData<UserServerArtifactData>();
        currencyService = GlobalManagers.Service.GetService<CurrencyService>();
    }


    public void Release()
    {
        
    }
    
    
    
    /// <summary>
    /// 유저가 가지고 있는 아티팩트 데이터의 정보를 가져옵니다.
    /// </summary>
    /// <returns></returns>
    public IReadOnlyList<KeyValuePair<string, ArtifactDTO>> GetUserArtifacts()
        => userServerArtifactData.UserArtifacts.ToList(); 
    
    
    
    /// <summary>
    /// 현재 아티팩트 ID가 유저가 가지고 있는지 확인합니다.
    /// </summary>
    /// <returns></returns>
    public bool IsOwned(string artifactID)
        => userServerArtifactData.UserArtifacts.ContainsKey(artifactID);


    
    /// <summary>
    /// 아티팩트 데이터를 갱신 합니다.
    /// </summary>
    /// <param name="artifactID"></param>
    /// <param name="artifactDto"></param>
    public void UpdateArtifact(string artifactID, ArtifactDTO artifactDto)
        => userServerArtifactData.UpdateArtifact(artifactID, artifactDto);

    
    /// <summary>
    /// 새로운 아티팩트 데이터를 추가합니다.
    /// </summary>
    /// <param name="artifactID"></param>
    /// <param name="artifactDto"></param>
    public void AddArtifact(string artifactID, ArtifactDTO artifactDto)
        => userServerArtifactData.AddArtifact(artifactID, artifactDto);
    
    
    
    /// <summary>
    /// 저당된 아티팩트의 lv 과 같은 데이터를 가져옵니다.
    /// </summary>
    /// <returns></returns>
    public ArtifactDTO GetUserArtifactData(string aritfactID)
    {
        if (userServerArtifactData.UserArtifacts.TryGetValue(aritfactID, out ArtifactDTO artifactDTO))
            return artifactDTO;

        return null;
    }


    /// <summary>
    /// 아티팩트를 업그레이드 합니다.
    /// </summary>
    /// <param name="artifactID"></param>
    /// <returns></returns>
    public async UniTask<bool> UpgradeArtifact(string artifactID)
    {
        if (!IsOwned(artifactID)) return false;

        ArtifactDTO artifactDto = GetUserArtifactData(artifactID);
        ArtifactUpgradeData artifactUpgradeData = GlobalManagers.Chart.GetArtifactUpgradeData(artifactDto.Level);
        
        int prevGold = currencyService.Gold;
        int costGold = artifactUpgradeData.RequiredGold;

        int soulStone = artifactDto.ArtifactStone;
        int costStone = artifactUpgradeData.RequiredSoulStone;


        if (GlobalDefines.MAX_VALUE.UNIT_MAX_LEVEL <= artifactDto.Level) return false;
        if (prevGold < costGold || soulStone < costStone)
            return false;

        currencyService.SpendGold(costGold);

        ArtifactDTO prevDto = new ArtifactDTO(artifactDto.Level, soulStone);
        ArtifactDTO newArtifactDto = new ArtifactDTO(artifactDto.Level + 1, Mathf.Max(soulStone - costStone, 0));
        userServerArtifactData.UpdateArtifact(artifactID, newArtifactDto);

        bool isSuccess = await userServerArtifactData.SaveDataAsync();

        if (isSuccess)
        {
            Logger.Log("승급 성공");
            GlobalManagers.Event.NotifyEvent(new OnArtifactUpgradeEventData
            {
                ArtifactID = artifactID
            });
            
            GlobalManagers.Event.NotifyEvent<OnDoDailyQuestEventData>(new OnDoDailyQuestEventData()
            {
                QuestType = EQuestType.UpgradeArtifact
            });
            
            GlobalManagers.Audio.PlaySfx(GlobalDefines.ResourceKey.Audio.ARTIFACT_UPGRADE_SFX);
            return true;
        }
        
        GlobalManagers.Event.NotifyEvent<OnBackEndErrorEventData>(new OnBackEndErrorEventData()
        {
            ErrorType = EBackEndErrorType.RequestFailed
        });
        
        Logger.LogError("승급 실패");
        currencyService.AddGold(costGold, false);
        userServerArtifactData.UpdateArtifact(artifactID, prevDto);
        return false;
    }

    
    
      
    /// <summary>
    /// 스냅샷을 만들어 놓습니다.
    /// </summary>
    /// <returns></returns>
    public Dictionary<string, ArtifactDTO> CreateSnapshot() 
        => new Dictionary<string, ArtifactDTO>(userServerArtifactData.UserArtifacts);
    
    
    /// <summary>
    /// 스냅샷으로 롤백 합니다.
    /// </summary>
    /// <param name="snapshot"></param>
    public void RestoreSnapshot(Dictionary<string, ArtifactDTO> snapshot)
    {
        foreach (var snap in snapshot)
        {
            UpdateArtifact(snap.Key,  snap.Value);
        }
    }


    /// <summary>
    /// 파라미터 값을 반환합니다.
    /// </summary>
    /// <returns></returns>
    public Param GetParam()
        => userServerArtifactData.ToParam();

    
    /// <summary>
    /// 아티팩트 데이터의 indate를 가져옵니다.
    /// </summary>
    /// <returns></returns>
    public string GetInDate()
        => userServerArtifactData.InDate;
}
