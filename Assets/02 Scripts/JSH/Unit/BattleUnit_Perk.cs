using System;
using System.Collections.Generic;

/// <summary>
/// BattleUnit의 재능 보유 부분입니다.
///
/// 유닛이 하는 일은 세 가지뿐입니다.
///  - 어떤 재능을 들고 있는지 보관
///  - 재능을 받을 수 있는지 규칙 판정 (개수 제한, 중복불가)
///  - 생명주기 시점을 재능에게 전달
/// 효과를 언제 어떻게 적용할지는 각 PerkBase가 결정합니다.
/// </summary>
public partial class BattleUnit
{
    private readonly List<PerkBase> perks = new();

    // 성급 상승으로 얻은, 아직 사용하지 않은 재능 선택권
    private bool hasPerkPick;
    
    public IReadOnlyList<PerkBase> Perks => perks;

    // 합성 재배치용 버퍼. 합성은 한 번에 하나씩만 일어나므로 static으로 공유해도 안전합니다.
    private static readonly List<PerkDataBase> mergePool = new();
    private static readonly List<(PerkDataBase data, int order)> mergeSorted = new();
    
    /// <summary>
    /// 재능 선택 UI를 띄워야 하는지 여부입니다.
    /// 최대치 미만일 때만 true입니다.
    /// </summary>
    public bool NeedsPerkPick
        => hasPerkPick && perks.Count < InGameBalance.MAX_PERK_COUNT;

    public event Action OnPerkChanged;
    
    #region 재능 판정

    /// <summary>
    /// 해당 ID의 재능을 이미 보유하고 있는지 확인합니다.
    /// </summary>
    public bool HasPerk(string perkId) => perks.Exists(p => p.PerkId == perkId);


    public EPerkEquipResult CheckEquip(PerkDataBase data)
    {
        if (data == null) return EPerkEquipResult.NoPerk;
        if (data.IsConsumable) return EPerkEquipResult.Success;
        if (perks.Count >= InGameBalance.MAX_PERK_COUNT) return EPerkEquipResult.Full;
        if (!data.CanDuplicate && HasPerk(data.PerkId)) return EPerkEquipResult.Duplicate;

        return EPerkEquipResult.Success;
    }

    // 판정 규칙을 CheckEquip 한 곳에만 둡니다.
    // 조건을 여기에 따로 쓰면 소모형 예외가 적용되지 않아 두 판정이 갈라집니다.
    public bool CanAddPerk(PerkDataBase data)
        => CheckEquip(data) == EPerkEquipResult.Success;

    #endregion

    #region 재능 보유

    /// <summary>
    /// 재능 데이터로 런타임 재능을 만들어 보유 목록에 추가합니다.
    /// inherited가 true면 합성 상속으로 받은 것이라 OnInherited를, 아니면 OnAcquired를 알립니다.
    /// </summary>
    public bool AddPerk(PerkDataBase data, bool inherited = false)
    {
        if (!CanAddPerk(data)) return false;

        PerkBase perk = PerkFactory.Create(this, data);
        if (perk == null) return false;

        // 소모형은 효과만 실행하고 목록에 넣지 않습니다.
        if (data.IsConsumable)
        {
            perk.OnAcquired();
            return true;
        }
        
        perks.Add(perk);
        if(inherited) perk.OnInherited();
        else perk.OnAcquired();
        
        OnPerkChanged?.Invoke();
        return true;
    }
    
    /// <summary>
    /// 보유한 모든 재능을 해제하고 목록을 비웁니다.
    /// 해제한 재능을 returned에 담아 돌려줍니다. 인벤토리에 넣을지는 호출자가 결정합니다.
    /// 유닛이 보드에서 완전히 사라질 때 호출합니다.
    /// 플레이어 스탯에 붙은 재능이 남지 않게 하려는 것입니다.
    /// </summary>
    public void ReleasePerks(List<PerkDataBase> returned = null)
    {
        foreach(PerkBase perk in perks)
        {
            perk.OnRemoved();
            returned?.Add(perk.Data);
        }
        
        perks.Clear();
        OnPerkChanged?.Invoke();
    }
    
    /// <summary>
    /// 합성 시 두 유닛의 재능을 모아 우선순위대로 survivor에게 다시 장착합니다.
    ///
    /// 우선순위
    ///  1. 등급 높은 순 (Legend → Unique → Rare → Normal)
    ///  2. 같은 등급이면 prior(필드 유닛) 쪽 먼저
    ///  3. 그래도 같으면 먼저 장착한 순서
    ///
    /// survivor의 재능도 한 번 해제했다가 다시 넣습니다. 우선순위가 낮으면 survivor 자신의 재능도 튕겨야 하기 때문입니다.
    /// 다시 넣을 때는 inherited로 넣어서 지속 효과만 다시 겁니다.
    /// 못 들어간 재능(칸 초과, 중복불가)은 rejected로 돌려줍니다.
    /// </summary>
    public static void MergePerks(BattleUnit survivor, BattleUnit absorbed, BattleUnit prior,
        List<PerkDataBase> rejected)
    {
        if (survivor == null || absorbed == null) return;
        BattleUnit other = prior == survivor ? absorbed : survivor;
        
        // prior 먼저, 그다음 other 순서로 모읍니다. 모은 순서 자체가 2, 3순위 규칙이 됩니다.
        mergePool.Clear();
        prior.TakeAllPerks(mergePool);
        other.TakeAllPerks(mergePool);
        
        // List.Sort는 같은 값끼리의 순서를 보장하지 않으므로, 모은 순서를 같이 들고 정렬해서 동점일 때 사용합니다.
        mergeSorted.Clear();
        for(int i = 0; i < mergePool.Count; i++) mergeSorted.Add((mergePool[i], i));
        
        mergeSorted.Sort((a,b) =>
        {
            int byRarity = b.data.PerkRarity.CompareTo(a.data.PerkRarity); // 높은 등급 먼저
            return byRarity != 0 ? byRarity : a.order.CompareTo(b.order);
        });

        foreach (var (data, _) in mergeSorted)
            if (!survivor.AddPerk(data, inherited: true))
                rejected?.Add(data);
            
    }

    /// <summary>
    /// 보유 재능을 모두 해제하고 데이터만 목록에 담습니다. (합성 재배치용)
    /// </summary>
    private void TakeAllPerks(List<PerkDataBase> into)
    {
        foreach (PerkBase perk in perks)
        {
            perk.OnRemoved();
            into.Add(perk.Data);
        }
        
        perks.Clear();
        OnPerkChanged?.Invoke();
    }
    
    #endregion

    #region 생명주기 전달

    /// <summary>
    /// 스탯이 새로 만들어졌음을 모든 재능에 알립니다.
    /// RebuildStat에서 Star 모디파이어 적용 직후, hp 리소스 생성 전에 호출해야
    /// MaxHp 관련 재능이 체력에 반영됩니다.
    /// </summary>
    private void NotifyPerksStatRebuilt()
    {
        foreach (PerkBase perk in perks) perk.OnStatRebuilt();
    }

    /// <summary>
    /// 전투 진입을 모든 재능에 알립니다.
    /// SkillTriggerSystem.Init 이후에 호출되어야 트리거 재능의 리스너가 유지됩니다.
    /// </summary>
    public void EnterBattlePerks()
    {
        foreach(PerkBase perk in perks) perk.OnBattleEnter();
    }

    /// <summary>
    /// 전투 종료를 모든 재능에 알립니다.
    /// </summary>
    public void ExitBattlePerks()
    {
        foreach (PerkBase perk in perks) perk.OnBattleExit();
    }

    #endregion
}
