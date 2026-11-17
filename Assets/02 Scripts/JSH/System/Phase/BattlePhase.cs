using System.Collections.Generic;
using UnityEngine;

public class BattlePhase : InGamePhaseBase
{
    private enum EStep { Ready, Fighting, Result, Reward }

    private EStep step;
    private float readyTimer;
    private float resultTimer;
    private const float BattleSfxLeadTime = 0.2f;
    private bool battleSfxPlayed;
    private bool lastWin;

    private BoardModel enemyBoard;
    private readonly SynergySystem enemySynergy = new();
    
    public override EInGamePhase Type => EInGamePhase.Battle;
    
    public BattlePhase(InGameController c, InGamePhaseMachine m) : base(c, m) { }

    public override void Enter()
    {
        step = EStep.Ready;
        readyTimer = 0f;
        battleSfxPlayed = false;

        if (controller.NextEnemyBoard == null) controller.PrepareNextEnemy();

        enemyBoard = controller.NextEnemyBoard;
        
        InGameManagers.Battle.Init(controller.Board, enemyBoard, controller.Context.Seed + controller.Context.BattleCount);        
        
        ApplySynergy();
        PrewarmVfx();
        
        controller.GameStatusNotifier.Show(EGameStatus.BattleStart);
        Logger.Log($"[Battle] 준비 - 아군 {controller.Board.GetBattleUnits().Count}, 적 {enemyBoard.GetBattleUnits().Count}");
    }

    private void ApplySynergy()
    {
        enemySynergy.Init(controller.SynergyResolver);
        
        bool allyHpChanged = controller.AllySynergy.Apply(controller.Board, enemyBoard);
        bool enemyHpChanged = enemySynergy.Apply(enemyBoard, controller.Board);
        
        if(allyHpChanged) RestoreAll(controller.Board);
        if(enemyHpChanged) RestoreAll(enemyBoard);
    }

    private static void RestoreAll(BoardModel board)
    {
        foreach (BattleUnit unit in board.GetBattleUnits())
            unit.RestoreHp();
    }

    public override void Update(float deltaTime)
    {
        switch (step)
        {
            case EStep.Ready:
                UpdateReady(deltaTime);
                break;
                
            case EStep.Fighting:
                UpdateFighting(deltaTime);
                break;

            case EStep.Result:
                UpdateResult(deltaTime);
                break;
            case EStep.Reward:
                break;
        }
    }

    private void UpdateReady(float dt)
    {
        readyTimer += dt;
        InGameManagers.Battle.TickOpening(dt);

        if (!battleSfxPlayed && readyTimer >= controller.BattleReadyDelay - BattleSfxLeadTime)
        {
            battleSfxPlayed = true;
            GlobalManagers.Audio.PlaySfx(GlobalDefines.ResourceKey.Audio.BATTLE_START_SFX);
        }
        
        if (readyTimer < controller.BattleReadyDelay) return;
        
        step = EStep.Fighting;
        controller.GameStatusNotifier.Hide();
        Logger.Log("[Battle] 전투 시작");
        InGameManagers.Battle.StartBattle();
        UpdateFighting(dt);
    }

    private void UpdateFighting(float dt)
    {
        InGameManagers.Battle.Tick(dt);
        
        if(controller.Board.IsAllDead() || InGameManagers.Battle.SuddenDeath.IsTimeUp) EndBattle(false);
        else if(enemyBoard.IsAllDead()) EndBattle(true);
    }

    // 결과 배너를 보여주는 동안 기다립니다.
    private void UpdateResult(float dt)
    {
        resultTimer += Time.unscaledDeltaTime;
        if (resultTimer < controller.BattleResultDelay) return;

        FinishRound();
    }

