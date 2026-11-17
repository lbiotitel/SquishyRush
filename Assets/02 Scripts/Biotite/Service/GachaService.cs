using System;
using System.Collections;
using System.Collections.Generic;
using BackEnd;
using LitJson;
using Cysharp.Threading.Tasks;
using UnityEngine;


public enum EGachaType
{
    Unit,
    Artifact
}

//가챠 로직에 관한 서비스 입니다.
public class GachaService : IService
{
    private UnitService unitService;
    private ArtifactService artifactService;
    private CurrencyService currencyService;
    
    private GachaBackEnd gachaBackEnd;
    
    
    public void Initialize()
    {
        gachaBackEnd = GlobalManagers.BackEnd.Gacha;
        unitService = GlobalManagers.Service.GetService<UnitService>();
        artifactService = GlobalManagers.Service.GetService<ArtifactService>();
        currencyService = GlobalManagers.Service.GetService<CurrencyService>();
    }

    public void Release()
    {
        
    }


    /// <summary>
    /// GachType 에 따른 가챠를 수행 합니다.
    /// </summary>
    /// <param name="gachaType">가챠 종류 입니다.</param>
    /// <param name="n">가챠를 시도할 횟수 입니다.</param>
    /// <returns></returns>
    public async UniTask<(bool isSuccess, List<string> result)> DoGachaAsync(EGachaType gachaType, int n)
    {
        string tableID = GetGachaTableID(gachaType);
        if (tableID == null)
        {
            Logger.LogError($"{gachaType}에 해당하는 테이블 아이디가 없습니다.");
            return (false, null);
        }

        int costGem = GlobalDefines.GameConstants.GACHA_1X_REQUIRED_GEM * n;
        if (!currencyService.HasEnoughGem(costGem)) return (false, null);   

        var (isSuccess, jsonData) = await gachaBackEnd.DoGachaAsync(tableID, n);
        if (!isSuccess) { NotifyBackEndError(); return (false, null); }

        List<string> result = ParseResult(gachaType, jsonData);
        
        Action rollback = CreateRollback(gachaType);

        try
        {
            currencyService.SpendGem(costGem);
            UpdateUserData(gachaType, result);

            Param userParam = GetUserDataParam(gachaType);
            Param currencyParam = currencyService.GetParam();

            isSuccess = await gachaBackEnd.SaveGachaResultsAsync(
                GetDataTableId(gachaType), userParam, GetDataInDate(gachaType),
                GlobalDefines.DataTableName.USER_CURRENCY_DATA_TABLE, currencyParam, currencyService.GetInDate());
        }
        catch (Exception e)
        {
            Logger.LogError($"가챠 저장 중 예외: {e}");
            isSuccess = false;
        }

        if (!isSuccess)
        {
            Logger.LogError("가챠 실패를 하여 롤백합니다.");
            rollback();
            NotifyBackEndError();
            return (false, null);
        }

        return (true, result);
    }


    /// <summary>
    /// 서버 가챠 결과(JsonData)를 ID 리스트로 변환합니다.
    /// </summary>
    private List<string> ParseResult(EGachaType gachaType, JsonData jsonData)
    {
        string key = gachaType switch
        {
            EGachaType.Unit => "UnitID",
            EGachaType.Artifact => "ArtifactID",
            _ => null
        };

        List<string> result = new List<string>();
        if (key == null) return result;

        for (int i = 0; i < jsonData.Count; i++)
        {
            result.Add(jsonData[i][key].ToString());
        }

        return result;
    }

    
    /// <summary>
    /// 가챠 타입에 맞는 파라미터 값을 가져옵니다.
    /// </summary>
    /// <param name="gachaType"></param>
    /// <returns></returns>
    private Param GetUserDataParam(EGachaType gachaType)
    {
        switch (gachaType)
        {
            case EGachaType.Unit:
                return unitService.GetParam();
            case EGachaType.Artifact:
                return artifactService.GetParam();
        }

        return null;
    }


    public string GetDataInDate(EGachaType gachaType)
    {
        switch (gachaType)
        {
            case EGachaType.Unit:
                return unitService.GetInDate();
            case EGachaType.Artifact:
                return artifactService.GetInDate();
        }

        return string.Empty;
    }


