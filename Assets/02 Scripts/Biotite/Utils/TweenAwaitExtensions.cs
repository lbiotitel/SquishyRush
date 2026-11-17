using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;

public static class TweenAwaitExtensions
{
    /// <summary>
    /// 토큰이 취소되면 트윈을 Kill하고 OperationCanceledException이 발생합니다.
    /// </summary>
    public static async UniTask AwaitTween(this Tween tween, CancellationToken ct)
    {
        using (ct.Register(() => tween.Kill()))
        {
            await UniTask.WaitUntil(
                () => !tween.IsActive() || tween.IsComplete(),
                cancellationToken: ct);
        }
    }
}