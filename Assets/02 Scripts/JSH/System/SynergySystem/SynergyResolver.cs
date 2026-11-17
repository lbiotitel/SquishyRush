using System.Collections.Generic;
using System.Text;

/// <summary>
/// 카운트를 "켜진 시너지 목록"으로 바꿉니다.
///
/// 축(속성/종족)마다 켜지는 시너지는 최대 하나입니다.
/// Fire가 6명이면 SY_0001(2명) / SY_0002(4명) / SY_0003(6명)이 전부 조건을 만족하지만,
/// 실제로 켜지는 건 가장 높은 SY_0003 하나뿐입니다. 하위 단계의 효과는 상위 단계 행에
/// 이미 포함되어 있습니다(SEG_0003에 상태이상 피해 50%가 그대로 들어 있는 이유).
///
/// 애셋은 한 번만 읽어 축별로 정렬해 둡니다. Resolve는 상점에서 배치가 바뀔 때마다
/// 불릴 수 있으므로 그때는 할당 없이 훑기만 합니다.
/// </summary>
public class SynergyResolver
{
    private readonly Dictionary<EElementType, List<SynergyData>> byElement = new();
    private readonly Dictionary<ERace, List<SynergyData>> byRace = new();
    
    public bool IsReady { get; private set; }

    /// <summary>
    /// SynergyData 애셋 전부를 받아 축별로 묶고 RequiredCount 오름차순으로 정렬합니다.
    /// 인게임 씬 진입 시 한 번만 부르면 됩니다.
    /// </summary>
    public void Init(IReadOnlyList<SynergyData> datas)
    {
        byElement.Clear();
        byRace.Clear();
        IsReady = false;

        if (datas == null) return;

        for (int i = 0; i < datas.Count; i++)
        {
            SynergyData data = datas[i];
            if (data == null) continue;

            if (data.TargetElement != EElementType.None)
            {
                GetOrCreate(byElement, data.TargetElement).Add(data);
            }
            else if (data.TargetRace != ERace.None)
            {
                GetOrCreate(byRace, data.TargetRace).Add(data);
            }
            else
            {
                Logger.LogWarning($"[시너지] '{data.name}'에 속성도 종족도 없습니다. 무시합니다.");
            }
        }

        foreach (var list in byElement.Values) Sort(list);
        foreach (var list in byRace.Values) Sort(list);

        IsReady = true;
        Logger.Log($"[시너지] 정의 로드 완료 - 속성 {byElement.Count}축 / 종족 {byRace.Count}축");
    }

    /// <summary>
    /// 현재 카운트로 켜진 시너지를 result에 채웁니다.
    /// result는 호출부가 들고 재사용합니다.
    /// </summary>
    public void Resolve(SynergyCounter counter, List<ActiveSynergy> result)
    {
        if (result == null) return;
        result.Clear();

        if (counter == null || !IsReady) return;

        foreach (var pair in byElement)
            TryAdd(pair.Value, counter.GetCount(pair.Key), result);

        foreach (var pair in byRace)
            TryAdd(pair.Value, counter.GetCount(pair.Key), result);
    }

    /// <summary>
    /// UI용. 인원이 1명 이상인 모든 축의 상태를 채웁니다 (안 켜진 축 포함).
    /// </summary>
    public void CollectStatus(SynergyCounter counter, List<SynergyStatus> result)
    {
        if (result == null) return;
        result.Clear();

        if (counter == null || !IsReady) return;

        foreach (var pair in byElement)
            AddStatus(pair.Value, counter.GetCount(pair.Key), result);

        foreach (var pair in byRace)
            AddStatus(pair.Value, counter.GetCount(pair.Key), result);
    }

    /// <summary>
    /// 켜진 단계 판정 규칙. Resolve(효과용)와 CollectStatus(UI용)가 같이 씁니다.
    /// 정렬해 뒀으므로 뒤에서부터 조건을 만족하는 첫 번째가 답입니다. 1부터, 없으면 0.
    /// </summary>
    private static int FindActiveTier(List<SynergyData> tiers, int count)
    {
        for (int i = tiers.Count - 1; i >= 0; i--)
            if (tiers[i].RequiredCount <= count)
                return i + 1;

        return 0;
    }

    /// <summary>
    /// 3성 보너스 후보를 채웁니다. 축마다 1단계 데이터 하나씩, 제외 종족은 뺍니다.
    /// 속성 5축 + 종족 8축 = 13개라 한 세션 최대 6개 노출(3칸 + 리롤 3번)에 충분합니다.
    /// </summary>
    public void CollectBonusCandidates(List<SynergyData> result)
    {
        result.Clear();
        if (!IsReady) return;
        
        foreach ( var pair in byElement)
            result.Add(pair.Value[0]);

        foreach (var pair in byRace)
        {
            if (SynergyBonusRule.IsExcludedRace(pair.Key)) continue;
            result.Add(pair.Value[0]);
        }
    }

    /// <summary> 한 축에서 켜진 최고 단계 하나를 효과용 목록에 추가합니다.</summary>
    private static void TryAdd(List<SynergyData> tiers, int count, List<ActiveSynergy> result)
    {
        int tier = FindActiveTier(tiers, count);
        if (tier == 0) return;
        
        result.Add(new ActiveSynergy(tiers[tier - 1], count, tier, tier == tiers.Count));
    }

    /// <summary> 한 축의 표시 상태를 UI용 목록에 추가합니다. 인원 0인 축은 제외.</summary>
    private static void AddStatus(List<SynergyData> tiers, int count, List<SynergyStatus> result)
    {
        if (count <= 0) return;
        
        result.Add(new SynergyStatus(tiers, count, FindActiveTier(tiers, count)));
    }
    
    /// <summary>다음 단계까지 몇 명 남았는지. UI 전용입니다. 없으면 false.</summary>
    public bool TryGetNextTier(SynergyCounter counter, SynergyData current, out int nextRequired)
    {
        nextRequired = 0;
        if (counter == null || current == null) return false;

        List<SynergyData> tiers = current.IsElementSynergy
            ? Find(byElement, current.TargetElement)
            : Find(byRace, current.TargetRace);

        if (tiers == null) return false;

        int count = current.GetCount(counter);
        for (int i = 0; i < tiers.Count; i++)
        {
            if (tiers[i].RequiredCount <= count) continue;

            nextRequired = tiers[i].RequiredCount;
            return true;
        }

        return false;
    }

    public string Dump(IReadOnlyList<ActiveSynergy> active)
    {
        if (active == null || active.Count == 0) return "(없음)";

        var sb = new StringBuilder();
        for (int i = 0; i < active.Count; i++)
        {
            ActiveSynergy a = active[i];
            string axis = a.Data.IsElementSynergy ? a.Data.TargetElement.ToString() : a.Data.TargetRace.ToString();
            
            sb.Append($"{axis} {a.TierIndex}단계({a.Count}/{a.Data.RequiredCount}) ");
        }

        return sb.ToString();
    }

    private static List<SynergyData> GetOrCreate<TKey>(Dictionary<TKey, List<SynergyData>> dict, TKey key)
    {
        if (!dict.TryGetValue(key, out var list))
        {
            list = new List<SynergyData>();
            dict[key] = list;
        }

        return list;
    }

    private static List<SynergyData> Find<TKey>(Dictionary<TKey, List<SynergyData>> dict, TKey key)
        => dict.TryGetValue(key, out var list) ? list : null;
    
    private static void Sort(List<SynergyData> list)
        =>list.Sort((a,b) => a.RequiredCount.CompareTo(b.RequiredCount)); 
}
