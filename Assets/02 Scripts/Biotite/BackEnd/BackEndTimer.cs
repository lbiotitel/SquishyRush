using System;
using System.Threading;
using Cysharp.Threading.Tasks;


public class BackEndTimer
{

    private readonly TimeSpan interval = TimeSpan.FromMinutes(GlobalDefines.GameConstants.DATA_SAVE_INTERVAL);
    private CancellationTokenSource cts;
    private bool isStart = false;
    
    /// <summary>
    /// 타이머를 시작합니다.
    /// </summary>
    public void StartTimer()
    {
        if (isStart) return;
        isStart = true;
        cts = new CancellationTokenSource();
        TimeTick(cts.Token).Forget();
        
        Logger.Log("타이머 시작");
    }

    /// <summary>
    /// 
    /// 정해진 시간마다 이벤트를 알립니다.
    /// </summary>
    private async UniTask TimeTick(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            await UniTask.Delay(interval, cancellationToken: token);

            if (token.IsCancellationRequested)
                break;

            Logger.LogWarning("데이터 갱신 시간이에용");
            GlobalManagers.Event.NotifyEvent<OnDataSaveTimeEventData>(new OnDataSaveTimeEventData());
        }
    }

    
    /// <summary>
    /// 타이머를 정지하고 리소스를 해제합니다.
    /// </summary>
    public void StopTimer()
    {
        if (!isStart) return;
        isStart = false;
        cts?.Cancel();
        cts?.Dispose();
        cts = null;
        Logger.Log("타이머 정지");
    }
}