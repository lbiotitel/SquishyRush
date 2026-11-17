using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


//이름을 설정하는 팝업창
public class NickNamePopUP : BaseUI
{

    [SerializeField] private TMP_InputField inputField;
    
    [Header("Button")]
    [SerializeField] private AnimButton confirmButton;
    [SerializeField] private AnimButton closeButton;
     
    private ProfileService profileService;


    private ToastMessageUIData notValidateNickNameData;
    private ToastMessageUIData nickNameChangedSucceedData;


    public override void Initialize(Transform anchor)
    {
        base.Initialize(anchor);
        profileService = GlobalManagers.Service.GetService<ProfileService>();
        BindEvent();
        
        notValidateNickNameData = new ToastMessageUIData 
        { 
            message = LocaliztionKeyCode.UI_NICKNAME_IS_NOT_VALIDATE_TEXT
        };

        nickNameChangedSucceedData = new ToastMessageUIData
        {
            message = LocaliztionKeyCode.UI_NICKNAME_CHANGE_SUCCESSED_TEXT
        };
    }

    
    private void BindEvent()
    {
        confirmButton.OnClicked += OnClickedConfirmBtn;
        closeButton.OnClicked += CloseUI;
    }
    

    private void OnDestroy()
    {
        confirmButton.OnClicked -= OnClickedConfirmBtn;
        closeButton.OnClicked -= CloseUI;
    }


    private void OnClickedConfirmBtn()
        => SetNickName().Forget();


    /// <summary>
    /// 확인 버튼 클릭 시 서비스에 닉네임을 바꿔달라고 요청합니다.
    /// </summary>
    private async UniTask SetNickName()
    { 
        string nickName = inputField.text;
        bool ok =  await Utils.WaitForAwaitMethodWithOverlayUI(profileService.SetName(nickName));
        if (ok)
        {
            GlobalManagers.UI.PopUpContainer.OpenUI<ToastMessageUI>(nickNameChangedSucceedData);
            CloseUI();
        }
        else 
        {
            GlobalManagers.UI.PopUpContainer.OpenUI<ToastMessageUI>(notValidateNickNameData);
        }
    }
}
