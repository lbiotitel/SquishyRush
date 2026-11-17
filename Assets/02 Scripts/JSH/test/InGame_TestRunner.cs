using System.Collections.Generic;
using UnityEngine;

public class InGame_TestRunner : MonoBehaviour
{
    [SerializeField] private List<UnitData> testUnits = new List<UnitData>();

    [SerializeField] private int battleRows = 2;
    [SerializeField] private int battleCols = 3;

    [SerializeField] private int benchCapacity = 5;

    private RunContext ctx;

    private BoardModel board;
    // Start is called before the first frame update
    void Start()
    {
        if (testUnits.Count == 0)
        {
            Logger.Log("testUnits.Count is 0");
            return;
        }

        ctx = new RunContext();
        ctx.Init(10, 3, 3, null);
        
        board = new BoardModel(battleRows, battleCols, benchCapacity);
        
        Test_RuntimeId();
        Test_StarMultiplier();
        Test_MergeChain();
        Test_MoveAndSwap();
        Test_TeamHp();
        Test_ShopDistribution();
        Test_RerollCost();
        Test_Buy();
    }

    //--- 1. RuntimeId가 순차 발급되는가
    private void Test_RuntimeId()
    {
        Logger.Log("===== 1. RuntimeId =====");

        var a = BattleUnitFactory.CreatePlayerUnit(ctx, testUnits[0]);
        var b = BattleUnitFactory.CreatePlayerUnit(ctx, testUnits[0]);

        Logger.Log($"a #{a.RuntimeId} / b #{b.RuntimeId}  → 서로 달라야 함");
    }

    //--- 2. 별 배율이 의도대로 곱해지는가 (PercentMult 검증)
    private void Test_StarMultiplier()
    {
        Logger.Log("===== 2. 별 배율 =====");

        var unit = BattleUnitFactory.CreatePlayerUnit(ctx, testUnits[0]);
        float baseAtk = unit.Stat.GetStat(EStatType.Atk);
        Logger.Log($"기본 Atk {baseAtk} / Hp {unit.Stat.GetStat(EStatType.MaxHp)}");
        for (int star = 0; star <= GlobalDefines.MAX_VALUE.IG_MAX_STAR; star++)
        {
            unit.SetStar(star);
            float atk = unit.Stat.GetStat(EStatType.Atk);
            float actual = baseAtk > 0 ? atk / baseAtk : 0f;
            //float expected = InGameBalance.GetAtkMult(star);

            //Logger.Log($"{star}성 → Atk {atk:F1} (배율 {actual:F2} / 기대 {expected:F2})");
            
        }
    }

    //--- 3. 0성 4개 투입 시 2성 1개로 연쇄되는가
    private void Test_MergeChain()
    {
        Logger.Log("===== 3. 연쇄 합성 =====");

        for (int i = 0; i < 4; i++)
        {
            var unit = BattleUnitFactory.CreatePlayerUnit(ctx, testUnits[0]);

            bool ok = board.AddUnit(unit);
            Logger.Log($"{i + 1}번째 투입 → {(ok ? "성공" : "실패")}");
            DumpBench();
        }

        Logger.Log("기대 결과: 2성 1마리만 남음");
    }

    private void Test_MoveAndSwap()
    {
        Logger.Log("===== 4. 이동 / 스왑 =====");

        //앞 테스트 결과와 섞이지 않게 새 보드로
        var b = new BoardModel(battleRows, battleCols, benchCapacity);
        var u1 = BattleUnitFactory.CreatePlayerUnit(ctx, testUnits[0]);
        var u2 = BattleUnitFactory.CreatePlayerUnit(ctx, testUnits[0]);

        var bench0 = new SlotIndex(EZone.Bench, 0);
        var battle3 = new SlotIndex(EZone.Battle, 3);

        b.Place(bench0, u1);
        Logger.Log($"이동: Bench[0] #{u1.RuntimeId} → Battle[3]");
        b.Move(bench0, battle3);
        Logger.Log($"  Battle[3]=#{b.Get(battle3)?.RuntimeId} / Bench[0]={(b.IsEmpty(bench0) ? "비었음" : "남음(버그)")}");

        b.Place(bench0, u2);
        Logger.Log($"스왑: Bench[0] #{u2.RuntimeId} ↔ Battle[3] #{u1.RuntimeId}");
        b.Move(bench0, battle3);
        Logger.Log($"  Battle[3]=#{b.Get(battle3)?.RuntimeId} / Bench[0]=#{b.Get(bench0)?.RuntimeId}");
    }
   
