using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 유닛의 Cell View 클래스입니다.
/// </summary>
public class UnitListCellView : MonoBehaviour, IPointerClickHandler
{
    [Header("Animator")]
    [SerializeField] private Animator unitAnimator;

    [Header("Image")]
    [SerializeField] private Image unitElementalImage;
    [SerializeField] private Image[] unitRaceImages;
    [SerializeField] private Image rarityBG;

    [Header("Text")]
    [SerializeField] private TMP_Text unitLevelText;
    [SerializeField] private TMP_Text unitNameText;
    [SerializeField] private TMP_Text upgradeText;

    [Header("Upgrade Slider")]
    [SerializeField] private Slider upgradeSlider;

    [Header("GameObject")]
    [SerializeField] private GameObject upgradeLockObject;
    [SerializeField] private GameObject canUpgradeObject;
    [SerializeField] private GameObject disableObject;

    [Header("Animation")]
    [SerializeField] private float punchScale = 0.1f;
    [SerializeField] private float punchDuration = 0.2f;
    private bool isAnim = false;

    public UnitData CurrentUnitData { get; private set; }
    private UnitService unitService;

    public void Initialize(UnitData unitData)
    {
        CurrentUnitData = unitData;
        unitService = GlobalManagers.Service.GetService<UnitService>();

        InitUnitUIAnimator();
        DrawUnitInfo();
    }

    /// <summary>
    /// 유닛 UI 애니메이터를 가져와 초기화합니다.
    /// </summary>
    private void InitUnitUIAnimator()
    {
        if (!GlobalManagers.Resource.TryGetResource<RuntimeAnimatorController>(CurrentUnitData.UnitUIAnimator,
                out RuntimeAnimatorController animatorController))
        {   
            Logger.LogError($"{CurrentUnitData.UnitUIAnimator}에 해당하는 애니메이터 컨트롤러가 없습니다.");
            return;
        }

        RuntimeAnimatorController unitAnimatorController = animatorController;
        if (unitAnimatorController != null)
            unitAnimator.runtimeAnimatorController = unitAnimatorController;
    }

    /// <summary>
    /// 유닛의 전체 정보를 그립니다. (공통 시각정보 + 보유상태별 정보)
    /// </summary>
    public void DrawUnitInfo()
    {
        if (unitService == null)
        {
            Debug.LogWarning($"[{nameof(UnitListCellView)}] UnitService가 초기화되지 않았습니다.");
            return;
        }

        DrawBaseUnitInfo();

        bool isOwned = unitService.IsOwned(CurrentUnitData.UnitId);
        if (isOwned)DrawOwnedInfo();
        else DrawLockedInfo();
    }
    

    
    /// <summary>
    /// 유닛의 공통 데이터를 그립니다.
    /// </summary>
    private void DrawBaseUnitInfo()
    {
        unitNameText.text =
            Localization.GetLocaliationText(Localization.LocalizationTable.UNIT, CurrentUnitData.UnitNameKey);
        unitElementalImage.sprite = GlobalManagers.Sprite.GetUnitElementalIcon(CurrentUnitData.ElementType);
        rarityBG.color = GlobalDefines.Rarity.GetRarityPastelColor(CurrentUnitData.UnitRarity);

        DrawRaceIcons(CurrentUnitData.Races);
    }
    

    /// <summary>
    /// 종족 아이콘들을 갱신합니다. 보유한 종족 수만큼만 활성화합니다.
    /// </summary>
    private void DrawRaceIcons(IReadOnlyList<ERace> races)
    {
        for (int i = 0; i < unitRaceImages.Length; i++)
        {
            bool hasRace = races.Count > i;
            unitRaceImages[i].gameObject.SetActive(hasRace);

            if (hasRace)
                unitRaceImages[i].sprite = GlobalManagers.Sprite.GetUnitRaceIcon(races[i]);
        }
    }

    
    
    /// <summary>
    /// 보유 중인 유닛의 레벨/강화 정보를 그립니다.
    /// </summary>
    private void DrawOwnedInfo()
    {
        disableObject.SetActive(false);
        upgradeLockObject.SetActive(false);

        UnitDTO unitDto = unitService.GetUserUnitData(CurrentUnitData.UnitId);
        unitLevelText.text = $"Lv.{unitDto.Level}";

        bool isMaxLevel = unitDto.Level >= GlobalDefines.MAX_VALUE.UNIT_MAX_LEVEL;
        if (isMaxLevel)
        {
            DrawMaxLevelUpgradeInfo();
            return;
        }

        UnitUpgradeData unitUpgradeData = GlobalManagers.Chart.GetUnitLevelChartData(unitDto.Level);
        int currentSoulStone = unitDto.SoulStoneCount;
        int requiredSoulStone = unitUpgradeData.RequiredSoulStone;

        upgradeText.text = $"{currentSoulStone}/{requiredSoulStone}";
        upgradeSlider.value = requiredSoulStone > 0 ? (float)currentSoulStone / requiredSoulStone : 1f;
        canUpgradeObject.SetActive(currentSoulStone >= requiredSoulStone);
    }
    
    
    /// <summary>
    /// 만렙 유닛 데이터를 그립니다.
    /// </summary>
    private void DrawMaxLevelUpgradeInfo()
    {
        upgradeText.text = GlobalDefines.MAX_VALUE.MAX_LEVEL_TEXT;
        upgradeSlider.value = 1f;
        canUpgradeObject.SetActive(false);
    }

    
    /// <summary>
    /// 미보유 유닛의 잠금 상태를 그립니다.
    /// </summary>
    private void DrawLockedInfo()
    {
        unitLevelText.text = string.Empty;
        disableObject.SetActive(true);
        upgradeLockObject.SetActive(true);
    }

    // UI 클릭 시 디테일 UI 표시
    public void OnPointerClick(PointerEventData eventData)
    {
        if (isAnim) return;

        GlobalManagers.Audio.PlaySfx(GlobalDefines.ResourceKey.Audio.BUTTON_CLICK_SFX);

        UnitDetailUIData unitDetailUIData = new UnitDetailUIData
        {
            UnitData = CurrentUnitData
        };

        transform.DOPunchScale(Vector3.one * punchScale, punchDuration)
            .OnComplete(() =>
            {
                GlobalManagers.UI.PopUpContainer.OpenUI<UnitDetailedUI>(unitDetailUIData);
            });
    }

    public void OpenUI() => gameObject.SetActive(true);
    public void CloseUI() => gameObject.SetActive(false);
}