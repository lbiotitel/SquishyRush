using System;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;


public enum ERoundResult
{
    None,
    Win,
    Lose
}

public class RunContext 
{
    //인게임 재화
    public int IGCurrency { get; private set; }
    //인게임 상점레벨
    public int IGShopLevel { get; private set; }
    //현재 레벨에서 쌓인 상점 경험치
    public int ShopExp { get; private set; }
    //현재 리롤 횟수
    public int RerollCount { get; private set; }
    //현재 진행중인 라운드
    public int RoundIdx { get; private set; }
    //현재 인게임의 유저 체력
    public int UserLife {get; private set;}
    // 지금까지 정산한 전투 수.
    public int BattleCount { get; private set; }
    //현재 계정에서 보유중인 유닛들 
    public IReadOnlyList<UnitData> OwnedUnits { get; private set; }
    
    public int Seed { get; private set; }
    
    private Dictionary<string, int> unitLevels;
    private Dictionary<string, UnitBaseStat> baseStatCache;
    private int nextRuntimeId = 1;
    
    private Dictionary<ERarity, List<UnitData>> unitsByRarity;
    private System.Random random;
    
    public event Action OnIGCurrencyChanged;
    public event Action<int> OnUserLifeChanged;
    public event Action OnRoundChanged;
    public event Action OnShopExpChanged;
    
    public void Init(int startCurrency, int startLife, int finalRound, List<UnitDataEntry> ownedUnits, int seed = 0)
    {
        Seed = seed != 0 ? seed : Environment.TickCount;
        random = new Random(Seed);
        
        IGCurrency = startCurrency;
        IGShopLevel = 1;
        ShopExp = 0;
        UserLife = startLife;
        RerollCount = 0;
        RoundIdx = 1;
        BattleCount = 0;
        FinalRound = finalRound;

        BuildStatCache(ownedUnits);
        
        Logger.Log($"[Run] 시드 {Seed} - 재화 {IGCurrency}, 보유유닛 {OwnedUnits.Count} 개");
    }

    //인게임 진입 시 1회만 유닛 레벨보너스를 계산해서 스탯에 저장해놓는다.
    private void BuildStatCache(List<UnitDataEntry> ownedUnits)
    {
        unitLevels = new Dictionary<string, int>();
        baseStatCache = new Dictionary<string, UnitBaseStat>();
        unitsByRarity = new Dictionary<ERarity, List<UnitData>>();

        List<UnitData> units = new List<UnitData>();
        OwnedUnits = units;

        if (ownedUnits == null)
        {
            Logger.LogWarning("[Run] 넘어온 유닛 목록이 null입니다.");
            return;
        }
        
        foreach (UnitDataEntry entry in ownedUnits)
        {
            if(string.IsNullOrEmpty(entry.UnitId)) continue;
            
            if(!GlobalManagers.Resource.TryGetResource<UnitData>(entry.UnitId, out UnitData unitData))
            {
                Logger.LogWarning($"[Run] UnitData '{entry.UnitId}'를 찾지 못했습니다.");
                continue;
            }

            string unitId = unitData.UnitId;
            if (string.IsNullOrEmpty(unitId))
            {
                Logger.LogWarning($"[Run] 에셋 '{unitData.name}'(어드레스 '{entry.UnitId}')의 UnitID가 비어 있어 제외합니다.");
                continue;
            }

            if (unitId != entry.UnitId)
                Logger.LogWarning($"[Run] 어드레스 '{entry.UnitId}'와 UnitID '{unitId}'가 다릅니다.");
            
            if (unitLevels.ContainsKey(unitId)) continue;
            
            unitLevels[unitId] = entry.Level;
            baseStatCache[unitId] = UnitStatCalculator.CalculateWithPlayerBonus(unitData, entry.Level);

            units.Add(unitData);
            
            if (!unitsByRarity.TryGetValue(unitData.UnitRarity, out var list))
            {
                list = new List<UnitData>();
                unitsByRarity[unitData.UnitRarity] = list;
            }

            list.Add(unitData);
        }
    }
    
    public int GetLevel(string unitId)
        => unitLevels.TryGetValue(unitId, out int lv) ? lv : 0;