    //--- 5. 전투 배치대 유닛만 팀 HP에 잡히는가
    private void Test_TeamHp()
    {
        Logger.Log("===== 5. 개별 체력 =====");

        var bench1 = new SlotIndex(EZone.Bench, 1);
        board.Move(bench1, new SlotIndex(EZone.Battle, 0));

        foreach (var u in board.GetBattleUnits())
            Logger.Log($"  #{u.RuntimeId} {u.Star}성 Hp {u.CurrentHp:F0}/{u.MaxHp:F0}");

        Logger.Log($"전투 배치 {board.GetBattleUnits().Count}마리 → 합계 {board.GetTotalMaxHp()}");

        //데미지 후 전멸 판정
        var target = board.Get(new SlotIndex(EZone.Battle, 0));
        target?.TakeDamage(9999f, null);
        Logger.Log($"치명타 후 → 생존 {target?.IsAlive}, 전멸 {board.IsAllDead()}");
    }

    private void DumpBench()
    {
        string log = "  Bench: ";
        for (int i = 0; i < board.BenchCapacity; i++)
        {
            var u = board.Get(new SlotIndex(EZone.Bench, i));
            log += u == null ? "[ - ] " : $"[#{u.RuntimeId} {u.Star}성] ";
        }
        Logger.Log(log);
    }
    
    //--- 6. 등급 분포
    private void Test_ShopDistribution()
    {
        Logger.Log("===== 6. 상점 등급 분포 =====");

        ctx.SetShopLevel(3);
        var shop = new ShopSystem(GetShopSetting);
        var dist = new Dictionary<ERarity, int>();
        int total = 0;

        for (int i = 0; i < 1000; i++)
        {
            shop.Roll(ctx);
            foreach (var slot in shop.Slots)
            {
                if (slot.Unit == null) continue;
                var r = slot.Unit.UnitRarity;
                dist[r] = dist.TryGetValue(r, out int c) ? c + 1 : 1;
                total++;
            }
        }

        foreach (var kv in dist)
            Logger.Log($"{kv.Key}: {kv.Value} ({(float)kv.Value / total * 100f:F1}%)");
        
        ctx.SetShopLevel(1);
    }

    //--- 7. 리롤 비용 곡선
    private void Test_RerollCost()
    {
        Logger.Log("===== 7. 리롤 비용 =====");

        var shop = new ShopSystem(GetShopSetting);
        ctx.ResetRerollCount();

        string log = "";
        for (int i = 0; i < 12; i++)
        {
            log += $"{shop.GetRerollCost(ctx)} ";
            ctx.IncreaseRerollCount();
        }
        Logger.Log($"비용: {log}  (기대: 1 2 2 2 3 3 4 4 5 6 7 8)");
        ctx.ResetRerollCount();
    }

    //--- 8. 구매
    private void Test_Buy()
    {
        Logger.Log("===== 8. 구매 =====");

        var b = new BoardModel(battleRows, battleCols, benchCapacity);
        var shop = new ShopSystem(GetShopSetting);

        for (int i = 0; i < 8; i++)
        {
            shop.Roll(ctx);
            EShopActionResult can = shop.CanBuy(0, ctx, b);
            EShopActionResult ok = shop.TryBuy(0, ctx, b);
            Logger.Log($"{i + 1}회차 → CanBuy={can}, 결과={ok}, 잔액={ctx.IGCurrency}");
            if (ok == EShopActionResult.Success) break;
        }
    }

    //임시 하드코딩. ShopLevelChartData 준비되면 교체
    private ShopLevelSetting GetShopSetting(int shopLevel)
    {
        switch (shopLevel)
        {
            case 1:  return new ShopLevelSetting(new[] { 100,  0,  0,  0 }, 1);
            case 2:  return new ShopLevelSetting(new[] {  75, 25,  0,  0 }, 1);
            case 3:  return new ShopLevelSetting(new[] {  55, 30, 15,  0 }, 1);
            case 4:  return new ShopLevelSetting(new[] {  40, 33, 22,  5 }, 1);
            default: return new ShopLevelSetting(new[] {  25, 35, 30, 10 }, 1);
        }
    }
}
