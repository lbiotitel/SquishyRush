using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class UnitDetailUIData : BaseUIData
{
    public UnitData UnitData;
}

//유닛의 상세정보를 표시하는 ui 입니다.
public class UnitDetailedUI : BaseUI
{
    [Header("Animator")] 
    [SerializeField] private Animator unitAnimator;
    
    [Header("Image")]
    [SerializeField] private Image unitElementalImage;
    [SerializeField] private Image[] unitRaceImages;
    [SerializeField] private Image unitRarityBg;
    
    [Header("Text")]
    [SerializeField] private TMP_Text unitNameText;
    [SerializeField] private TMP_Text unItLevelText;
    [SerializeField] private TMP_Text unitRarityText;

    [SerializeField] private TMP_Text unitSoulStoneText;
    [SerializeField] private TMP_Text levelUpRequiredCostText;

    [Header("Stat Text")]
    [SerializeField] private TMP_Text atkStatText;
    [SerializeField] private TMP_Text hpStatText;
    [SerializeField] private TMP_Text criticalRateStatText;

    [Header("Slider")]
    [SerializeField] private Slider unitSoulStoneSlider;

    [Header("Object")]
    [SerializeField] private GameObject lockObject;
    [SerializeField] private GameObject canUpgradeObject;

    [Header("Button")]
    [SerializeField] private AnimButton closeButton;
    [SerializeField] private AnimButton levelUpButton;

    [Header("Level Cell View")]
    [SerializeField] private ScrollRect unitLevelScrollRect;
    [SerializeField] private Transform unitLevelContentTr;
    
    [Header("SkillView")]
    [SerializeField] private SkillViewComponent skillView;
 
    private GameObject unitLevelCellViewPrefab;
    private List<UnitLevelNodeUI> unitLevelCellViews = new List<UnitLevelNodeUI>();

    //toast message data
    private ToastMessageUIData upgradeSucceedData;
    private ToastMessageUIData upgradeFailedData;


    private UnitService unitService;
    private UnitData currentUnitData;



    public override void Initialize(Transform anchor)
    {
        base.Initialize(anchor);

        unitService = GlobalManagers.Service.GetService<UnitService>();
        
        BindEvent();

        upgradeSucceedData = new ToastMessageUIData()
        {
            message = LocaliztionKeyCode.UI_LEVEL_UPGRADE_SUCCESSED_TEXT
        };

        upgradeFailedData = new ToastMessageUIData()
        {
            message = LocaliztionKeyCode.UI_NOT_ENOUGH_RESOURCE_TEXT
        };

        if (!GlobalManagers.Resource.TryGetResource<GameObject>(GlobalDefines.ResourceKey.UI.UNIT_LEVEL_CELL_VIEW_PREFAB,
                out GameObject prefab))
        {
            Logger.LogError($"{GlobalDefines.ResourceKey.UI.UNIT_LEVEL_CELL_VIEW_PREFAB}에 해당하는 프리팹이 없습니다.");
            return;
        }

        unitLevelCellViewPrefab = prefab;
        CreateLevelListObject();
    }


    private void BindEvent()
    {
        closeButton.OnClicked += CloseUI;
        levelUpButton.OnClicked += OnClickedLevelUpButton;
    }


    private void OnDestroy()
    {
        closeButton.OnClicked -= CloseUI;
        levelUpButton.OnClicked -= OnClickedLevelUpButton;
    }

    
    private void OnClickedLevelUpButton()
        => UpgradeUnitLevel().Forget();


    public override void OpenUI()
    {
        unitLevelScrollRect.verticalNormalizedPosition = 1;
        base.OpenUI();
    }


    public override void SetInfo(BaseUIData data)
    {
        base.SetInfo(data);

        if (data is UnitDetailUIData unitDetailUIData)
        {
            currentUnitData = unitDetailUIData.UnitData;
            DrawDetailedUI();
        }
    }

    
    
    
    /// <summary>
    /// 레벨 리스트 오브젝트들을 미리 만들어 풀링합니다.
    /// </summary>
    private void CreateLevelListObject()
    {
        int maxLevel = GlobalDefines.MAX_VALUE.UNIT_MAX_LEVEL;
        
        for(int i = 0; i < maxLevel; i++)
        {
            GameObject go = Instantiate(unitLevelCellViewPrefab, unitLevelContentTr);
            UnitLevelNodeUI node = go.GetComponent<UnitLevelNodeUI>();
            unitLevelCellViews.Add(node);
        }
    }


    //유닛 상세 정보를 그립니다.
    private void DrawDetailedUI()
    {
        if (!GlobalManagers.Resource.TryGetResource<RuntimeAnimatorController>(currentUnitData.UnitUIAnimator,
                out RuntimeAnimatorController animatorController))
        {
            Logger.LogError($"{currentUnitData.UnitUIAnimator}에 해당하는 ui animator가 없습니다.");
            return;
        }

        unitAnimator.runtimeAnimatorController = animatorController;

        DrawUnitBaseInfo();

        bool isOwned = unitService.IsOwned(currentUnitData.UnitId);
        if (isOwned) DrawOwnedUnitInfo();
        else DrawLockedUnitInfo();

        int level = isOwned ? unitService.GetUserUnitData(currentUnitData.UnitId).Level : 0;
        DrawSkillView(level);

        DrawUnitLevelList();
    }
    
    
    
