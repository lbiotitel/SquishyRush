
//유닛 생성 지점
public static class BattleUnitFactory 
{

    
    public static BattleUnit CreatePlayerUnit(RunContext ctx, UnitData unitData, int star = 0)
    {
        BattleUnit unit = new BattleUnit();
        unit.Init(ctx.IssueRuntimeId(), unitData, ctx.GetLevel(unitData.UnitId), ctx.GetBaseStat(unitData), true ,star);
        return unit;
    }
    
    //적 유닛 - 라운드 기반으로 점점 강해지도록 설정 필요, 즉석으로 계산
    public static BattleUnit CreateEnemyUnit(RunContext ctx, UnitData unitData, int level, int star = 0)
    {
        UnitBaseStat baseStat = UnitStatCalculator.Calculate(unitData, level);
        
        BattleUnit unit = new BattleUnit();
        unit.Init(ctx.IssueRuntimeId(), unitData, level, baseStat, false ,star);
        return unit;
    }
}
