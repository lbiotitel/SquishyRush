
using Unity.VisualScripting;

public static class UnitStatCalculator 
{
    public static UnitBaseStat Calculate(UnitData unitData, int level)
    {
        UnitStat stat = new UnitStat();
        stat.Init(new UnitBaseStat(unitData));

        ApplyLevelEntryBonuses(stat, unitData, level);

        return new UnitBaseStat(stat);
    }

    /// <summary>
    /// 로비 레벨 보너스를 구운 base + 플레이어 보너스(아티팩트) 모디파이어 목록.
    /// 런 시작 시 RunContext가 유닛별로 한 번 계산해 캐시합니다.
    /// 아티팩트는 base에 굽지 않고 UnitStat.Init에서 모디파이어로 붙어, 시너지·퍽 %와 합연산됩니다.
    /// PlayerStat이 채워진 뒤(InitArtifacts 이후)에 불려야 합니다.
    /// </summary>
    public static UnitBaseStat CalculateWithPlayerBonus(UnitData unitData, int level)
    {
        UnitStat stat = new UnitStat();
        stat.Init(new UnitBaseStat(unitData));
        
        ApplyLevelEntryBonuses(stat, unitData, level);

        //아티팩트는 여기서 계산에 섞지 않고 모디파이어 목록으로 만들어 같이 넘깁니다.
        return new UnitBaseStat(stat, PlayerStatBonusApplier.Build(unitData));
    }
    
    private static void ApplyLevelEntryBonuses(UnitStat stat, UnitData unitData, int level)
    {
        for (int lv = 1; lv <= level; lv++)
        {
            StatLevelBonus bonus = unitData.GetLevelBonus(lv);
            if (bonus == null || bonus.Value == 0f) continue;

            stat.AddModifier(bonus.StatType, new StatModifier(bonus.Value, EStatModifireType.Add, 0, "LevelBonus"));
        }
    }
    
}