    /// <summary>
    /// 유닛의 기본 정보 데이터를 그립니다.
    /// </summary>
    private void DrawUnitBaseInfo()
    {
        unitRarityBg.color = GlobalDefines.Rarity.GetRarityPastelColor(currentUnitData.UnitRarity);
        unitNameText.text = Localization.GetLocaliationText(Localization.LocalizationTable.UNIT, currentUnitData.UnitNameKey);
        unitRarityText.text = GlobalDefines.Rarity.GetRarityText(currentUnitData.UnitRarity);
        unitRarityText.color = GlobalDefines.Rarity.GetRarityColor(currentUnitData.UnitRarity);
        unitElementalImage.sprite = GlobalManagers.Sprite.GetUnitElementalIcon(currentUnitData.ElementType);

        var races = currentUnitData.Races;
        
        for (int i = 0; i < unitRaceImages.Length; i++)
        {
            unitRaceImages[i].gameObject.SetActive(false);

            if (races.Count > i)
            {
                unitRaceImages[i].sprite = GlobalManagers.Sprite.GetUnitRaceIcon(races[i]);
                unitRaceImages[i].gameObject.SetActive(true);
            }
        }
    }
    

    /// <summary>
    /// 소지하고 있는 유닛이 그려할 데이터 항목을 그립니다.
    /// </summary>
    private void DrawOwnedUnitInfo()
    {
        UnitDTO unitDto = unitService.GetUserUnitData(currentUnitData.UnitId);
        UnitUpgradeData unitUpgradeData = GlobalManagers.Chart.GetUnitLevelChartData(unitDto.Level);

        if (unitUpgradeData == null) return;

        unItLevelText.gameObject.SetActive(true);
        lockObject.SetActive(false);

        unItLevelText.text = $"Lv.{unitDto.Level}";
        levelUpRequiredCostText.text = $"{unitUpgradeData.RequiredGold}";

        DrawUnitStats(unitDto.Level);

        bool isMaxLevel = unitDto.Level >= GlobalDefines.MAX_VALUE.UNIT_MAX_LEVEL;
        DrawSoulStoneProgress(unitDto, unitUpgradeData, isMaxLevel);

        canUpgradeObject.SetActive(!isMaxLevel && unitDto.SoulStoneCount >= unitUpgradeData.RequiredSoulStone);
        levelUpButton.gameObject.SetActive(!isMaxLevel);
    }
    
    
        
    /// <summary>
    /// 소지하고이지 않은 유닛의 데이터 정보를 그립니다.
    /// </summary>
    private void DrawLockedUnitInfo()
    {
        atkStatText.text = $"{currentUnitData.Atk}";
        hpStatText.text = $"{currentUnitData.MaxHp}";
        criticalRateStatText.text =  $"{currentUnitData.CriticalRate}%";
        
        lockObject.SetActive(true);
        canUpgradeObject.SetActive(false);
        unItLevelText.gameObject.SetActive(false);
        levelUpButton.gameObject.SetActive(false);
    }

    
    
    /// <summary>
    /// 유닛의 레벨에 따른 스탯 정보를 그립니다.
    /// </summary>
    /// <param name="level"></param>
    private void DrawUnitStats(int level)
    {
        UnitBaseStat unitStat = UnitStatCalculator.Calculate(currentUnitData, level);

        float bonusAtk = unitStat.Atk - currentUnitData.Atk;
        float bonusHp = unitStat.MaxHp - currentUnitData.MaxHp;
        float bonusCriRate = unitStat.CritRate - currentUnitData.CriticalRate;

        atkStatText.text = Utils.FormatStatWithBonus(unitStat.Atk, bonusAtk);
        hpStatText.text = Utils.FormatStatWithBonus(unitStat.MaxHp, bonusHp);
        criticalRateStatText.text = Utils.FormatStatWithBonus(unitStat.CritRate, bonusCriRate, "%");
    }
    
    
    
    
    /// <summary>
    /// 유닛의 소울 스톤의 부분을 그립니다.
    /// </summary>
    /// <param name="unitDto"></param>
    /// <param name="unitUpgradeData"></param>
    /// <param name="isMaxLevel"></param>
    private void DrawSoulStoneProgress(UnitDTO unitDto, UnitUpgradeData unitUpgradeData, bool isMaxLevel)
    {
        if (isMaxLevel)
        {
            unitSoulStoneSlider.value = 1f;
            unitSoulStoneText.text = GlobalDefines.MAX_VALUE.MAX_LEVEL_TEXT;
        }
        else
        {
            unitSoulStoneSlider.value = (float)unitDto.SoulStoneCount / unitUpgradeData.RequiredSoulStone;
            unitSoulStoneText.text = $"{unitDto.SoulStoneCount}/{unitUpgradeData.RequiredSoulStone}";
        }
    }
    

        
    /// <summary>
    /// 유닛 레벨에 따른 스탯 정보를 그립니다.
    /// </summary>
    private void DrawUnitLevelList()
    {
        List<LevelBonusEntry> entries = currentUnitData.GetLevelBonusList();

        int index = 0;
        foreach (var entry in entries)
        {
            UnitLevelNodeUI node = unitLevelCellViews[index++];
            node.Init(entry);
        }
    }

    private void DrawSkillView(int level)
    {
        UnitStat stat = new UnitStat();
        stat.Init(UnitStatCalculator.Calculate(currentUnitData, level));
        skillView.Initialize(currentUnitData.SkillId, stat, -1);
    }

    /// <summary>
    /// 유닛을 업그레이드 합니다.
    /// </summary>
    private async UniTask UpgradeUnitLevel()
    {
        bool res =
             await Utils.WaitForAwaitMethodWithOverlayUI(unitService.UpgradeUnit(currentUnitData.UnitId));

        if (res)
        {
            DrawDetailedUI();
            GlobalManagers.UI.PopUpContainer.OpenUI<ToastMessageUI>(upgradeSucceedData);
        }
        else
        {
            GlobalManagers.UI.PopUpContainer.OpenUI<ToastMessageUI>(upgradeFailedData);
        }
    }
}
