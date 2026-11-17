using System.Collections.Generic;

public enum EStatType
{
    None = -1,
    Atk = 0,
    MaxHp = 1,
    SG = 2,
    SGChargeAmount = 3,
    CritRate = 4,
    Accuracy = 5,
    StatusEffectDmg = 6,
    DmgMultiplier = 7,
    AtkSpeed = 8,
    Evasion = 9,
    HealCrit = 10,
    HealMultiplier = 11,
    CriDamageMultiplier = 12,
    
    //플레이어 스탯
    FireElementAtkBonus,
    WaterElementAtkBonus,
    EarthElementAtkBonus,
    LightElementAtkBonus,
    DarkElementAtkBonus,
    RerollCostReduction,
    FirstGold,
    GoldMultiplier,
    ExecuteBonusRate = 21,
    NormalAtkDmg = 22,
    SellPriceMultiplier = 23,
}

public readonly struct UnitBaseStat
{
    public readonly float Atk, MaxHp, CritRate, Accuracy, AtkSpeed, SG, SGChargeAmount;

        
    // 아티팩트 등 플레이어 보너스. 숫자로 굽지 않고 모디파이어 목록으로 들고 다니다가
    // UnitStat.Init에서 붙입니다. 그래야 시너지·퍽의 PercentAdd와 같은 괄호에서 합연산됩니다.
    // 적 유닛·로비 UI는 default(빈 목록)라 아무것도 안 붙습니다.
    public readonly PlayerUnitBonus PlayerBonus;
    
    //테스트용 값 직접 대입
    public UnitBaseStat(float atk, float maxHp, float critRate, float accuracy,
        float atkSpeed, float sg, float sgChargeAmount)
    {
        Atk = atk;
        MaxHp = maxHp;
        CritRate = critRate;
        Accuracy = accuracy;
        AtkSpeed = atkSpeed;
        SG = sg;
        SGChargeAmount = sgChargeAmount;
        PlayerBonus = default;
    }
    
    //유닛 원본 스탯. 레벨 미반영
    public UnitBaseStat(UnitData unitData)
    {
        Atk = unitData.Atk;
        MaxHp = unitData.MaxHp;
        CritRate = unitData.CriticalRate;
        Accuracy = unitData.Accuracy;
        AtkSpeed = unitData.AtkSpeed;
        SG = unitData.SG;
        SGChargeAmount = unitData.SGChargeAmount;
        PlayerBonus = default;
    }
    
    //레벨이 반영된 기본 스탯
    public UnitBaseStat(UnitStat calculated)
    {
        Atk = calculated.GetStat(EStatType.Atk);
        MaxHp = calculated.GetStat(EStatType.MaxHp);
        CritRate = calculated.GetStat(EStatType.CritRate);
        Accuracy = calculated.GetStat(EStatType.Accuracy);
        AtkSpeed = calculated.GetStat(EStatType.AtkSpeed);
        SG = calculated.GetStat(EStatType.SG);
        SGChargeAmount = calculated.GetStat(EStatType.SGChargeAmount);
        PlayerBonus = default;
    }

    public UnitBaseStat(UnitStat calculated, PlayerUnitBonus playerBonus) : this(calculated)
    {
        PlayerBonus = playerBonus;
    }
}

public class UnitStat
{
    private const float DMG_SCALE_BASE = 1f;
    
    private Dictionary<EStatType, Stat> statDict;

    public void Init(UnitBaseStat baseStat)
    {
        statDict = new Dictionary<EStatType, Stat>(9)
        {
            { EStatType.Atk, new Stat(baseStat.Atk) },
            { EStatType.MaxHp, new Stat(baseStat.MaxHp) },
            { EStatType.SG, new Stat(baseStat.SG) },
            { EStatType.SGChargeAmount, new Stat(baseStat.SGChargeAmount) },
            { EStatType.CritRate, new Stat(baseStat.CritRate) },
            { EStatType.Accuracy, new Stat(baseStat.Accuracy) },

            { EStatType.StatusEffectDmg, new Stat(DMG_SCALE_BASE) },
            { EStatType.DmgMultiplier, new Stat(DMG_SCALE_BASE) },
            { EStatType.HealMultiplier , new Stat(DMG_SCALE_BASE) },
            { EStatType.NormalAtkDmg, new Stat(DMG_SCALE_BASE) },
            { EStatType.SellPriceMultiplier , new Stat(DMG_SCALE_BASE) },

            { EStatType.AtkSpeed, new Stat(baseStat.AtkSpeed) },

            { EStatType.Evasion , new Stat(0f)},
            { EStatType.HealCrit , new Stat(0f)},
            { EStatType.ExecuteBonusRate , new Stat(0f)},

            { EStatType.CriDamageMultiplier , new Stat(InGameBalance.CRIT_MULTIPLIER)},
        };
        
        baseStat.PlayerBonus.ApplyTo(this);  //플레이어 보너스를 모디파이어로 붙임. Stat을 새로 만들 때마다(Init) 붙으니 뗄 필요 없음
    }

    public float GetStat(EStatType type)
    {
        return GetStatObject(type)?.Value ?? 0f;
    }
    
    
    public Stat GetStatObject(EStatType type)
    {
        if (statDict == null) return null;
        return statDict.TryGetValue(type, out Stat stat) ? stat : null;
    }

    public void AddModifier(EStatType type, StatModifier modifier)
    {
        GetStatObject(type)?.AddModifier(modifier);
    }

    public void RemoveAllModifiersFromSource(object source)
    {
        if (statDict == null) return;

        foreach (var stat in statDict.Values)
            stat.RemoveAllModifier(source);
        
    }
}
