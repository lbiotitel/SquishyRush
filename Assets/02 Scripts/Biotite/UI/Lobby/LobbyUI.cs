using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;



//로비에서 쓰는 공통 UI를 관리하는 클래스입니다.
public class LobbyUI : MonoBehaviour
{
    [SerializeField] private MenuButtonUI gachaButton;
    [SerializeField] private MenuButtonUI unitButton;
    [SerializeField] private MenuButtonUI battleButton;
    [SerializeField] private MenuButtonUI artifactButton;
    [SerializeField] private MenuButtonUI leaderBoardButton;
    private MenuButtonUI currentMenuButton;

    private ConfirmUIData exitConfirmData;

    private void Awake()
    {
        Initialize();
    }

    private void Start()
    {
        exitConfirmData = new ConfirmUIData()
        {
            ConfirmType = EConfirmType.OK_CANCLE,
            Title = LocaliztionKeyCode.UI_ASK_EXIT_TITLE_TEXT,
            Desc = LocaliztionKeyCode.UI_ASK_EXIT_DESC_TEXT,
            OkText = LocaliztionKeyCode.UI_OK_TEXT,
            CancelText = LocaliztionKeyCode.UI_CANCEL_TEXT,
            OkAction = () => Application.Quit(0),
        };
    }

    private void OnEnable()
    {
        ClickedMenuButton(battleButton);
    }

    private void OnDisable()
    {
        ResetAllButton();
    }

    private void Update()
    {
        CheckBackButton();
    }

    
    
    
    /// <summary>
    /// 백 버튼을 누를시 가장위의 팝업 ui를 닫습니다.
    /// </summary>
    private void CheckBackButton()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (GlobalManagers.UI.PopUpContainer.GetFrontUI() != null)
            {
                GlobalManagers.UI.PopUpContainer.CloseFrontUI();
            }
            else
            {
                GlobalManagers.UI.PopUpContainer.OpenUI<ConfirmUI>(exitConfirmData);
            }
        }
    }


    private void Initialize()
    {
        InitMenuButton(gachaButton, "ui_menu_gacha_button");
        InitMenuButton(unitButton, "ui_menu_equipment_button");
        InitMenuButton(battleButton, "ui_menu_battle_button");
        InitMenuButton(artifactButton, "ui_menu_equipment_button");
        InitMenuButton(leaderBoardButton, "ui_menu_leader_board_button");
    }

    private void InitMenuButton(MenuButtonUI button, string nameKey)
    {
        button.Initialize(Localization.GetLocaliationText(Localization.LocalizationTable.UI, nameKey));
        button.OnClicked += ClickedMenuButton;
    }


    private void OnDestroy()
    {
        gachaButton.OnClicked -= ClickedMenuButton;
        unitButton.OnClicked -= ClickedMenuButton;
        battleButton.OnClicked -= ClickedMenuButton;
        artifactButton.OnClicked -= ClickedMenuButton;
        leaderBoardButton.OnClicked -= ClickedMenuButton;
    }


    private void ClickedMenuButton(MenuButtonUI clickedButton)
    {
        if (currentMenuButton == clickedButton) 
            return;
        
        GlobalManagers.Audio.PlaySfx(GlobalDefines.ResourceKey.Audio.BUTTON_CLICK_SFX);
        ResetAllButton();
        clickedButton.ClickedAnimation();

        currentMenuButton = clickedButton;
        
        if (clickedButton == unitButton)
            GlobalManagers.UI.SheetContainer.OpenUI<UnitListUI>();
        if (clickedButton == battleButton)
            GlobalManagers.UI.SheetContainer.OpenUI<BattleUI>();
        if (clickedButton == artifactButton)
            GlobalManagers.UI.SheetContainer.OpenUI<ArtifactListUI>();
        if (clickedButton == gachaButton)
            GlobalManagers.UI.SheetContainer.OpenUI<GachaUI>();
        if (clickedButton == leaderBoardButton)
            GlobalManagers.UI.SheetContainer.OpenUI<LeaderBoardUI>();
    }

    private void ResetAllButton()
    {
        gachaButton.ResetAnimation();
        unitButton.ResetAnimation();
        battleButton.ResetAnimation();
        artifactButton.ResetAnimation();
        leaderBoardButton.ResetAnimation();
        currentMenuButton = null;
    }


}