    /// <summary>
    /// 백엔드 에러를 알립니다.
    /// </summary>
    private void NotifyBackEndError()
    {
        GlobalManagers.Event.NotifyEvent<OnBackEndErrorEventData>(new OnBackEndErrorEventData()
        {
            ErrorType = EBackEndErrorType.RequestFailed
        });
    }
    
    
    /// <summary>
    /// 롤백 지점을 만들어 놓습니다.
    /// </summary>
    /// <param name="gachaType"></param>
    /// <returns></returns>
    private Action CreateRollback(EGachaType gachaType)
    {
        var currencySnapshot = currencyService.CreateSnapshot();

        switch (gachaType)
        {
            case EGachaType.Unit:
                var unitSnapshot = unitService.CreateSnapshot();
                return () =>
                {
                    unitService.RestoreSnapshot(unitSnapshot);
                    currencyService.RestoreSnapshot(currencySnapshot);
                };
            case EGachaType.Artifact:
                var artifactSnapshot = artifactService.CreateSnapshot();
                return () =>
                {
                    artifactService.RestoreSnapshot(artifactSnapshot);
                    currencyService.RestoreSnapshot(currencySnapshot);
                };
        }
        return () => currencyService.RestoreSnapshot(currencySnapshot);
    }


    /// <summary>
    /// 가챠 결과 로컬 데이터에 저장합니다.
    /// </summary>
    /// <param name="gachaType"></param>
    /// <param name="gachaResults"></param>
    /// <returns></returns>
    private void UpdateUserData(EGachaType gachaType ,List<string> gachaResults)
    {
        if (gachaType == EGachaType.Unit)
        {
            foreach (var unitID in gachaResults)
            {
                if (unitService.IsOwned(unitID))
                {
                    UnitDTO unitDto = unitService.GetUserUnitData(unitID);
                    UnitDTO newUnitDto = 
                        new UnitDTO(unitDto.Level, unitDto.SoulStoneCount + 
                                                   GlobalDefines.GameConstants.GACHA_DUPLICATED_STONE_COUNT);
                    
                    unitService.UpdateUnit(unitID, newUnitDto);
                }
                else
                {
                    unitService.AddNewUnit(unitID, new UnitDTO(0,0));
                }
            }
        }
        else if(gachaType == EGachaType.Artifact)
        {
            foreach (var artifactID in gachaResults)
            {
                if (artifactService.IsOwned(artifactID))
                {
                    ArtifactDTO artifactDto = artifactService.GetUserArtifactData(artifactID);
                    ArtifactDTO newArtifactDto = 
                        new ArtifactDTO(artifactDto.Level, artifactDto.ArtifactStone + 
                                                           GlobalDefines.GameConstants.GACHA_DUPLICATED_STONE_COUNT);
                    
                    artifactService.UpdateArtifact(artifactID, newArtifactDto);
                }
                else
                {
                    artifactService.AddArtifact(artifactID, new ArtifactDTO(0,0));
                }
            }
        }
    }

    
    /// <summary>
    /// 가챠타입에 맞는 가챠 데이터 테이블 id를 리턴합니다.
    /// </summary>
    /// <param name="gachaType"></param>
    /// <returns></returns>
    private string GetDataTableId(EGachaType gachaType)
    {
        switch (gachaType)
        {
            case EGachaType.Unit:
                return GlobalDefines.DataTableName.USER_UNIT_DATA_TABLE;
            case EGachaType.Artifact:
                return GlobalDefines.DataTableName.USER_ARTIFACT_DATA_TABLE;
        }

        return string.Empty;
    }

    /// <summary>
   /// 가챠 테이블 의 id를 가져옵니다.
   /// </summary>
   /// <param name="eGachaType"></param>
   /// <returns></returns>
    private string GetGachaTableID(EGachaType eGachaType)
    {
        switch (eGachaType)
        {
            case EGachaType.Unit:
                return GlobalDefines.DataTableName.UNIT_GACHA_TABLE;
            case EGachaType.Artifact:
                return GlobalDefines.DataTableName.ARTIFACT_GACHA_TABLE;
        }

        return null;
    }
}
