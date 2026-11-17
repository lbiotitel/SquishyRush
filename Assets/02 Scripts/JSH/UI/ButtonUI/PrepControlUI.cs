using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 정비 단계 동안만 보이는 진행 조작 버튼 묶음입니다 (전투 시작, 재능상점 라운드의 적/상품 전환).
/// 유닛 구매 UI(ShopPanel)와 화면 위치·등장 방식·역할이 달라서 분리했습니다.
/// 버튼은 요청만 하고 실제 처리는 IPrepActions(InGameController)가 합니다.
/// </summary>
public class PrepControlUI : MonoBehaviour
{
    [Header("전투 시작")] 
    [SerializeField] private TMP_Text battleTitleText;
    [SerializeField] private AnimButton startBtn;

    [Header("재능상점 라운드 - 적/상품 전환")] 
    [SerializeField] private AnimButton previewToggleBtn;
    [SerializeField] private Image previewToggleIcon;
    [SerializeField] private Sprite showEnemySprite;
    [SerializeField] private Sprite showPerkSprite;

    private IPrepActions actions;

    public void Init(IPrepActions actions)
    {
        this.actions = actions;

        battleTitleText.text =
            Localization.GetLocaliationText(Localization.LocalizationTable.UI, LocaliztionKeyCode.UI_MENU_BATTLE_BUTTON);
        
        startBtn.OnClicked -= OnClickStart;
        startBtn.OnClicked += OnClickStart;

        previewToggleBtn.OnClicked -= OnClickPreviewToggle;
        previewToggleBtn.OnClicked += OnClickPreviewToggle;
        
        SetPerkShopMode(false);
        Hide();
    }

    public void Show() => gameObject.SetActive(true);
    public void Hide() => gameObject.SetActive(false);

    /// <summary>
    /// 재능상점 라운드 진입/종료 시 컨트롤러가 호출합니다.
    /// 진입하면 상품부터 보이므로 아이콘도 "적 보기" 상태로 시작합니다.
    /// </summary>
    public void SetPerkShopMode(bool isPerkShopRound)
    {
        previewToggleBtn.gameObject.SetActive(isPerkShopRound);
        
        if(isPerkShopRound) RefreshPreviewToggle(true);
    }

    private void OnClickStart()
    {
        GlobalManagers.Audio.PlaySfx(GlobalDefines.ResourceKey.Audio.BUTTON_CLICK_SFX);
        actions.StartBattle();
    }

    private void OnClickPreviewToggle()
    {
        GlobalManagers.Audio.PlaySfx(GlobalDefines.ResourceKey.Audio.BUTTON_CLICK_SFX);
        RefreshPreviewToggle(actions.TogglePerkPreview());
    }

    private void RefreshPreviewToggle(bool showingPerk)
    {
        if (previewToggleIcon == null) return;
        previewToggleIcon.sprite = showingPerk ? showEnemySprite : showPerkSprite;
    }

    private void OnDestroy()
    {
        if (startBtn != null) startBtn.OnClicked -= OnClickStart;
        if(previewToggleBtn != null) previewToggleBtn.OnClicked -= OnClickPreviewToggle;
    }
}