    private void FinishRound()
    {
        var ctx = controller.Context;

        if (ctx.IsRunOver || (lastWin && ctx.IsFinalRoundCleared))
        {
            step = EStep.Reward;
            controller.GameOver(isClear: lastWin);
            Logger.Log($"[Run] 종료 - 라운드 {ctx.RoundIdx}, 라이프 {ctx.UserLife}");
            return;
        }
        
        if(lastWin && controller.IsEliteRound)
            controller.GiveEliteReward();

        int displayRound = controller.StageManager.CurrentStage.GetDisplayRound(ctx.RoundIdx);
        
        ctx.SettleRound(advanceRound: lastWin, incomeRound: displayRound);
        //Logger.Log(lastWin ? $"[Round] 정산 → 라운드 {ctx.RoundIdx}, 골드 {ctx.IGCurrency}, 상점 Lv{ctx.IGShopLevel} ({ctx.ShopExp}/{ctx.ShopExpReq})"
        //    : $"[Round] 패배 → {ctx.RoundIdx}라운드 재도전, 라이프 {ctx.UserLife}, 골드 {ctx.IGCurrency}");
        
        machine.ChangePhase(controller.ShopPhaseState);
    }

    public override void Exit()
    {
        controller.GameStatusNotifier.Hide();
        controller.AllySynergy.Clear();
        enemySynergy.Clear();
        
        controller.BoardView.ClearEnemyBoard();
        controller.BoardView.ClearDeadViews();
        
        foreach(BattleUnit unit in controller.Board.GetBattleUnits())
            unit.ResetForNext();
        
        if(enemyBoard != null)
            foreach (BattleUnit unit in enemyBoard.GetBattleUnits())
                unit.ResetForNext();
        
        InGameManagers.Battle.Release();
        enemyBoard = null;
        controller.ClearNextEnemy();
    }

    /// <summary>
    /// 승패가 갈린 순간. 결과 배너를 띄우고 결과 대기로 넘어갑니다.
    ///
    /// [미터기] 여기서 결과표를 굽습니다. 이 뒤로는 HP·상태이상이 더 바뀌지 않고(Tick이 멈춤),
    /// Exit의 ResetForNext보다 앞이라 아군 칸의 유닛 정보(성급·초상화)가 그대로입니다.
    /// 라운드 번호는 SettleRound 전이라 방금 싸운 라운드 번호입니다.
    /// </summary>
    private void EndBattle(bool isWin)
    {
        step = EStep.Result;
        resultTimer = 0f;
        lastWin = isWin;

        var ctx = controller.Context;
        //Logger.Log($"[Battle] 종료 - {(isWin ? "승리" : "패배")}");
        
        int displayRound = controller.StageManager.CurrentStage.GetDisplayRound(ctx.RoundIdx);
        controller.SetLastMeter(MeterSnapshot.Build(InGameManagers.Battle.Stats, controller.Board, displayRound));
        
        if(!isWin) ctx.LoseLife();
        
        controller.GameStatusNotifier.Show(isWin ? EGameStatus.Victory : EGameStatus.Defeat);
    }

    
    /// <summary>
    /// 이번 라운드에 등장하는 유닛들의 연출을 미리 풀에 채웁니다.
    ///
    /// PoolingSystem은 지연 생성이라, 손대지 않으면 각 스킬이 처음 나갈 때마다
    /// Instantiate가 한 번씩 일어나 전투 초반에 프레임이 계속 튑니다.
    ///
    /// Ready 단계(BattleReadyDelay) 동안 끝나므로 플레이어는 알아채지 못합니다.
    /// 아군과 적을 따로 넘기지 않는 이유는 VfxPrewarmer가 중복을 걸러내기 때문입니다.
    /// </summary>
    private void PrewarmVfx()
    {
        if (controller.VfxSystem == null) return;

        var units = new List<BattleUnit>(controller.Board.GetBattleUnits());
        units.AddRange(enemyBoard.GetBattleUnits());

        controller.VfxSystem.Prewarm(units);
    }
    
#if UNITY_EDITOR

    #region 디버그

    public bool CanForceEnd => step == EStep.Ready || step == EStep.Fighting;

    public bool ForceEnd(bool isWin)
    {
        if (!CanForceEnd) return false;
        EndBattle(isWin);
        return true;
    }
    
    #endregion
#endif
    
}
