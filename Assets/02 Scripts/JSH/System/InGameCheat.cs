#if UNITY_EDITOR
/// <summary>
/// 에디터 전용 치트. ConsoleSystem(F1)과 UnitSpawnerWindow에서만 사용합니다.
/// InGameController의 public API만 사용하므로 컨트롤러에는 치트 코드가 들어가지 않습니다.
/// 전투 진입은 StartBattle(), 승리는 BattlePhase.EndBattle → FinishRound 경로를 그대로 탑니다.
/// </summary>
public class InGameCheat
{
    private readonly InGameController controller;

    public InGameCheat(InGameController controller)
    {
        this.controller = controller;
    }

    private RunContext Ctx => controller.Context;
    private BoardModel Board => controller.Board;
    private bool CanShopAction => controller.CurrentPhase == EInGamePhase.Shop 
                                  && !controller.IsMergeBusy && !controller.IsEliteBonusBusy;

    #region 라운드 / 스테이지 클리어

    /// <summary>
    /// 현재 라운드를 승리 처리합니다.
    /// 상점 페이즈면 StartBattle()로 전투에 들어간 뒤 즉시 승리, 전투 중이면 바로 승리.
    /// </summary>
    public bool ClearRound()
    {
        switch (controller.CurrentPhase)
        {
            case EInGamePhase.Shop:
                if (!CanEnterBattle()) return false;
                
                controller.ForceEnterBattle();

                if (controller.CurrentPhase != EInGamePhase.Battle) return false;
                return controller.BattlePhaseState.ForceEnd(isWin: true);
            
            case EInGamePhase.Battle:
                return controller.BattlePhaseState.ForceEnd(isWin: true);
            
            default:
                Logger.LogWarning($"[Cheat] {controller.CurrentPhase} 페이즈에서는 라운드 클리어를 쓸 수 없습니다.");
                return false;
        }
    }

    /// <summary>
    /// 마지막 라운드로 점프한 뒤 승리 처리합니다. → FinishRound에서 GameOver(isClear: true)
    /// </summary>
    public bool ClearStage()
    {
        switch (controller.CurrentPhase)
        {
            case EInGamePhase.Shop:
                // 라운드를 옮긴 뒤 전투 진입이 거절되면 라운드만 바뀐 채 남으므로 먼저 확인합니다.
                if (!CanEnterBattle()) return false;
                break;
            case EInGamePhase.Battle:
                if (!controller.BattlePhaseState.CanForceEnd)
                {
                    Logger.LogWarning("[Cheat] 전투 결과 처리 중에는 스테이지 클리어를 쓸 수 없습니다.");
                    return false;
                }

                break;
            
            default:
                Logger.LogWarning($"[Cheat] {controller.CurrentPhase} 페이즈에서는 스테이지 클리어를 쓸 수 없습니다.");
                return false;
        }

        Ctx.SetRound(Ctx.FinalRound);
        
        if(controller.CurrentPhase == EInGamePhase.Shop) controller.PrepareNextEnemy();

        return ClearRound();
    }
    
    private bool CanEnterBattle()
    {
        if (CanShopAction) return true;
        
        Logger.LogWarning("[Cheat] 합성 진행 중에는 클리어를 쓸 수 없습니다.");
        return false;
    }

    #endregion

    #region 유닛 스폰

    /// <summary>
    /// 아군 유닛을 벤치에 스폰합니다. 상점 페이즈(합성 중 아님)에서만 가능합니다.
    /// allowMerge가 true면 구매와 같은 규칙(AddUnit)으로 들어가 합성까지 일어납니다.
    /// </summary>
    public bool SpawnAlly(UnitData data, int star, bool allowMerge)
    {
        if (data == null) return false;
        if (!CanShopAction)
        {
            Logger.LogWarning("[Cheat] 아군 스폰은 상점 페이즈(합성 중 아님)에서만 가능합니다.");
            return false;
        }

        BattleUnit unit = BattleUnitFactory.CreatePlayerUnit(Ctx, data, star);

        if (allowMerge)
        {
            if (Board.AddUnit(unit)) return true;
        }
        else if (Board.TryGetEmptyBenchSlot(out SlotIndex slot))
        {
            return Board.Place(slot, unit);
        }
        
        Logger.LogWarning("[Cheat] 벤치가 가득 찼습니다.");
        return false;
    }

 
    /// <summary>
    /// 다음 라운드 적 보드에 유닛을 스폰합니다. 상점 페이즈에서만 가능합니다.
    /// slotIndex가 -1이면 전투 구역 0번부터 빈 칸을 찾습니다.
    /// </summary>
    public bool SpawnEnemy(UnitData data, int star, int level, int slotIndex = -1)
    {
        if (data == null) return false;

        BoardModel enemy = controller.NextEnemyBoard;
        if (controller.CurrentPhase != EInGamePhase.Shop || enemy == null)
        {
            Logger.LogWarning("[Cheat] 적 스폰은 상점 페이즈에서만 가능합니다.");
            return false;
        }

        if (!TryGetEmptySlot(enemy, slotIndex, out SlotIndex slot))
        {
            Logger.LogWarning("[Cheat] 적 보드에 놓을 빈 칸이 없습니다.");
            return false;
        }

        BattleUnit unit = BattleUnitFactory.CreateEnemyUnit(Ctx, data, level, star);
        if (!enemy.Place(slot, unit)) return false;

        // ShowEnemyBoard는 뷰를 전부 새로 만들기 때문에 기존 뷰를 먼저 지웁니다.
        controller.BoardView.ClearEnemyBoard();
        controller.BoardView.ShowEnemyBoard(enemy);
        return true;
    }

    private static bool TryGetEmptySlot(BoardModel board, int slotIndex, out SlotIndex slot)
    {
        if (slotIndex >= 0)
        {
            slot = new SlotIndex(EZone.Battle, slotIndex);
            return board.IsValid(slot) && board.IsEmpty(slot);
        }

        for (int i = 0; i < board.BattleCapacity; i++)
        {
            slot = new SlotIndex(EZone.Battle, i);
            if (board.IsEmpty(slot)) return true;
        }

        slot = default;
        return false;
    }
    
    #endregion
}
#endif
