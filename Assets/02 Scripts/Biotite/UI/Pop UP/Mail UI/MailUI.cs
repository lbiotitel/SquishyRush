using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;


//매일 UI에 대한 클래스 입니다.
public class MailUI : BaseUI
{
    [Header("Button")]
    [SerializeField] private AnimButton closeButton;
    [SerializeField] private AnimButton claimAllButton;
    
    [Header("Transform")]
    [SerializeField] private Transform contentTr;

    [Header("Property")] 
    [SerializeField] private int cellCount;

    [Header("Object")] 
    [SerializeField] private GameObject mailEmptyObject;
    
    private List<MailCellView> mailCellViews;
    private GameObject mailCellViewPrefab;

    //service
    private MailService mailService;


    public override void Initialize(Transform anchor)
    {
        base.Initialize(anchor);

        if (!GlobalManagers.Resource.TryGetResource<GameObject>(GlobalDefines.ResourceKey.UI.MAIL_CELL_VIEW_PREFAB,
                out GameObject prefab))
        {
            Logger.LogError($"{GlobalDefines.ResourceKey.UI.MAIL_CELL_VIEW_PREFAB}에 해당하는 프리팹이 없습니다.");
            return;
        }
        
        mailCellViewPrefab = prefab;
        mailService = GlobalManagers.Service.GetService<MailService>();
        
        CreateMailCellViews();
        BindEvents();
        
        DrawMailCellView();
    }


    private void BindEvents()
    {
        closeButton.OnClicked += CloseUI;
        claimAllButton.OnClicked += OnClickedClaimAllBtn;
        GlobalManagers.Event?.AddEvent<OnPostMailLoadedEventData>(OnPostMailLoadedEventDataHandler);
    }

    private void OnDestroy()
    {
        closeButton.OnClicked -= CloseUI;
        claimAllButton.OnClicked -= ClaimAllMail;
        GlobalManagers.Event?.RemoveEvent<OnPostMailLoadedEventData>(OnPostMailLoadedEventDataHandler);
    }

    
    /// <summary>
    /// 일정 시간 마다 매일을 다시 그립니다.
    /// </summary>
    /// <param name="e"></param>
    private void OnPostMailLoadedEventDataHandler(OnPostMailLoadedEventData e)
        => DrawMailCellView();

    
    
    /// <summary>
    /// cell view를 미리 만들어 풀링합니다.
    /// </summary>
    private void CreateMailCellViews()
    {
        mailCellViews = new List<MailCellView>();

        for (int i = 0; i < cellCount; i++)
        {
            GameObject go = Instantiate(mailCellViewPrefab, contentTr);
            MailCellView mailCellView = go.GetComponent<MailCellView>();
            mailCellView.OnClickedClaimBtn += OnClickedClaimBtn;
            mailCellViews.Add(mailCellView);
            mailCellView.CloseUI();
        }
    }
    
    
    /// <summary>
    /// 매일 리스트를 가지고와서 cell view에 출력합니다.
    /// </summary>
    private void DrawMailCellView()
    {
        List<MailData> mailDatas = mailService.GetMails(GlobalDefines.GameConstants.MAIL_REQUEST_COUNT);
        mailEmptyObject.SetActive(mailDatas.Count <= 0);

        if (mailDatas.Count == 0)
        {
            Logger.Log("매일이 없습니다");
            return;
        }
        
        for (int i = 0; i < mailDatas.Count; i++)
        {
            if (i < mailCellViews.Count)
            {
                MailCellView mailCellView = mailCellViews[i];
                mailCellView.OpenUI(mailDatas[i]);
            }
            else
            {
                MailCellView mailCellView = mailCellViews[i];
                mailCellView.CloseUI();
            }
        }
    }

    
    
    /// <summary>
    /// 받기 버튼을 눌렸을 때 매일 수령 메서드를 호출합니다.
    /// </summary>
    /// <param name="mailCellView"></param>
    private void OnClickedClaimBtn(MailCellView mailCellView)
        => ClaimMail(mailCellView).Forget();
    
    /// <summary>
    /// 모두 받기 버튼을 눌렸을 때 모든 매일 수령 메서드를 호출합니다.
    /// </summary>
    private void OnClickedClaimAllBtn()
        => ClaimAllMail();

    
    /// <summary>
    /// 매일을 수령합니다.
    /// </summary>
    /// <param name="mailCellView"></param>
    private async UniTask ClaimMail(MailCellView mailCellView)
    {
        bool res = await Utils.WaitForAwaitMethodWithOverlayUI(mailService.ReceiveMailAsync(mailCellView.InDate));
        if (res)
        {
            Logger.Log("매일을 수령했습니다");
            mailCellView.CloseUI();
        }
        else
        {
            Logger.Log("매일을 수령하지 못했습니다");
        }
    }

    
    /// <summary>
    /// 모든 매일을 수령합니다.
    /// </summary>
    private void ClaimAllMail()
    {
        for (int i = 0; i < mailCellViews.Count; i++)
        {
            if (!string.IsNullOrEmpty(mailCellViews[i].InDate))
            {
                ClaimMail(mailCellViews[i]).Forget();
            }
        }
    }
}
