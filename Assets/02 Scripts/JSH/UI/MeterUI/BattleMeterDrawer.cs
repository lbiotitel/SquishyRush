using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BattleMeterDrawer : MonoBehaviour
{
    [Serializable]
    private class TabButton
    {
        public EMeterTab Tab;
        public AnimButton Button;
        public Image Background;
        public TMP_Text Label;
    }

    [Header("슬라이드")]
    [SerializeField] private RectTransform panelRect;
    [SerializeField] private float shownX = 0f;
    [Tooltip("닫혔을 때 X. 패널 폭만큼 밀어 손잡이만 화면 끝에 남게 합니다.")]
    [SerializeField] private float hiddenX = 240f;
    [SerializeField] private float slideDuration = 0.3f;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("손잡이")]
    [SerializeField] private AnimButton handleButton;
    [SerializeField] private RectTransform handleArrow;

    [Header("머리")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private GameObject damageLegend;
    [SerializeField] private GameObject supportLegend;

    [Header("줄")] 
    [SerializeField] private ScrollRect rowScroll;
    [SerializeField] private RectTransform rowRoot;
    [SerializeField] private BattleMeterRowView rowPrefab;

    [Header("탭 (맨 아래)")]
    [SerializeField] private TabButton[] tabs;
    [SerializeField] private Color tabOnColor = new(0.23f, 0.18f, 0.29f);
    [SerializeField] private Color tabOffColor = new(1f, 1f, 1f, 0f);
    [SerializeField] private Color tabOnTextColor = new(1f, 0.96f, 0.89f);
    [SerializeField] private Color tabOffTextColor = new(0.23f, 0.18f, 0.29f);
    
    [Header("막대 색")]
    [SerializeField] private Color attackColor = new(0.96f, 0.57f, 0.24f);   // 주황
    [SerializeField] private Color skillColor = new(0.31f, 0.56f, 0.94f);    // 파랑
    [SerializeField] private Color fixedColor = new(0.95f, 0.94f, 0.91f);    // 흰색
    [SerializeField] private Color healColor = new(0.31f, 0.76f, 0.43f);     // 초록
    [SerializeField] private Color shieldColor = new(0.54f, 0.85f, 0.96f);   // 하늘

    private readonly List<BattleMeterRowView> rowViews = new();

    private MeterSnapshot snapshot;
    private EMeterTab currentTab = EMeterTab.Dealt;
    private bool isOpen = true;
    private Tween slideTween;

    /// <summary>
    /// 버튼을 연결하고 숨긴 상태로 시작합니다. InGameController.InitRun에서 한 번 부릅니다.
    /// </summary>
    public void Init()
    {
        handleButton.OnClicked -= OnClickHandle;
        handleButton.OnClicked += OnClickHandle;

        for (int i = 0; i < tabs.Length; i++)
        {
            TabButton tab = tabs[i];
            tab.Button.OnClicked += () => SelectTab(tab.Tab);
        }
        
        SetOpen(isOpen, instant: true);
        gameObject.SetActive(false);
    }

    /// <summary>
    /// 상점 페이즈에 들어올 때 지난 전투 결과표를 보여줍니다.
    /// 결과가 없으면(첫 라운드) 서랍 자체를 숨깁니다.
    /// 탭과 열림 상태는 지난 라운드에 사용자가 둔 그대로 이어갑니다.
    /// </summary>
    public void Show(MeterSnapshot result)
    {
        snapshot = result;
        if (snapshot == null)
        {
            Hide();
            return;
        }
        
        gameObject.SetActive(true);
        SetDragHidden(false);
        
        // TODO: 로컬라이즈 키로 교체 ("{0}라운드 결과")
        titleText.text = $"{snapshot.DisplayRound}라운드 결과";
        
        SetOpen(isOpen, instant: true);
        Render();
    }

    /// <summary>상점 페이즈가 끝날 때 숨깁니다. 결과표는 다음 Show에서 새 것으로 바뀝니다.</summary>
    public void Hide()
    {
        slideTween?.Kill();
        gameObject.SetActive(false);
    }

    /// <summary>
    /// 유닛 드래그 중에는 서랍이 보드 오른쪽을 가리지 않도록 투명하게 하고 입력을 막습니다.
    /// </summary>
    public void SetDragHidden(bool hidden)
    {
        canvasGroup.alpha = hidden ? 0f : 1f;
        canvasGroup.blocksRaycasts = !hidden;
    }
    
    #region 입력

    private void OnClickHandle()
    {
        GlobalManagers.Audio.PlaySfx(GlobalDefines.ResourceKey.Audio.BUTTON_CLICK_SFX);
        SetOpen(!isOpen, instant: false);
    }

    /// <summary>탭을 바꾸고 다시 그립니다. 같은 탭을 눌러도 문제는 없습니다.</summary>
    private void SelectTab(EMeterTab tab)
    {
        GlobalManagers.Audio.PlaySfx(GlobalDefines.ResourceKey.Audio.BUTTON_CLICK_SFX);
        currentTab = tab;
        Render();
        ScrollToTop();
    }

    /// <summary>
    /// 줄 목록을 맨 위(1등)로 되돌립니다. 새 결과표를 열 때와 탭을 바꿀 때 부릅니다.
    /// verticalNormalizedPosition은 레이아웃이 다시 계산되기 전이면 이전 높이로 계산되어 튈 수 있어서,
    /// 위쪽에 붙은 RowRoot의 위치를 0으로 직접 놓고 관성도 멈춥니다.
    /// </summary>
    private void ScrollToTop()
    {
        if(rowScroll != null) rowScroll.StopMovement();
        rowRoot.anchoredPosition = new Vector2(rowRoot.anchoredPosition.x, 0f);
    }
    
    #endregion

    #region 그리기

    /// <summary>
    /// 현재 탭 기준으로 줄을 정렬해 그립니다.
    /// 정렬·Max 계산은 MeterSnapshot이 하고, 여기서는 받은 순서대로 줄 뷰에 넘기기만 합니다.
    /// 줄 뷰는 모자랄 때만 만들고, 남는 것은 끕니다(탭마다 시너지 행 유무로 줄 수가 다름).
    /// </summary>
    private void Render()
    {
        if (snapshot == null) return;

        IReadOnlyList<MeterRow> sorted = snapshot.GetSorted(currentTab, out float max);
        MeterBarColors colors = GetColors(currentTab);
        
        while(rowViews.Count < sorted.Count)
            rowViews.Add(Instantiate(rowPrefab, rowRoot));

        for (int i = 0; i < rowViews.Count; i++)
        {
            if (i < sorted.Count) rowViews[i].Bind(sorted[i], currentTab, max, colors);
            else rowViews[i].Hide();
        }

        bool support = currentTab == EMeterTab.Support;
        damageLegend.SetActive(!support);
        supportLegend.SetActive(support);
        
        RefreshTabs();
    }

    /// <summary>
    /// 탭별 막대 색. 뒤(back)부터 앞(front) 순서입니다.
    /// 지원 탭은 두 장만 쓰므로 front는 fillAmount가 0이라 색은 상관없습니다.
    /// </summary>
    private MeterBarColors GetColors(EMeterTab tab) => tab == EMeterTab.Support
        ? new MeterBarColors(shieldColor, healColor, healColor)
        : new MeterBarColors(fixedColor, skillColor, attackColor);

    /// <summary>선택된 탭만 진하게 칠합니다.</summary>
    private void RefreshTabs()
    {
        for (int i = 0; i < tabs.Length; i++)
        {
            bool on = tabs[i].Tab == currentTab;
            tabs[i].Background.color = on ? tabOnColor : tabOffColor;
            tabs[i].Label.color = on ? tabOnTextColor : tabOffTextColor;
        }
    }
    
    #endregion
    
    #region 슬라이드

    /// <summary>
    /// 서랍을 여닫습니다. 손잡이는 패널 자식이라 같이 움직이고, 닫혀도 화면 끝에 남습니다.
    /// 화살표는 열림 = '>'(닫기), 닫힘 = (열기)가 되도록 180도 돌립니다.
    /// instant는 Show 직후처럼 연출 없이 바로 맞출 때 씁니다.
    /// </summary>
    private void SetOpen(bool open, bool instant)
    {
        isOpen = open;
        slideTween?.Kill();

        float x = open ? shownX : hiddenX;
        handleArrow.localEulerAngles = new Vector3(0f, 0f, open ? 0f : 180f);

        if (instant)
        {
            Vector2 pos = panelRect.anchoredPosition;
            pos.x = x;
            panelRect.anchoredPosition = pos;
            return;
        }

        slideTween = panelRect.DOAnchorPosX(x, slideDuration)
            .SetEase(open ? Ease.OutCubic : Ease.InCubic)
            .SetLink(gameObject);
    }

    #endregion

    private void OnDestroy()
    {
        slideTween?.Kill();
        if(handleButton != null) handleButton.OnClicked -= OnClickHandle;
    }
}
