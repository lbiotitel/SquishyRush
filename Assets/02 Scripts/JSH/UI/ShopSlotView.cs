using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopSlotView : MonoBehaviour
{
    [Header("Image")] 
    [SerializeField] private Image slotBG;
    [SerializeField] private Image unitImage;
    [SerializeField] private TraitTagView elementTag;
    [SerializeField] private TraitTagView[] raceTags;

    [Header("Text")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text goldText;

    [Header("GameObject")] 
    [SerializeField] private GameObject contentRoot;
    [SerializeField] private GameObject canUpgradeObject;
    [SerializeField] private GameObject disableObject;

    [Header("구매 연출")] 
    [SerializeField] private float punchScale = 0.2f;
    [SerializeField] private float punchDuration = 0.2f;
    
    [Header("누름 연출")]
    [SerializeField] private float pressScale = 0.95f;
    [SerializeField] private float liftScale = 1.06f;
    [SerializeField] private float pressDuration = 0.08f;
    [SerializeField] private float liftDuration = 0.12f;
    
    private Tween punch;
    private Tween scaleTween;
    
    public bool HasUnit { get; private set; }

    public void Init()
    {
        unitImage.preserveAspect = true;
    }

    /// <summary>펀치 연출 중인지. 연출 도중 다시 누르면 무시합니다.</summary>
    public bool IsPunching => punch != null && punch.IsActive();
    
    public void PlayPressDown() => ScaleTo(pressScale, pressDuration, Ease.OutQuad);
    public void PlayLift() => ScaleTo(liftScale, liftDuration, Ease.OutBack);
    public void PlayRelease() => ScaleTo(1f, pressDuration, Ease.OutQuad);
    
    /// <summary>
    /// 누름 상태 연출. 구매 펀치가 진행 중이면 건드리지 않습니다.
    /// </summary>
    private void ScaleTo(float scale, float duration, Ease ease)
    {
        if (IsPunching) return;
        
        scaleTween?.Kill();
        scaleTween = transform.DOScale(scale, duration).SetEase(ease).SetLink(gameObject);
    }
    
    /// <summary>
    /// 슬롯을 한 번 튕기고, 끝나면 onComplete를 부릅니다.
    /// </summary>
    public void PlayPunch(Action onComplete = null)
    {
        scaleTween?.Kill();
        transform.localScale = Vector3.one;
        
        punch = transform.DOPunchScale(Vector3.one * punchScale, punchDuration)
            .SetLink(gameObject)
            .OnComplete(() => onComplete?.Invoke());
    }

    public void SetSlot(ShopSlot slot, bool canUpgrade, EShopActionResult buyResult)
    {
        bool hasUnit = slot != null && !slot.IsEmpty;
        HasUnit = hasUnit;

        contentRoot.SetActive(hasUnit);

        if (!hasUnit)
        {
            if(canUpgradeObject != null) canUpgradeObject.SetActive(false);
            if(disableObject != null) disableObject.SetActive(false);
            return;
        }

        UnitData unitData = slot.Unit;

        slotBG.color = GlobalDefines.Rarity.GetRarityPastelColor(unitData.UnitRarity);
        unitImage.sprite = unitData.UnitImage;

        DrawTraits(unitData);
        
        nameText.text = Localization.GetLocaliationText(Localization.LocalizationTable.UNIT, unitData.UnitNameKey);

        goldText.text = InGameBalance.GetUnitCost(unitData.UnitRarity).ToString();

        if(canUpgradeObject != null) canUpgradeObject.SetActive(canUpgrade);
        if(disableObject != null) disableObject.SetActive(buyResult == EShopActionResult.NotEnoughGold);
    }

    private void DrawTraits(UnitData unitData)
    {
        elementTag.Set(
            GlobalManagers.Sprite.GetUnitElementalIcon(unitData.ElementType),
            GlobalDefines.UnitConstant.GetUnitElementalName(unitData.ElementType));

        var races = unitData.Races;
        for (int i = 0; i < raceTags.Length; i++)
        {
            if (races != null && i < races.Count)
                raceTags[i].Set(
                    GlobalManagers.Sprite.GetUnitRaceIcon(races[i]),
                    GlobalDefines.UnitConstant.GetUnitRaceName(races[i]));
            else
                raceTags[i].Hide();

        }
    }

}