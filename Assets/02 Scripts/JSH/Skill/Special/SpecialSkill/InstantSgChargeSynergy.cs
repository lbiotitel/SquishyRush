using System.Collections.Generic;


public class InstantSgChargeSynergy : SynergySpecial<InstantSgChargeData>
{
    public override void Apply(List<BattleUnit> targets, object source)
    {
        for (int i = 0; i < targets.Count; i++)
        {
            BattleUnit unit = targets[i];
            unit.RefillSG(Data.SgChargePercent);
            
            Logger.Log($"[시너지] 영혼의 공명 - {targets.Count}명 SG {Data.SgChargePercent:F0}% 충전");
        }
    }
}
