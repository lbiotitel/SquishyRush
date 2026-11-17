using System.Collections.Generic;

/// <summary>
/// 스탯을 증감시키는 상태이상. 18개 중 8개가 여기 속합니다.
/// 실명·광휘·광폭·동상·야수의 본능·충전·신속·건강.
///
/// 광폭만 모디파이어가 2개입니다(피해 +100%, 최대체력 -50%).
/// 엑셀에서 같은 StatusID를 Order만 바꿔 두 행으로 적은 것이 Modifiers 리스트로 들어옵니다.
/// </summary>
public class StatModifierEffect : StatusEffect
{
    //이 상태가 모디파이어를 붙인 Stat들. 해제할 때 되짚어서 뗍니다.
    private readonly List<Stat> applied = new();

    public override void OnApply(BattleUnit owner)
    {
        OnDetach(owner);

        if (owner.Stat == null) return;

        var modifiers = Data.Modifiers;
        for (int i = 0; i < modifiers.Count; i++)
        {
            StatusModifierEntry entry = modifiers[i];
            if (entry.statType == EStatType.None) continue;

            Stat stat = owner.Stat.GetStatObject(entry.statType);
            if (stat == null) continue;

            float raw = (i == 0 && OverrideValue.HasValue ? OverrideValue.Value : entry.modifierValue) * Stack;

            StatModifier mod = entry.modifierType == EValueModifierType.Flat
                ? new StatModifier(raw, EStatModifireType.Add, 0, this)
                : new StatModifier(raw * 0.01f, EStatModifireType.PercentAdd, 1, this);
            
            stat.AddModifier(mod);
            Logger.Log($"[스탯상태] {owner.RuntimeId} {Data.StatusId} override={(OverrideValue.HasValue ? OverrideValue.Value.ToString() : "없음")} raw={raw} -> {entry.statType}={stat.Value:F2}");
            applied.Add(stat);
            
        }
    }

    public override void OnDetach(BattleUnit owner)
    {
        for (int i = 0; i < applied.Count; i++)
        {
            applied[i].RemoveAllModifier(this);
        }
        
        applied.Clear();
    }
}
