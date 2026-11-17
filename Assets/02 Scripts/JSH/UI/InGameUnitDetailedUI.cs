using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InGameUnitDetailedUI : MonoBehaviour
{
    [Header("기본 정보")] 
    [SerializeField] private TMP_Text rarityText;
    [SerializeField] private Image rarityBg;
    [SerializeField] private TMP_Text unitNameText;
    [SerializeField] private Image unitIcon;
    [SerializeField] private Image unitBg;
    [SerializeField] private GameObject[] starObjects;

    [Header("라벨")] 
    [SerializeField] private TMP_Text atkLabel;
    [SerializeField] private TMP_Text hpLabel;
    [SerializeField] private TMP_Text critLabel;
    [SerializeField] private TMP_Text atkSpeedLabel;
    [SerializeField] private TMP_Text dmgLabel;
    [SerializeField] private TMP_Text statusDmgLabel;
    [SerializeField] private TMP_Text perkTitleLabel;
    
    [Header("스탯")] 
    [SerializeField] private TMP_Text atkText;
    [SerializeField] private TMP_Text hpText;
    [SerializeField] private TMP_Text critText;
    [SerializeField] private TMP_Text atkSpeedText;
    [SerializeField] private TMP_Text dmgText;
    [SerializeField] private TMP_Text statusDmgText;

    [Header("스킬")] 
    [SerializeField] private TMP_Text skillNameText;
    [SerializeField] private Image skillIcon;
    [SerializeField] private TMP_Text skillDescText;

    [Header("재능")] 
    [SerializeField] private GameObject perkTitle;
    [SerializeField] private GameObject perkLayout;
    [SerializeField] private UnitPerkSlotView[] perkSlots;
    
    [Header("종족")]
    [SerializeField] private Image elementIcon;
    [SerializeField] private GameObject[] raceSlots;
    [SerializeField] private Image[] raceIcons;
    
    public BattleUnit Target { get; private set; }

    private CanvasGroup canvasGroup;

    // 표시 중인 스탯 → 갱신 함수 매핑 (구독 해제용)
    private readonly (EStatType type, Action<float, float> handler)[] bindings =
        new (EStatType type, Action<float, float>)[7];

    private readonly Stat[] boundStats = new Stat[7];

    private bool isInit;
    
    private string skillDescTemplate;
    private bool skillDescDirty;
    
    private static string UI(string key)
        => Localization.GetLocaliationText(Localization.LocalizationTable.UI, key);
    
    private void EnsureInit()
    {
        if (isInit) return;
        isInit = true;

        atkLabel.text = UI(LocaliztionKeyCode.UI_STAT_ATK_TEXT);
        hpLabel.text = UI(LocaliztionKeyCode.UI_STAT_HP_TEXT);
        critLabel.text = UI(LocaliztionKeyCode.UI_STAT_CRITRATE_TEXT);
        atkSpeedLabel.text = UI(LocaliztionKeyCode.UI_STAT_ATKSPEED_TEXT);
        dmgLabel.text = UI(LocaliztionKeyCode.UI_STAT_DMG_TEXT);
        statusDmgLabel.text = UI(LocaliztionKeyCode.UI_STAT_STATUSDMG_TEXT);
        perkTitleLabel.text = UI(LocaliztionKeyCode.UI_PERK_TITLE_TEXT);
        
        bindings[0] = (EStatType.Atk, (_, v) =>
        {
            atkText.text = Mathf.RoundToInt(v).ToString();
            skillDescDirty = true;
        });
        bindings[1] = (EStatType.MaxHp, (_, v) =>
        {
            hpText.text = Mathf.RoundToInt(v).ToString();
            skillDescDirty = true;
        });
        bindings[2] = (EStatType.CritRate, (_, v) => critText.text = $"{v:0.#}%");
        bindings[3] = (EStatType.AtkSpeed, (_, v) => atkSpeedText.text = v.ToString("0.##"));
        bindings[4] = (EStatType.DmgMultiplier, (_, v) =>
        {
            dmgText.text = ToBonusPercent(v);
            skillDescDirty = true;
        });
        bindings[5] = (EStatType.StatusEffectDmg, (_, v) => statusDmgText.text = ToBonusPercent(v));
        bindings[6] = (EStatType.HealMultiplier, (_, __) => skillDescDirty = true);
    }

    public void Show(BattleUnit unit)
    {
        if (unit == null) return;
        EnsureInit();
        if (Target == unit && gameObject.activeSelf) return;

        UnBind();
        Target = unit;

        SetBasicInfo(unit);
        SetSkill(unit.SkillData);
        SetPerks(unit);
        BindStats(unit);
        unit.OnPerkChanged += OnTargetPerkChanged;
        unit.OnStarChanged += OnTargetStarChanged;
        
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        if (!isInit) return;
        UnBind();
        Target = null;
        gameObject.SetActive(false);
    }

    private void SetBasicInfo(BattleUnit unit)
    {
        UnitData data = unit.CurrentUnitData;

        rarityText.text = GlobalDefines.Rarity.GetRarityText(data.UnitRarity);
        rarityBg.color = GlobalDefines.Rarity.GetRarityColor(data.UnitRarity);
        unitNameText.text = Localization.GetLocaliationText(Localization.LocalizationTable.UNIT, data.UnitNameKey);
        unitIcon.sprite = data.UnitImage;
        unitBg.color = GlobalDefines.Rarity.GetRarityPastelColor(data.UnitRarity);
        
        elementIcon.sprite = GlobalManagers.Sprite.GetUnitElementalIcon(data.ElementType);

        DrawRace(data.Races);
        
        for(int i = 0; i < starObjects.Length; i++)
            starObjects[i].SetActive(i < unit.Star);
        
    }

    private void DrawRace(IReadOnlyList<ERace> races)
    {
        int count = races?.Count ?? 0;
        for (int i = 0; i < raceIcons.Length; i++)
        {
            bool hasRace = i < count;
            raceSlots[i].gameObject.SetActive(hasRace);

            if (hasRace)
                raceIcons[i].sprite = GlobalManagers.Sprite.GetUnitRaceIcon(races[i]);
        }
    }

    private void SetSkill(SkillData skill)
    {
        bool hasSkill = skill != null;
        skillNameText.gameObject.SetActive(hasSkill);
        skillIcon.transform.parent.gameObject.SetActive(hasSkill);
        if (!hasSkill) return;

        skillNameText.text = Localization.GetLocaliationText(Localization.LocalizationTable.Skill, skill.SkillNameKey);
        skillDescTemplate = Localization.GetLocaliationText(Localization.LocalizationTable.Skill, skill.SkillDescKey);
        skillIcon.sprite = skill.SkillIcon;
        RefreshSkillDesc();
    }

    private void RefreshSkillDesc()
    {
        if (Target == null || Target.SkillData == null) return;
        skillDescText.text = SkillDescFormatter.Build(skillDescTemplate, Target.SkillData, Target.Stat, Target.Star);
    }

    private void OnDescStatChanged(float prev, float next) => skillDescDirty = true;

    private void LateUpdate()
    {
        if (!skillDescDirty) return;
        skillDescDirty = false;
        RefreshSkillDesc();
    }

    /// <summary>
    /// 표시할 스탯들에 구독하고 현재 값을 한 번 그립니다. 구독한 Stat 객체는 해제용으로 보관합니다.
    /// </summary>
    private void BindStats(BattleUnit unit)
    {
        for(int i = 0; i < bindings.Length; i++)
        {
            Stat stat = unit.Stat.GetStatObject(bindings[i].type);
            boundStats[i] = stat; // 해제는 '구독한 그 객체'에 해야 합니다
            if (stat == null) continue;

            stat.OnValueChanged += bindings[i].handler;
            bindings[i].handler(0f, stat.Value);
        }
    }

    private void UnBindStats()
    {
        for (int i = 0; i < bindings.Length; i++)
        {
            if (boundStats[i] != null) boundStats[i].OnValueChanged -= bindings[i].handler;
            boundStats[i] = null;
        }
    }

    private void UnBind()
    {
        if (Target == null) return;

        Target.OnPerkChanged -= OnTargetPerkChanged;
        Target.OnStarChanged -= OnTargetStarChanged;
        
        UnBindStats();
    }

    private static string ToBonusPercent(float multiplier)
        => $"{Mathf.RoundToInt((multiplier - 1f) * 100)}%";

    /// <summary>
    /// 보유한 재능 수만큼 칸을 켜서 그립니다. 재능이 없으면 재능 영역 전체를 숨깁니다.
    /// </summary>
    private void SetPerks(BattleUnit unit)
    {
        IReadOnlyList<PerkBase> perks = unit.Perks;
        bool hasPerk = perks.Count > 0;

        for (int i = 0; i < perkSlots.Length; i++)
        {
            bool active = i < perks.Count;
            perkSlots[i].SetActive(active);
            if (active) perkSlots[i].Draw(perks[i].Data);
        }
    }

    private void OnTargetPerkChanged()
    {
        if(Target != null) SetPerks(Target);
    }

    // 합성으로 RebuildStat()이 돌면 Stat이 새 객체가 되므로 다시 구독합니다.
    private void OnTargetStarChanged(int star)
    {
        if(Target == null) return;
        
        UnBindStats();
        BindStats(Target);
        SetBasicInfo(Target);
        RefreshSkillDesc();
    }
    
    private void OnDestroy() => UnBind();
    
}
