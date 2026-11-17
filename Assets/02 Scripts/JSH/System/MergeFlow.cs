using System;
using System.Threading;
using Cysharp.Threading.Tasks;

/// <summary>
/// 합성 흐름을 진행합니다.
/// 합성 1단계 → (3성이면 보너스 선택 대기) → 다음 단계 를 순서대로 이어서,
/// 연쇄 합성이어도 "2성 탄생 → 3성 탄생 → 보너스 선택" 순서가 보장됩니다.
///
/// 진행 중에는 IsBusy가 true이며, InGameController가 이 동안 조작을 막습니다.
/// </summary>
public class MergeFlow
{
    private readonly BoardModel board;
    private readonly SynergyBonusFlow bonusFlow;
    private readonly CancellationTokenSource cts = new();
    
    public bool IsBusy { get; private set; }

    /// <summary>
    /// 보드의 합성 이벤트를 구독합니다.
    /// </summary>
    public MergeFlow(BoardModel board, SynergyBonusFlow bonusFlow)
    {
        this.board = board;
        this.bonusFlow = bonusFlow;

        board.OnUnitMerged += OnUnitMerged;
    }

    /// <summary>
    /// 구독을 해제하고 진행 중인 선택을 취소합니다. 씬 종료 시 호출합니다.
    /// </summary>
    public void Release()
    {
        board.OnUnitMerged -= OnUnitMerged;
        cts.Cancel();
        cts.Dispose();
    }

    /// <summary>
    /// 구매로 첫 합성이 일어났을 때 흐름을 시작합니다.
    /// </summary>
    private void OnUnitMerged(BattleUnit unit)
    {
        if (IsBusy)
        {
            // 조작 가드가 있어 정상적으로는 올 수 없음
            Logger.LogError("[Merge] 합성 흐름이 진행 중인데 새 합성이 발생했습니다.");
            return;
        }

        RunAsync(unit).Forget();
    }

    /// <summary>
    /// 합성된 유닛이 3성이면 보너스 선택을 기다리고, 이어서 합성할 수 있으면 한 단계씩 계속합니다.
    /// </summary>
    private async UniTaskVoid RunAsync(BattleUnit unit)
    {
        IsBusy = true;

        try
        {
            BattleUnit current = unit;

            do
            {
                // 이 시점의 current는 방금 합성으로 성급이 오른 유닛입니다.
                // 루프 한 바퀴에 한 단계씩 오르므로 3성 판정은 정확히 한 번만 걸립니다.
                if (SynergyBonusRule.IsBonusStar(current.Star))
                    await bonusFlow.RunAsync(current, cts.Token);
            } while (board.TryMergeStep(current, out current));
        }
        catch (OperationCanceledException)
        {
            // 씬 종료 등으로 선택이 취소됨
        }
        finally
        {
            IsBusy = false;
        }
    }
}
