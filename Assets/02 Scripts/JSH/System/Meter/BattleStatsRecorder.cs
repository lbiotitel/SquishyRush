using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// [미터기] 막대 한 줄 안의 색 구분.
/// 평타(주황) / 스킬(파랑) / 고정(흰색). 고정에는 상태이상(화상·독·출혈·지옥불), 가시, 감전,
/// 서든데스, 시너지 피해가 모두 들어갑니다.
/// </summary>
public enum EMeterLayer
{
    Attack = 0,
    Skill = 1,
    Fixed = 2,
}

/// <summary>[미터기] 유닛 한 명의 이번 전투 누적값.</summary>
public sealed class UnitMeterRecord
{
    /// <summary>가한 피해. EMeterLayer 순서(평타/스킬/고정). HP 감소 + 실드 흡수, 오버킬 제외.</summary>
    public readonly float[] Dealt = new float[3];

    /// <summary>받은 피해. 구성은 Dealt와 같습니다. 보호로 막힌 피해는 0이라 자연히 빠집니다.</summary>
    public readonly float[] Taken = new float[3];

    /// <summary>이 유닛이 넣은 실제 회복량(초과 회복 제외).</summary>
    public float Heal;

    /// <summary>이 유닛이 부여한 실드량(소모량이 아니라 부여량).</summary>
    public float Shield;
}

/// <summary>[미터기] 아군 시너지 하나(축 단위: 화염 결속, 드래곤)의 이번 전투 누적 피해.</summary>
public sealed class SynergyMeterRecord
{
    /// <summary>아이콘·이름용 대표 데이터. 같은 축의 어느 단계든 아이콘과 이름이 같습니다.</summary>
    public SynergyData Data;
    public float Dealt;
}

/// <summary>
/// [미터기] 전투 중 피해·회복·실드를 누적만 하는 기록기입니다.
///
/// 정렬·UI 갱신은 하지 않습니다. 전투가 끝나면 MeterSnapshot.Build가 여기서 값을 복사해 갑니다.
/// BattleManager가 하나를 들고 있고 Init마다 Reset합니다.
///
/// 공격마다 불리므로 EventManager를 거치지 않고 BattleUnit / StatusEffect가 직접 부릅니다.
/// </summary>
public class BattleStatsRecorder
{
    private readonly Dictionary<BattleUnit, UnitMeterRecord> units = new();
    private readonly Dictionary<int, SynergyMeterRecord> allySynergies = new();

    /// <summary>
    /// 새 전투를 시작하기 전에 비웁니다. BattleManager.Init에서 부릅니다.
    /// 지난 전투 값은 이미 MeterSnapshot으로 복사됐으므로 지워도 화면에는 영향이 없습니다.
    /// </summary>
    public void Reset()
    {
        units.Clear();
        allySynergies.Clear();
    }

    #region 기록

    /// <summary>
    /// 피해 1회를 기록합니다. BattleUnit.TakeDamage에서 HP를 깎은 직후 한 번만 부릅니다.
    ///
    /// credit      : 본 피해의 주인 (보통 때린 사람)
    /// baseAmount  : 본 피해 중 실제로 깎인 HP + 실드가 흡수한 양 (오버킬 제외)
    /// shockCredit : 감전 추가 피해의 주인 = 감전을 건 사람 (감전이 없으면 비어 있음)
    /// shockAmount : 감전 추가 피해 중 실제로 깎인 HP + 실드가 흡수한 양 (항상 고정/흰색)
    ///
    /// 가한 쪽과 받은 쪽에 같은 숫자를 넣으므로 두 탭의 합이 맞아떨어집니다.
    /// 보호로 막힌 피해는 TakeDamage가 여기 오기 전에 끝나므로 기록되지 않습니다.
    /// </summary>
    public void RecordDamage(BattleUnit victim, EDamageSource source,
        in DamageCredit credit, float baseAmount,
        in DamageCredit shockCredit, float shockAmount)
    {
        if (victim == null) return;
        if (baseAmount <= 0f && shockAmount <= 0f) return;

        // 시너지 피해는 source가 Skill로 들어와도(드래곤) 흰색입니다. 그래서 시너지 여부를 먼저 봅니다.
        EMeterLayer baseLayer = credit.IsSynergy ? EMeterLayer.Fixed : ToLayer(source);

        // 받은 피해: 주인이 없어도(서든데스) 맞은 유닛에게는 기록합니다.
        UnitMeterRecord taken = GetOrCreate(victim);
        taken.Taken[(int)baseLayer] += Mathf.Max(0f, baseAmount);
        taken.Taken[(int)EMeterLayer.Fixed] += Mathf.Max(0f, shockAmount);

        // 가한 피해: 주인이 없으면 건너뜁니다.
        // 본 피해는 때린 사람, 감전 추가분은 감전을 건 사람에게 갑니다.
        AddDealt(credit, baseLayer, Mathf.Max(0f, baseAmount));
        AddDealt(shockCredit, EMeterLayer.Fixed, Mathf.Max(0f, shockAmount));
    }

    /// <summary>
    /// 회복 1회를 기록합니다. BattleUnit.Heal에서 실제 회복량(healed)이 나온 뒤 부릅니다.
    /// healer는 회복을 "넣은" 유닛입니다. 재생(HotEffect)은 Caster, 흡혈 재능은 본인입니다.
    /// </summary>
    public void RecordHeal(BattleUnit healer, float amount)
    {
        if (healer == null || amount <= 0f) return;
        GetOrCreate(healer).Heal += amount;
    }

