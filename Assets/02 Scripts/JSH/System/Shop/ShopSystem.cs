using System;
using System.Collections.Generic;
using UnityEngine;

//상점 진열, 리롤, 구매를 담당하는 로직
public class ShopSystem
{
    public const int SLOT_COUNT = GlobalDefines.MAX_VALUE.IG_MAX_SLOT_COUNT;
    
    private readonly Func<int, ShopLevelSetting> settingProvider;
    private readonly ShopSlot[] slots = new ShopSlot[SLOT_COUNT];

    public IReadOnlyList<ShopSlot> Slots => slots;

    public event Action OnShopRefreshed;

    public ShopSystem(Func<int, ShopLevelSetting> settingProvider)
    {
        this.settingProvider = settingProvider;
        for (int i = 0; i < SLOT_COUNT; i++)
            slots[i] = new ShopSlot();
    }

    public ShopLevelSetting GetLevelSetting(int shopLevel) => settingProvider(shopLevel);

    #region 진열 / 리롤

    //4칸 새로 채움
    public void Roll(RunContext ctx)
    {
        ShopLevelSetting setting = settingProvider(ctx.IGShopLevel);

        for (int i = 0; i < SLOT_COUNT; i++)
        {
            ERarity rarity = PickRarity(setting, ctx);
            slots[i].Unit = PickUnit(rarity, ctx);
            slots[i].IsSold = false;
        }
        
        GlobalManagers.Audio.PlaySfx(GlobalDefines.ResourceKey.Audio.REROLL_SFX);
        OnShopRefreshed?.Invoke();
    }

    public int GetRerollCost(RunContext ctx)
    {
        ShopLevelSetting setting = settingProvider(ctx.IGShopLevel);
        int cost = InGameBalance.GetRerollCost(setting.RerollBaseCost, ctx.RerollCount);
        
        float reduction = InGameManagers.PlayerStat.GetStatValue(EStatType.RerollCostReduction);
        if (reduction <= 0f) return cost;
        
        return Mathf.Max(0, Mathf.CeilToInt(cost * (1f - reduction * 0.01f)));
    }

    //리롤 버튼 클릭 시 
    public EShopActionResult TryReroll(RunContext ctx)
    {
        int cost = GetRerollCost(ctx);
      
        if (!ctx.TrySpendGold(cost)) return EShopActionResult.NotEnoughGold;
        
        ctx.IncreaseRerollCount();
        Roll(ctx);
        return EShopActionResult.Success;
    }

    #endregion

    #region 구매

    //살수 있는지 확인
    public EShopActionResult CanBuy(int slotIndex, RunContext ctx, BoardModel board)
    {
        if (slotIndex < 0 || slotIndex >= SLOT_COUNT) return EShopActionResult.InvalidSlot;
        
        ShopSlot slot = slots[slotIndex];
        if (slot.IsEmpty) return EShopActionResult.SlotEmpty;

        if (!board.CanAcceptNewUnit(slot.Unit.UnitId)) return EShopActionResult.BenchFull;
        if (ctx.IGCurrency < InGameBalance.GetUnitCost(slot.Unit.UnitRarity)) return EShopActionResult.NotEnoughGold;

        return EShopActionResult.Success;
    }

    //구매 함수
    public EShopActionResult TryBuy(int slotIndex, RunContext ctx, BoardModel board)
    {
        EShopActionResult result = CanBuy(slotIndex, ctx, board);
        if (result != EShopActionResult.Success) return result;

        ShopSlot slot = slots[slotIndex];
        int cost = InGameBalance.GetUnitCost(slot.Unit.UnitRarity);
        
        if(!ctx.TrySpendGold(cost)) return EShopActionResult.NotEnoughGold;

        BattleUnit unit = BattleUnitFactory.CreatePlayerUnit(ctx, slot.Unit);
        if (!board.AddUnit(unit))
        {
            ctx.AddGold(cost, applyMultiplier: false);
            Logger.LogError("배치실패");
            return EShopActionResult.BenchFull;
        }

        slot.IsSold = true;
        OnShopRefreshed?.Invoke();
        
        GlobalManagers.Audio.PlaySfx(GlobalDefines.ResourceKey.Audio.PURCHASE_SFX);
        
        return EShopActionResult.Success;
    }

    #endregion

    #region 추첨

    //가중치로 등급 뽑기
    private ERarity PickRarity(ShopLevelSetting setting, RunContext ctx)
    {
        int total = 0;
        for (int i = 0; i < setting.RarityWeights.Length; i++) total += setting.RarityWeights[i];
        if (total <= 0) return ERarity.Normal;

        int roll = ctx.NextInt(total);
        int acc = 0;
        for (int i = 0; i < setting.RarityWeights.Length; i++)
        {
            acc += setting.RarityWeights[i];
            if (roll < acc) return (ERarity)i;
        }

        return ERarity.Normal;
    }

    //등급에서 유닛 뽑기
    private UnitData PickUnit(ERarity rarity, RunContext ctx)
    {
        for (int r = (int)rarity; r >= 0; r--)
        {
            var candidates = ctx.GetOwnedUnitsByRarity((ERarity)r);
            if(candidates == null || candidates.Count == 0) continue;

            return candidates[ctx.NextInt(candidates.Count)];
        }

        return null;
    }
    #endregion
}
