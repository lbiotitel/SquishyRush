using System.Collections.Generic;
using UnityEngine;

/// <summary>SP_0025~0028 (창조의 축복) ? 무작위 아군 1명 몰아주기.</summary>
public class RandomBuffSynergy : SynergySpecial<RandomBuffData>
{
    private readonly List<int> pool = new();
    
    public override void Apply(List<BattleUnit> targets, object source)
    {
        var rng = InGameManagers.Battle?.Rng;
        
        for (int i = 0; i < targets.Count; i++)
        {
            BattleUnit unit = targets[i];
            if (unit == null || !unit.IsAlive) continue;

            ApplyStatBuff(unit, source, rng);
            ApplyStatusBuffs(unit, rng);

        }    
    }

    private void ApplyStatBuff(BattleUnit unit, object source, System.Random rng)
    {
        IReadOnlyList<EStatType> candidates = Data.StatCandidates;
        if (candidates.Count == 0 || Data.Percent == 0f) return;
        
        EStatType picked = candidates[rng != null ? rng.Next(candidates.Count) : 0];

        Stat stat = unit.Stat.GetStatObject(picked);
        if (stat == null)
        {
            Logger.LogWarning($"[시너지] {picked}가 UnitStat.Init에 등록되어 있지 않습니다.");
            return;
        }
            
        stat.AddModifier(new StatModifier(Data.Percent * 0.01f, EStatModifireType.PercentAdd, 1, source));
            
        Logger.Log($"[시너지] 창조의 축복 - {unit.RuntimeId} {picked} +{Data.Percent:F0}%");
    }

    private void ApplyStatusBuffs(BattleUnit unit, System.Random rng)
    {
        if (Data.StatusPickCount <= 0) return;

        IReadOnlyList<string> ids = Data.StatusIds;
        if (ids.Count == 0) return;
        
        pool.Clear();
        for (int i = 0; i < ids.Count; i++) pool.Add(i);
        
        int pickCount = Mathf.Min(Data.StatusPickCount, pool.Count);
            
        for (int p = 0; p < pickCount; p++)
        {
            // 뽑은 자리에 마지막 원소를 옮기고 줄입니다.
            // RemoveAt(중간)은 뒤를 전부 당기지만 이 방식은 상수 시간입니다.
            int slot = rng != null ? rng.Next(pool.Count) : 0;
            int idIndex = pool[slot];
                
            pool[slot] = pool[pool.Count - 1];
            pool.RemoveAt(pool.Count - 1);
                
            if(!GlobalManagers.Resource.TryGetResource(ids[idIndex], out StatusEffectData statusData))
            {
                Logger.LogWarning($"[시너지] StatusEffectData '{ids[idIndex]}'를 찾지 못했습니다.");
                continue;
            }
            
            unit.ApplyStatus(statusData, unit, Data.StatckPerPick);
            
            Logger.Log($"[시너지] 창조의 축복 - {unit.RuntimeId} <- {statusData.NameKey}");
        }
    }
}