    /// <summary>
    /// 실드 부여 1회를 기록합니다. ShieldEffect가 Amount를 늘린 직후, 철벽(ScaleShields)이 늘린 직후 부릅니다.
    /// 시너지 몫 실드는 미터기에 행이 없으므로(시너지 행은 가한 피해 탭 전용) 버립니다.
    /// </summary>
    public void RecordShieldGrant(in DamageCredit credit, float amount)
    {
        if (amount <= 0f || credit.IsEmpty || credit.IsSynergy) return;

        if (credit.IsSplit)
        {
            // 실드는 지금 Split으로 오지 않지만, 오더라도 비율대로 나눠 넣습니다.
            float sum = WeightSum(credit);
            for(int i = 0; i < credit.SplitCredits.Count; i++)
                RecordShieldGrant(credit.SplitCredits[i], amount * SplitRatio(credit,i,sum));
            return;
        }

        if (credit.Unit != null) GetOrCreate(credit.Unit).Shield += amount;
    }
    
    #endregion

    #region 조회

    /// <summary>이 유닛의 기록이 있으면 돌려줍니다. 전투에서 아무것도 안 한 유닛은 없을 수 있습니다.</summary>
    public bool TryGetUnit(BattleUnit unit, out UnitMeterRecord record)
        => units.TryGetValue(unit, out record);

    /// <summary>피해를 넣은 아군 시너지 기록을 result에 채웁니다.</summary>
    public void GetAllySynergies(List<SynergyMeterRecord> result)
    {
        result.Clear();
        foreach (var pair in allySynergies) result.Add(pair.Value);
    }
    
    #endregion

    #region 코어

    /// <summary>
    /// 피해 출처 → 막대 색. 평타·스킬만 따로 칠하고 나머지는 전부 고정(흰색)입니다.
    /// DoT(상태이상·지옥불), Reflect(가시), Environment(서든데스) 모두 흰색입니다.
    /// </summary>
    private static EMeterLayer ToLayer(EDamageSource source) => source switch
    {
        EDamageSource.Attack => EMeterLayer.Attack,
        EDamageSource.Skill => EMeterLayer.Skill,
        _ => EMeterLayer.Fixed,
    };

    /// <summary>
    /// 가한 피해를 영수증의 주인에게 더합니다.
    ///   Split   → 비율대로 나눠서 각 주인에게 다시 이 함수를 부릅니다.
    ///   Synergy → 아군 시너지면 시너지 행에 더합니다. 적 시너지는 미터기에 나오지 않으므로 버립니다.
    ///   Unit    → 그 유닛의 해당 색에 더합니다.
    /// </summary>
    private void AddDealt(in DamageCredit credit, EMeterLayer layer, float amount)
    {
        if (amount <= 0f || credit.IsEmpty) return;

        if (credit.IsSplit)
        {
            // 람다를 쓰지 않고 직접 돕니다. 화상 틱마다 불리므로 클로저 할당을 피합니다.
            float sum = WeightSum(credit);
            for(int i = 0; i < credit.SplitCredits.Count; i++)
                AddDealt(credit.SplitCredits[i], layer, amount * SplitRatio(credit, i, sum));
            return;
        }

        if (credit.IsSynergy)
        {
            if (!credit.SynergyIsAlly) return;
            GetOrCreateSynergy(credit.Synergy).Dealt += amount;
            return;
        }
        
        GetOrCreate(credit.Unit).Dealt[(int)layer] += amount;
    }

    /// <summary>Split 영수증의 가중치 합.</summary>
    private static float WeightSum(in DamageCredit credit)
    {
        float sum = 0f;
        IReadOnlyList<float> weights = credit.SplitWeights;
        for (int i = 0; i < weights.Count; i++) sum += weights[i];
        return sum;
    }

    /// <summary>
    /// Split 영수증에서 i번째 주인이 가져갈 비율(0~1).
    /// </summary>
    private static float SplitRatio(in DamageCredit credit, int i, float sum)
        => sum > 0f ? credit.SplitWeights[i] / sum : 1f / credit.SplitCredits.Count;

    private UnitMeterRecord GetOrCreate(BattleUnit unit)
    {
        if (!units.TryGetValue(unit, out UnitMeterRecord record))
        {
            record = new UnitMeterRecord();
            units.Add(unit, record);
        }

        return record;
    }
    
    

    /// <summary>
    /// 시너지는 단계(2/4/6)가 아니라 축(화염 결속, 드래곤) 단위로 한 줄입니다.
    /// SynergyData는 단계마다 따로 있으므로 속성/종족 값으로 키를 만듭니다.
    /// </summary>
    private SynergyMeterRecord GetOrCreateSynergy(SynergyData data)
    {
        // 문자열 키는 드래곤 피해마다 할당이 생기므로 정수로 만듭니다. 속성은 0~, 종족은 100~.
        int key = data.IsElementSynergy ? (int)data.TargetElement : 100 + (int)data.TargetRace;

        if (!allySynergies.TryGetValue(key, out SynergyMeterRecord record))
        {
            record = new SynergyMeterRecord { Data = data };
            allySynergies.Add(key, record);
        }

        return record;
    }
    
    #endregion
}
