using System.Collections.Generic;
using UnityEngine;

/// <summary>[미터기] 탭 세 개.</summary>
public enum EMeterTab
{
    Dealt = 0,     // 가한 피해 (평타/스킬/고정)
    Taken = 1,    // 받은 피해 (평타/스킬/고정)
    Support = 2,  // 회복·보호막 (치유/보호막)
}

/// <summary>
/// [미터기] 화면의 한 줄. 유닛 또는 아군 시너지 하나입니다.
///
/// 전투가 끝난 순간의 값을 복사해 둔 것이라 살아 있는 BattleUnit을 들고 있지 않습니다.
/// 상점에서 유닛을 팔거나 합성해도 이 줄은 바뀌지 않습니다.
/// </summary>
public sealed class MeterRow
{
    public Sprite Icon;
    public string Name;

    public int Star;
    public bool IsSynergy;

    public float DealtAttack, DealtSkill, DealtFixed;
    public float TakenAttack, TakenSkill, TakenFixed;
    public float Heal, Shield;

    /// <summary>
    /// 이 탭에 이 줄을 보여줄지 여부. 시너지 행은 가한 피해 탭에서만 나옵니다.
    /// 시너지는 맞지도 않고 회복·실드도 넣지 않기 때문입니다.
    /// </summary>
    public bool IsVisibleIn(EMeterTab tab) => !IsSynergy || tab == EMeterTab.Dealt;

    /// <summary>탭별 총합. 오른쪽 숫자이자 정렬 기준입니다.</summary>
    public float GetTotal(EMeterTab tab) => tab switch
    {
        EMeterTab.Dealt => DealtAttack + DealtSkill + DealtFixed,
        EMeterTab.Taken => TakenAttack + TakenSkill + TakenFixed,
        _ => Heal + Shield,
    };

    /// <summary>
    /// 막대 세 장의 fillAmount를 계산합니다. 누적 방식이라 뒤쪽 장일수록 길거나 같습니다.
    ///
    ///   피해 탭  back  = (평타+스킬+고정)/max  흰색
    ///            mid   = (평타+스킬)/max       파랑
    ///            front = 평타/max              주황
    ///   지원 탭  back  = (치유+보호막)/max     하늘색
    ///            mid   = 치유/max              초록
    ///            front = 0
    ///
    /// 시너지 행은 고정만 있으므로 mid·front가 자연히 0이 되어 흰색 한 장만 보입니다.
    /// </summary>
    public void GetFills(EMeterTab tab, float max, out float back, out float mid, out float front)
    {
        if (max <= 0f)
        {
            back = mid = front = 0f;
            return;
        }

        float a, s, f;
        switch (tab)
        {
            case EMeterTab.Dealt: a = DealtAttack; s = DealtSkill; f = DealtFixed;
                break;
            case EMeterTab.Taken: a = TakenAttack; s = TakenSkill; f = TakenFixed;
                break;
            default:
                back = Mathf.Clamp01((Heal + Shield) / max);
                mid = Mathf.Clamp01(Heal / max);
                front = 0f;
                return;
        }

        back = Mathf.Clamp01((a + s + f) / max);
        mid = Mathf.Clamp01((a + s) / max);
        front = Mathf.Clamp01(a / max);
    }
}

/// <summary>
/// [미터기] 한 전투의 결과표. 전투가 끝날 때 한 번 만들고, 다음 전투가 시작될 때까지 보여줍니다.
/// 정렬과 최댓값 계산도 여기 있어서 UI는 받은 순서대로 그리기만 합니다.
/// </summary>
public sealed class MeterSnapshot
{
    private const int TabCount = 3;
    
    private readonly List<MeterRow> rows = new();

    // 탭별로 미리 정렬해 둔 줄과 1등 값. 인덱스는 EMeterTab 값입니다.
    private readonly List<MeterRow>[] sortedByTab = { new(), new(), new() };
    private readonly float[] maxByTab = new float[TabCount];
    
    /// <summary>화면용 라운드 번호(재능상점 제외, 1~35).</summary>
    public int DisplayRound { get; private set; }
    public IReadOnlyList<MeterRow> Rows => rows;

