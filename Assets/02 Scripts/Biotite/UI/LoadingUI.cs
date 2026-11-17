using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class LoadingUI : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private float fadeInTime = 0.5f;
    [SerializeField] private float fadeOutTime = 1f;

    private CancellationTokenSource fadeCts;

    private void Awake()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();
    }

    private void Start()
    {
        SetState(0f, true);
    }

    public async UniTask FadeIn()
    {
        canvasGroup.blocksRaycasts = true;
        await FadeTo(1f, fadeInTime);
    }

    public async UniTask FadeOut()
    {
        await FadeTo(0f, fadeOutTime);
        canvasGroup.blocksRaycasts = false;
    }

    private async UniTask FadeTo(float targetA, float duration)
    {
        fadeCts?.Cancel();
        fadeCts?.Dispose();

        fadeCts = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
        CancellationToken token = fadeCts.Token;

        float startA = canvasGroup.alpha;
        float elapsed = 0f;

        try
        {
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                canvasGroup.alpha = Mathf.Lerp(startA, targetA, t);

                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }

            canvasGroup.alpha = targetA;
        }
        catch (System.OperationCanceledException)
        {
            return;
        }
    }

    private void SetState(float alpha, bool block)
    {
        canvasGroup.alpha = alpha;
        canvasGroup.blocksRaycasts = block;
    }

    private void OnDestroy()
    {
        canvasGroup.alpha = 0f;
        fadeCts?.Cancel();
        fadeCts?.Dispose();
    }
}