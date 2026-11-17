using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


public struct GachaResultDatas
{
    public string ResultName;
    public ERarity ResultRarity;
    public Sprite ResultImage;
}


public class GachaEffectUIData : BaseUIData
{
    public List<GachaResultDatas> gachaResults;
}


//가챠 연출 UI에 대한 정의 입니다
public class GachaResultUI : BaseUI, IPointerClickHandler
{
    private enum EGachaState
    {
        Idle,           // UI가 닫혀 있음
        ChestDropping,  // 상자가 떨어지는 중 (클릭 무시)
        WaitingOpen,    // 클릭하면 상자를 열 수 있음
        Opening,        // 상자 열리는 연출 중
        Revealing,      // 결과 프레임을 하나씩 보여주는 중
        Finished        // 연출 끝, 클릭하면 닫힘
    }


    [Serializable]
    private class ChestOpenAnimSetting
    {
        public float shakeDuration = 0.5f;
        public float shakeStrength = 15f;
        public int shakeVibrato = 30;
        public Vector3 punchScale1 = new Vector3(0.8f, 1.2f, 1f);
        public Vector3 punchScale2 = new Vector3(1.2f, 0.8f, 1f);
        public float punchDuration = 0.15f;
        public float spriteChangeTime = 0.2f;
        public float restoreDuration = 0.15f;
    }

    private const int InitialPoolSize = 10;
    private static readonly int DropChestHash = Animator.StringToHash("DropChest");

    [Header("Sprite")]
    [SerializeField] private Sprite openedChestSprite;
    [SerializeField] private Sprite closedChestSprite;

    [Header("Image")]
    [SerializeField] private Image chestImage;

    [Header("Transform")]
    [SerializeField] private Transform gachaGridTr;
    [SerializeField] private Transform gachaFrameOriginTr;

    [Header("Anim")]
    [SerializeField] private float gachaMoveDuration;
    [SerializeField] private int gachaInterval = 200;
    [SerializeField] private int legendEffectTime = 2000;

    [Header("Chest Open Anim")]
    [SerializeField] private ChestOpenAnimSetting normalOpenSetting = new ChestOpenAnimSetting();
    [SerializeField] private ChestOpenAnimSetting legendOpenSetting = new ChestOpenAnimSetting
    {
        shakeDuration = 1.2f,
        shakeStrength = 35f,
        shakeVibrato = 40,
        punchScale1 = new Vector3(0.7f, 1.4f, 1f),
        punchScale2 = new Vector3(1.4f, 0.7f, 1f),
        punchDuration = 0.2f,
        spriteChangeTime = 0.25f,
    };

    [Header("Game Object")]
    [SerializeField] private float lightEffectRotationSpeed = 60f;
    [SerializeField] private GameObject chestLightEffectObject;

    private List<GachaFrame> gachaFrameList = new List<GachaFrame>();
    private GameObject gachaFramePrefab;
    private MemoryPool gachaFramePool;

    private Animator animator;
    private EGachaState state = EGachaState.Idle;
    
    private Vector3 chestOriginLocalPos;
    private Vector3 chestOriginScale;

    // 가챠UI에서 넘어온 가챠 결과 데이터 입니다.
    private List<GachaResultDatas> gachaResultDatas;
    private bool isLegend;

    // 연출 시퀀스 취소용 (CloseUI / Destroy 시 취소)
    private CancellationTokenSource sequenceCts;

    
    public override void Initialize(Transform anchor)
    {
        base.Initialize(anchor);

        animator = GetComponent<Animator>();
        chestLightEffectObject.SetActive(false);

        chestOriginLocalPos = chestImage.transform.localPosition;
        chestOriginScale = chestImage.transform.localScale;

        if (!GlobalManagers.Resource.TryGetResource<GameObject>(GlobalDefines.ResourceKey.UI.GACHA_FRAME_PREFAB,
                out GameObject prefab))
        {
            Logger.LogError($"{GlobalDefines.ResourceKey.UI.GACHA_FRAME_PREFAB}에 해당하는 키값이 없습니다.");
            return;
        }

        gachaFramePrefab = prefab;
        gachaFramePool = new MemoryPool(gachaFramePrefab);
    }

    public override void SetInfo(BaseUIData data)
    {
        base.SetInfo(data);

        GachaEffectUIData gachaEffectUIData = data as GachaEffectUIData;
        if (gachaEffectUIData == null) return;

        gachaResultDatas = gachaEffectUIData.gachaResults;
        isLegend = gachaResultDatas.Any(result => result.ResultRarity == ERarity.Legend);
        
    }

    public override void OpenUI()
    {
        base.OpenUI();

        state = EGachaState.ChestDropping;
        animator.SetTrigger(DropChestHash);
    }

    public override void CloseUI()
    {
        CancelSequence();
        state = EGachaState.Idle;

        ResetChest();
        ResetLightEffect();
        ResetGachaFrames();

        base.CloseUI();
    }

    
    private void OnDestroy()
    {
        gachaFramePool.DestroyObjects();
        CancelSequence();
    }
    

    

    // 상자 드롭 애니메이션 끝에서 호출되는 애니메이션 이벤트
    public void ChestDropAnimFinished()
    {
        state = EGachaState.WaitingOpen;
        SetGachaFrames();
        PlayLightEffect();
    }

    

    public void OnPointerClick(PointerEventData eventData)
    {
        HandleClickAsync().Forget();
    }

    private async UniTask HandleClickAsync()
    {
        switch (state)
        {
            case EGachaState.WaitingOpen:
                await PlayGachaSequenceAsync();
                break;

            case EGachaState.Finished:
                CloseUI();
                break;
        }
    }
    

    