    /// <summary>
    /// 기록기에서 아군 값만 복사해 결과표를 만듭니다. BattlePhase.EndBattle에서 부릅니다.
    ///
    /// 유닛은 기록 여부와 관계없이 아군 전투 칸에 있던 유닛 전부를 넣습니다.
    /// 아무것도 못 하고 죽은 유닛도 0으로 한 줄이 있어야 "이 유닛은 놀았다"가 보이기 때문입니다.
    /// 시너지는 피해를 넣은 것만 들어갑니다(화염 결속 시작 화상, 드래곤).
    ///
    /// ResetForNext보다 먼저 불러야 합니다. 그 뒤에는 전투 칸의 유닛 상태가 초기화됩니다.
    /// </summary>
    public static MeterSnapshot Build(BattleStatsRecorder recorder, BoardModel allyBoard, int displayRound)
    {
        var snapshot = new MeterSnapshot { DisplayRound = displayRound };
        if (recorder == null || allyBoard == null) return snapshot;

        List<BattleUnit> units = allyBoard.GetBattleUnits();
        for (int i = 0; i < units.Count; i++)
        {
            BattleUnit unit = units[i];
            if (unit?.CurrentUnitData == null) continue;

            var row = new MeterRow
            {
                Icon = unit.CurrentUnitData.UnitImage,
                Name = Localization.GetLocaliationText(Localization.LocalizationTable.UNIT,
                    unit.CurrentUnitData.UnitNameKey),
                Star = unit.Star,
                IsSynergy = false,
            };

            if (recorder.TryGetUnit(unit, out UnitMeterRecord rec))
            {
                row.DealtAttack = rec.Dealt[(int)EMeterLayer.Attack];
                row.DealtSkill = rec.Dealt[(int)EMeterLayer.Skill];
                row.DealtFixed = rec.Dealt[(int)EMeterLayer.Fixed];
                row.TakenAttack = rec.Taken[(int)EMeterLayer.Attack];
                row.TakenSkill = rec.Taken[(int)EMeterLayer.Skill];
                row.TakenFixed = rec.Taken[(int)EMeterLayer.Fixed];
                row.Heal = rec.Heal;
                row.Shield = rec.Shield;
            }
            
            snapshot.rows.Add(row);
        }

        var synergies = new List<SynergyMeterRecord>();
        recorder.GetAllySynergies(synergies);
        for (int i = 0; i < synergies.Count; i++)
        {
            SynergyData data = synergies[i].Data;
            snapshot.rows.Add(new MeterRow
            {
                Icon = data.DisplayIcon,
                Name = Localization.GetLocaliationText(Localization.LocalizationTable.Synergy, data.SynergyNameKey),
                Star = 0,
                IsSynergy = true,
                DealtFixed = synergies[i].Dealt,
            });
        }

        snapshot.BakeSortedTabs();
        return snapshot;
    }

    /// <summary>
    /// 이 탭의 정렬된 줄과 1등 값(Max_Value)을 돌려줍니다.
    /// Build에서 세 탭 모두 미리 정렬해 두었으므로 여기서는 꺼내기만 합니다.
    /// 탭을 몇 번 바꿔도 정렬은 다시 돌지 않습니다.
    /// </summary>
    public IReadOnlyList<MeterRow> GetSorted(EMeterTab tab, out float max)
    {
        max = maxByTab[(int)tab];
        return sortedByTab[(int)tab];
    }
    
    /// <summary>
    /// 이 탭에 보일 줄을 총합 내림차순으로 result에 채우고, 1등 값(Max_Value)을 돌려줍니다.
    ///
    /// 유닛과 시너지를 나누지 않고 한 번에 정렬합니다. 화염 결속 150이면 구구 198과 분홍 젤리 64 사이에 옵니다.
    /// 동점이면 유닛이 시너지보다 위, 그다음은 원래 순서(보드 칸 순서)를 지킵니다.
    /// List.Sort는 안정 정렬이 아니라서 원래 순번을 직접 비교합니다.
    /// </summary>
    public void BakeSortedTabs()
    {
        for (int t = 0; t < TabCount; t++)
        {
            EMeterTab tab = (EMeterTab)t;
            List<MeterRow> list = sortedByTab[t];
            list.Clear();
            
            for(int i = 0; i < rows.Count; i++)
                if(rows[i].IsVisibleIn(tab)) list.Add(rows[i]);
            
            list.Sort((x, y) =>
            {
                int byTotal = y.GetTotal(tab).CompareTo(x.GetTotal(tab));
                if (byTotal != 0) return byTotal;

                int bySynergy = x.IsSynergy.CompareTo(y.IsSynergy);
                if (bySynergy != 0) return bySynergy;
            
                return rows.IndexOf(x).CompareTo(rows.IndexOf(y));
            });

            // 정렬된 첫 줄이 1등이므로 그 값이 Max입니다.
            maxByTab[t] = list.Count > 0 ? list[0].GetTotal(tab) : 0f;
        }
    }
}
