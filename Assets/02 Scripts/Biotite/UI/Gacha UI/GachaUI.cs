using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;



//뽑기 UI에 대한 정의입니다.
public class GachaUI : BaseUI
{
    [Header("Button")]
    [SerializeField] private AnimButton unitGachax1Button;
    [SerializeField] private AnimButton unitGachax10Button;
    [SerializeField] private AnimButton artifactGachax1Button;
    [SerializeField] private AnimButton artifactGachax10Button;

    private List<UnitData> unitDatas;
    private List<ArtifactData> artifactDatas;
    private bool isGachaLogicFinished = true;
    
    //service
    private GachaService gachaService;
    private CurrencyService currencyService;

    private ToastMessageUIData notEnoughGemData;
    

    //초기화
    public override void Initialize(Transform anchor)
    {
        base.Initialize(anchor);
        
        unitDatas = GlobalManagers.Resource.GetResources<UnitData>();
        artifactDatas = GlobalManagers.Resource.GetResources<ArtifactData>();
        
        gachaService = GlobalManagers.Service.GetService<GachaService>();
        currencyService = GlobalManagers.Service.GetService<CurrencyService>();
        
        notEnoughGemData = new ToastMessageUIData
        {
            message = LocaliztionKeyCode.UI_GEM_IS_NOT_ENOUGH_TEXT
        };

        BindEvents();
    }


    private void BindEvents()
    {
        GlobalManagers.Event?.AddEvent<OnGachaResultAnimFinishedEventData>(OnGachaResultAnimFinishedEventHandler);
        unitGachax1Button.OnClicked += OnClickedUnit1xGachBtn;
        unitGachax10Button.OnClicked += OnClickedUnit10GachaBtn;
        artifactGachax1Button.OnClicked += OnClickedArtifact1xGachBtn;
        artifactGachax10Button.OnClicked += OnClickedArtifact10GachaBtn;
    }

    private void OnDestroy()
    {
        GlobalManagers.Event?.RemoveEvent<OnGachaResultAnimFinishedEventData>(OnGachaResultAnimFinishedEventHandler);
        unitGachax1Button.OnClicked -= OnClickedUnit1xGachBtn;
        unitGachax10Button.OnClicked -= OnClickedUnit10GachaBtn;
        artifactGachax1Button.OnClicked -= OnClickedArtifact1xGachBtn;
        artifactGachax10Button.OnClicked -= OnClickedArtifact10GachaBtn;
    }
    
    
    private void OnGachaResultAnimFinishedEventHandler(OnGachaResultAnimFinishedEventData e)
        => isGachaLogicFinished = true;

    private void OnClickedUnit1xGachBtn()
        => DoGacha(EGachaType.Unit, 1).Forget();

    private void OnClickedUnit10GachaBtn()
        => DoGacha(EGachaType.Unit, 10).Forget();

    private void OnClickedArtifact1xGachBtn()
        => DoGacha(EGachaType.Artifact, 1).Forget();

    private void OnClickedArtifact10GachaBtn()
        => DoGacha(EGachaType.Artifact, 10).Forget();
    
    
    
    /// <summary>
    /// 지정된 가챠를 count 회 뽑습니다
    /// </summary>
    private async UniTask DoGacha(EGachaType gachaType, int count)
    {
        if (!isGachaLogicFinished) return;

        isGachaLogicFinished = false;
        try
        {
            int requiredGem = GlobalDefines.GameConstants.GACHA_1X_REQUIRED_GEM * count;
            if (!currencyService.HasEnoughGem(requiredGem))
            {
                GlobalManagers.UI.PopUpContainer.OpenUI<ToastMessageUI>(notEnoughGemData);
                return;
            }

            var (isSuccess, result) = 
                await Utils.WaitForAwaitMethodWithOverlayUI(gachaService.DoGachaAsync(gachaType, count));

            
            if (!isSuccess)
            {
                Logger.LogError("가챠를 실패하였습니다.");
                return;
            }

            GlobalManagers.Event.NotifyEvent<OnDoDailyQuestEventData>(new OnDoDailyQuestEventData()
            {
                QuestType = EQuestType.Gacha
            });

            List<GachaResultDatas> gachaResultDatas = CreateGachaResultsDatas(gachaType, result); 
        
            ShowGachaEffect(gachaResultDatas);
        }
        catch (System.Exception ex)
        {
            Logger.LogError($"[GachaUI] 가챠 진행 중 에러 발생: {ex.Message}");
        }
        finally
        {
            isGachaLogicFinished = true; 
        }
    }
    
    
    /// <summary>
    /// 가챠 연출에 쓰일 데이터를 생성합니다.
    /// </summary>
    /// <param name="gachaType"></param>
    /// <param name="gachResults"></param>
    /// <returns></returns>
    private List<GachaResultDatas> CreateGachaResultsDatas(EGachaType gachaType ,List<string> gachResults)
    {
        List<GachaResultDatas>  gachaResultDatas = new List<GachaResultDatas>();
        
        foreach (var id in gachResults)
        {
            GachaResultDatas resultData = new GachaResultDatas();
            if (gachaType == EGachaType.Unit)
            {
                if(GlobalManagers.Resource.TryGetResource<UnitData>(id, out UnitData unitData))
                {
                    resultData.ResultName = Localization.GetLocaliationText(Localization.LocalizationTable.UNIT, unitData.UnitNameKey);
                    resultData.ResultImage = unitData.UnitImage;
                    resultData.ResultRarity =  unitData.UnitRarity;
                }
                else
                {
                    Logger.LogError($"{id}에 해당하는 유닛데이터가 없습니다.");
                }
            }
            else if (gachaType == EGachaType.Artifact)
            {
                if (GlobalManagers.Resource.TryGetResource<ArtifactData>(id, out ArtifactData artifactData))
                {
                    resultData.ResultName = Localization.GetLocaliationText(Localization.LocalizationTable.ARTIFACT, artifactData.ArtifactNamekey); ;
                    resultData.ResultImage = artifactData.ArtifactImage;
                    resultData.ResultRarity = artifactData.ArtifactRarity; 
                }else
                {
                    Logger.LogError($"{id}에 해당하는 아티팩트 데이터가 없습니다.");
                }
            }
            
            gachaResultDatas.Add(resultData);
        }
        
        return gachaResultDatas;
    }


    /// <summary>
    /// 가챠 연출을 진행합니다.
    /// </summary>
    private void ShowGachaEffect(List<GachaResultDatas> gachaResultDatas)
    {
        GachaEffectUIData gachaEffectUIData = new GachaEffectUIData()
        {
            gachaResults = gachaResultDatas
        };
        
        GlobalManagers.UI.PopUpContainer.OpenUI<GachaResultUI>(gachaEffectUIData);
    }

}