    /// <summary>
    /// 상자 열기 → 결과 공개까지 전체 연출 흐름입니다.
    /// CloseUI / 오브젝트 파괴 시 토큰이 취소되어 중간에 안전하게 끊깁니다.
    /// (Forget()은 OperationCanceledException을 무시합니다)
    /// </summary>
    private async UniTask PlayGachaSequenceAsync()
    {
        CancellationToken ct = BeginSequence();

        state = EGachaState.Opening;
        await PlayChestOpenAnimationAsync(ct);

        state = EGachaState.Revealing;
        await OpenGachaResultAsync(ct);

        GlobalManagers.Event.NotifyEvent<OnGachaResultAnimFinishedEventData>(new OnGachaResultAnimFinishedEventData());
        state = EGachaState.Finished;
    }

    private CancellationToken BeginSequence()
    {
        CancelSequence();
        sequenceCts = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
        return sequenceCts.Token;
    }

    private void CancelSequence()
    {
        if (sequenceCts == null) return;

        sequenceCts.Cancel();
        sequenceCts.Dispose();
        sequenceCts = null;
    }

    /// <summary>
    /// 상자를 열때 연출을 플레이 합니다.
    /// </summary>
    private async UniTask PlayChestOpenAnimationAsync(CancellationToken ct)
    {
        ChestOpenAnimSetting setting = isLegend ? legendOpenSetting : normalOpenSetting;
        Transform chestTr = chestImage.transform;
        
        await chestTr
            .DOShakePosition(setting.shakeDuration, setting.shakeStrength, setting.shakeVibrato)
            .AwaitTween(ct);
        
        Sequence openSequence = DOTween.Sequence();
        openSequence
            .Append(chestTr.DOScale(setting.punchScale1, setting.punchDuration))
            .Append(chestTr.DOScale(setting.punchScale2, setting.punchDuration))
            .InsertCallback(setting.spriteChangeTime, () =>
            {
                if (openedChestSprite != null)
                    chestImage.sprite = openedChestSprite;
            })
            .Append(chestTr.DOScale(chestOriginScale, setting.restoreDuration));

        await openSequence.AwaitTween(ct);
    }

    
    /// <summary>
    /// 가챠의 결과를 하나하나 연출 합니다.
    /// </summary>
    private async UniTask OpenGachaResultAsync(CancellationToken ct)
    {
        for (int i = 0; i < gachaFrameList.Count; i++)
        {
            await RevealFrameAsync(gachaFrameList[i], ct);
        }
    }

    
    private async UniTask RevealFrameAsync(GachaFrame frame, CancellationToken ct)
    {
        frame.OpenUI();
        
        if(frame.UnitRarity != ERarity.Legend)
            GlobalManagers.Audio.PlaySfx(GlobalDefines.ResourceKey.Audio.GACHA_UNIT_SFX);

        await frame.transform.DOMove(gachaGridTr.position, gachaMoveDuration).
            AwaitTween(ct);

        if (frame.UnitRarity == ERarity.Legend)
        {
            frame.ShowLightEffect();
            GlobalManagers.Audio.PlaySfx(GlobalDefines.ResourceKey.Audio.LEGEND_UNIT_SFX);
            await UniTask.Delay(legendEffectTime, cancellationToken: ct);
        }

        frame.transform.SetParent(gachaGridTr);

        await UniTask.Delay(gachaInterval, cancellationToken: ct);
    }


    /// <summary>
    /// 가챠 결과에 따른 가챠 프레임들의 정보를 설정합니다.
    /// </summary>
    private void SetGachaFrames()
    {
        gachaFrameList.Clear();
        
        for (int i = 0; i < gachaResultDatas.Count; i++)
        {
            GachaResultDatas data = gachaResultDatas[i];
            GameObject go = gachaFramePool.GetObject();
            GachaFrame frame = go.GetComponent<GachaFrame>();
            
            frame.transform.SetParent(transform);
            frame.transform.position = gachaFrameOriginTr.position;
            frame.Initialize(data.ResultImage, data.ResultName, data.ResultRarity);
            frame.CloseUI();
            
            gachaFrameList.Add(frame);
        }
    }

    /// <summary>
    /// 가챠 프레임들의 위치를 원래 위치로 돌립니다.
    /// </summary>
    private void ResetGachaFrames()
    {
        foreach (GameObject go in gachaFramePool.GetPoolObjects())
        {
            GachaFrame frame = go.GetComponent<GachaFrame>();
            frame.transform.DOKill();
            frame.transform.SetParent(transform);
            frame.transform.position = gachaFrameOriginTr.position;
            frame.CloseUI();
        }
    }
    

    

    private void ResetChest()
    {
        Transform chestTr = chestImage.transform;
        chestTr.DOKill();
        chestTr.localPosition = chestOriginLocalPos;
        chestTr.localScale = chestOriginScale;
        chestImage.sprite = closedChestSprite;
    }

    /// <summary>
    /// 상자뒤의 빛 파티클 이펙트 애니메이션을 연출합니다.
    /// </summary>
    private void PlayLightEffect()
    {
        Transform lightTr = chestLightEffectObject.transform;

        chestLightEffectObject.SetActive(true);
        lightTr.localScale = Vector3.zero;

        lightTr.DOScale(1f, 0.6f)
            .SetEase(Ease.OutBack)
            .OnComplete(() =>
            {
                float rotateDuration = 360f / Mathf.Max(lightEffectRotationSpeed, 0.01f);

                lightTr.DORotate(new Vector3(0, 0, 360), rotateDuration, RotateMode.FastBeyond360)
                    .SetEase(Ease.Linear)
                    .SetLoops(-1, LoopType.Restart);
            });
    }

    private void ResetLightEffect()
    {
        chestLightEffectObject.transform.DOKill();
        chestLightEffectObject.SetActive(false);
    }

}