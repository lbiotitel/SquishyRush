using System.Collections.Generic;

/// <summary>
/// 유닛 하나에 붙일 플레이어 보너스 모디파이어 목록. UnitBaseStat에 실려 다닙니다.
/// default(목록 없음)면 아무것도 안 붙습니다 — 적 유닛, 로비 UI.
/// </summary>
public readonly struct PlayerUnitBonus
{
    public readonly struct Entry
    {
        public readonly EStatType Stat;
        public readonly EStatModifireType Type;
        public readonly float Value;

        public Entry(EStatType stat, EStatModifireType type, float value)
        {
            Stat = stat;
            Type = type;
            Value = value;
        }
    }

    private readonly Entry[] entries;

    public PlayerUnitBonus(Entry[] entries)
    {
        this.entries = entries;
    }

    public void ApplyTo(UnitStat stat)
    {
        if (entries == null || stat == null) return;
        
        for (int i = 0; i < entries.Length; i++)
        {
            Entry e = entries[i];
            stat.AddModifier(e.Stat, new StatModifier(e.Value, e.Type, 0, InGameBalance.PLAYER_BONUS_MODIFIER_SOURCE));
        }
    }
}

/// <summary>
/// PlayerStat(아티팩트 등 런 전체 보너스)을 아군 유닛에 붙일 모디파이어 목록으로 변환합니다.
///
/// UnitStatCalculator.CalculateWithPlayerBonus에서 유닛 종류별로 런당 한 번 호출되고,
/// 결과(PlayerUnitBonus)는 UnitBaseStat에 실려 RunContext에 캐시됩니다.
/// 실제로 붙는 곳은 UnitStat.Init이라, 유닛 생성·성급 상승으로 Stat이 새로 만들어질 때마다 다시 붙습니다.
///
/// PlayerStat의 값은 전부 "n" 그대로(10 = 10%)입니다. 유닛 스탯마다 단위가 달라서
/// 어떤 모디파이어로 옮길지는 아래 규칙 표가 정합니다.
///   - 기본값이 있는 스탯(공격력, 체력, 공속, SG 충전량)   : PercentAdd n%  → 시너지·퍽 %와 합연산
///   - 기본값 1인 배수 스탯(피해, 상태이상 피해, 치유량)   : PercentAdd n%  → 시너지·상태이상 %와 합연산
///   - 치명타 피해 배수(기본 1.5)                           : Add n * 0.01  → 150% → 150+n%
///   - %p 단위 스탯(치명타 확률, 회피)                      : Add n         → 20% → 20+n%
///   - 속성 공격력                                          : 자기 속성 하나만 공격력 PercentAdd n%
/// </summary>
/// 
public static class PlayerStatBonusApplier
{
    private readonly struct Rule
    {
        public readonly EStatType From;
        public readonly EStatType To;
        public readonly EStatModifireType Type;
        public readonly float Scale;

        public Rule(EStatType from, EStatType to, EStatModifireType type, float scale)
        {
            From = from;
            To = to;
            Type = type;
            Scale = scale;
        }
    }

    private const float PERCENT = 0.01f;

    private static readonly Rule[] rules =
    {
        new(EStatType.Atk, EStatType.Atk, EStatModifireType.PercentAdd, PERCENT),
        new(EStatType.MaxHp, EStatType.MaxHp, EStatModifireType.PercentAdd, PERCENT),
        new(EStatType.AtkSpeed, EStatType.AtkSpeed, EStatModifireType.PercentAdd, PERCENT),
        new(EStatType.SGChargeAmount, EStatType.SGChargeAmount, EStatModifireType.PercentAdd, PERCENT),

        new(EStatType.DmgMultiplier, EStatType.DmgMultiplier, EStatModifireType.PercentAdd, PERCENT),
        new(EStatType.StatusEffectDmg, EStatType.StatusEffectDmg, EStatModifireType.PercentAdd, PERCENT),
        new(EStatType.HealMultiplier, EStatType.HealMultiplier, EStatModifireType.PercentAdd, PERCENT),

        new(EStatType.CriDamageMultiplier, EStatType.CriDamageMultiplier, EStatModifireType.Add, PERCENT),

        new(EStatType.CritRate, EStatType.CritRate, EStatModifireType.Add, 1f),
        new(EStatType.Evasion, EStatType.Evasion, EStatModifireType.Add, 1f),
    };
    
    //이 유닛 종류에 붙일 보너스 목록. 값이 0인 규칙은 빠집니다.
    public static PlayerUnitBonus Build(UnitData unitData)
    {
        PlayerStatManager playerStat = InGameManagers.PlayerStat;
        if (playerStat == null) return default;

        var entries = new List<PlayerUnitBonus.Entry>(rules.Length + 1);

        for (int i = 0; i < rules.Length; i++)
        {
            Rule rule = rules[i];
            AddIfNonZero(entries, rule.To, rule.Type, playerStat.GetStatValue(rule.From) * rule.Scale);
        }
        
        //속성 보너스: 자기 속성 하나만. 공격력 PercentAdd라 전체 공격력 %·시너지 %와 합산됩니다.
        EStatType elementStat = ElementStatOf(unitData);
        if (elementStat != EStatType.None)
            AddIfNonZero(entries, EStatType.Atk, EStatModifireType.PercentAdd,
                playerStat.GetStatValue(elementStat) * PERCENT);

        return entries.Count == 0 ? default : new PlayerUnitBonus(entries.ToArray());
    }

    private static void AddIfNonZero(List<PlayerUnitBonus.Entry> entries,
        EStatType statType, EStatModifireType type, float value)
    {
        if (value == 0f) return;
        entries.Add(new PlayerUnitBonus.Entry(statType, type, value));
    }

    private static EStatType ElementStatOf(UnitData data)
    {
        if (data == null) return EStatType.None;

        return data.ElementType switch
        {
            EElementType.FIRE => EStatType.FireElementAtkBonus,
            EElementType.WATER => EStatType.WaterElementAtkBonus,
            EElementType.EARTH => EStatType.EarthElementAtkBonus,
            EElementType.LIGHT => EStatType.LightElementAtkBonus,
            EElementType.DARK => EStatType.DarkElementAtkBonus,
            _ => EStatType.None,
        };
    }
}