    /// <summary>
    /// 유닛의 기본 스탯을 가져옵니다.
    /// 보유 목록에 없는 유닛은
    /// 레벨 보너스 없이 원본 스탯으로 계산해 캐시합니다.
    /// /// </summary>
    public UnitBaseStat GetBaseStat(UnitData unitData)
    {
        string unitId = unitData.UnitId;
        if (baseStatCache.TryGetValue(unitId, out UnitBaseStat cached)) return cached;

        UnitBaseStat stat = UnitStatCalculator.Calculate(unitData, GetLevel(unitId));
        baseStatCache[unitId] = stat;
        
        Logger.Log($"[Run] {unitId}는 보유 목록에 없어 레벨 {GetLevel(unitId)} 기준으로 기본 스탯을 계산했습니다.");
        return stat;
    }

    public bool TrySpendGold(int amount)
    {
        if (amount < 0 || IGCurrency < amount) return false;
        IGCurrency -= amount;
        OnIGCurrencyChanged?.Invoke();
        return true;
    }

    public void AddGold(int amount, bool applyMultiplier = true)
    {
        if (amount <= 0) return;

        if (!applyMultiplier)
        {
            IGCurrency += amount;
            OnIGCurrencyChanged?.Invoke();
            return;
        }
        
        float mult = InGameManagers.PlayerStat.GetStatValue(EStatType.GoldMultiplier);
        if (mult > 0f) amount = Mathf.RoundToInt(amount * (1f + mult * 0.01f));
        
        IGCurrency += amount;
        OnIGCurrencyChanged?.Invoke();
    }

    public void IncreaseRerollCount() => RerollCount++;
    
    public void ResetRerollCount() => RerollCount = 0;

    public void SetShopLevel(int level)
    {
        IGShopLevel = Mathf.Clamp(level, 1, InGameBalance.MAX_SHOP_LEVEL);
        ShopExp = 0;
        OnShopExpChanged?.Invoke();
    }

    public bool IsMaxShopLevel => IGShopLevel >= InGameBalance.MAX_SHOP_LEVEL;

    //다음 레벨까지 필요한 경험치(UI 용)
    public int ShopExpReq => InGameBalance.GetShopExpReq(IGShopLevel);

    public void AddShopExp(int amount)
    {
        if (amount <= 0 || IsMaxShopLevel) return;

        ShopExp += amount;

        while (!IsMaxShopLevel && ShopExp >= ShopExpReq)
        {
            ShopExp -= ShopExpReq;
            IGShopLevel++;
            Logger.Log($"[Shop] 상점 레벨업 → {IGShopLevel}");
        }

        if (IsMaxShopLevel) ShopExp = 0;
        
        OnShopExpChanged?.Invoke();
    }
    public int NextInt(int maxExclusive) => random.Next(maxExclusive);
    
    public bool IsRunOver => UserLife <= 0;

    public void LoseLife(int amount = 1)
    {
        UserLife = Mathf.Max(0, UserLife - amount);
        OnUserLifeChanged?.Invoke(UserLife);
    }

    public IReadOnlyList<UnitData> GetOwnedUnitsByRarity(ERarity rarity)
        => unitsByRarity.TryGetValue(rarity, out var list) ? list : null;

    /// <summary>
    /// 전투가 끝난 뒤 다음 라운드로 넘어가기 전 정산입니다.
    /// 수입표가 밀리지 않도록 현재 라운드 기준으로 지급한 뒤 라운드를 올립니다.
    /// 패배하면 같은 라운드를 다시 도전합니다.
    /// </summary>
    public void SettleRound(bool advanceRound, int incomeRound)
    {
        BattleCount++;
        AddGold(InGameBalance.GetRoundIncome(incomeRound));
        AddShopExp(InGameBalance.SHOP_EXP_PER_ROUND);
        
        if(advanceRound) NextRound();
    }
    
    public void NextRound()
    {
        RoundIdx++;
        OnRoundChanged?.Invoke();
    }

    public bool IsFinalRoundCleared => RoundIdx >= FinalRound;
    public int FinalRound { get; private set; }
    public int IssueRuntimeId() => nextRuntimeId++;
    
#if UNITY_EDITOR
    #region 디버그

    public void SetRound(int round)
    {
        RoundIdx = Mathf.Clamp(round, 1, FinalRound);
        OnRoundChanged?.Invoke();
    }

    #endregion
#endif
}
