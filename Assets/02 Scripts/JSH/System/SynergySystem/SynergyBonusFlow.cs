using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

/// <summary>
/// 3성 보너스 한 번을 처리합니다. 후보 수집 → 팝업 → 선택 대기 → 카운트 +1.
/// MergeFlow가 3성이 된 시점에 await 하므로, 선택이 끝날 때까지 다음 연쇄 합성이 진행되지 않습니다.
/// </summary>
public class SynergyBonusFlow
{
    private readonly BoardModel board;
    private readonly SynergyResolver resolver;
    private readonly SynergySystem synergy;

    public SynergyBonusFlow(BoardModel board, SynergyResolver resolver, SynergySystem synergy)
    {
        this.board = board;
        this.resolver = resolver;
        this.synergy = synergy;
    }

    public UniTask RunAsync(BattleUnit unit, CancellationToken ct)
        => RunAsync<SynergyBonusUI>(ct);
    
    public async UniTask RunAsync<TUI>(CancellationToken ct) where TUI : SynergyBonusUI
    {
        // 세션이 풀 참조를 들고 있으므로 매번 새 리스트를 씁니다. 3성 때만 불려서 할당 부담은 없습니다.
        var pool = new List<SynergyData>();
        resolver. CollectBonusCandidates(pool);
        if (pool.Count == 0)
        {
            Logger.LogWarning($"[시너지 보너스] {typeof(TUI).Name} 후보 시너지가 없습니다. 건너뜁니다.");
            return;
        }

        var tcs = new UniTaskCompletionSource<SynergyData>();
        
        var data = new SynergyBonusUIData
        {
            Session = new SynergyBonusSession(pool),
            Counter = synergy.Counter,
            OnPicked = picked => tcs.TrySetResult(picked),
        };

        if (InGameManagers.UI.PopUpContainer.OpenUI<TUI>(data) == null)
        {
            Logger.LogError($"[시너지 보너스] {typeof(TUI).Name} 팝업을 열지 못했습니다.");
            return;
        }
        
        // 씬 종료로 취소되면 OperationCanceledException이 MergeFlow까지 올라갑니다.
        SynergyData result;
        using (ct.Register(() => tcs.TrySetCanceled()))
        {
            result = await tcs.Task;
        }

        synergy.AddPermanentBonus(result, board);
    }
}
