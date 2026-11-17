using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


//매일의 cell view를 정의 합니다.
public class MailCellView : MonoBehaviour
{
    [Header("Text")] 
    [SerializeField] private TMP_Text mailTitleText;
    [SerializeField] private TMP_Text mailContentText;
    [SerializeField] private TMP_Text mailExpirationDate;
    [SerializeField] private TMP_Text mailItemAmount;
    
    [Header("Image")]
    [SerializeField] private Image mailItemImage;
 
    [Header("Button")] 
    [SerializeField] private AnimButton claimButton;

    [Header("Transform")]
    [SerializeField] private Transform mailRewardContentTr;

    [Header("Property")]
    [SerializeField] private int mailRewardItemPoolingCount;


    private List<MailRewardItemCellView> mailRewardItemCellViews;
    private GameObject mailRewardItemCellViewPrefab;

    public string InDate { get; private set; }
    public Action<MailCellView> OnClickedClaimBtn;



    private void Awake()
    {
        if (!GlobalManagers.Resource.TryGetResource<GameObject>(
                GlobalDefines.ResourceKey.UI.MAIL_REWARD_ITEM_CELL_VIEW_PREFAB, out GameObject prefab))
        {

            Logger.LogError($"{GlobalDefines.ResourceKey.UI.MAIL_REWARD_ITEM_CELL_VIEW_PREFAB}에 해당하는 프리팹이 없습니다.");
            return;
        }

        mailRewardItemCellViewPrefab = prefab;
        
        CreateMailRewardItemCellView();
        BindEvents();
    }

    private void BindEvents()
    {
        claimButton.OnClicked += Claim;
    }



    /// <summary>
    /// 매일 아이템 cell view를 미리 만들어 풀링 합니다.
    /// </summary>
    private void CreateMailRewardItemCellView()
    {
        mailRewardItemCellViews = new List<MailRewardItemCellView>();

        for (int i = 0; i < mailRewardItemPoolingCount; i++)
        {
            GameObject go = Instantiate(mailRewardItemCellViewPrefab ,mailRewardContentTr);
            MailRewardItemCellView mailRewardCellView = go.GetComponent<MailRewardItemCellView>();
            mailRewardItemCellViews.Add(mailRewardCellView);
        }
    }


    
    /// <summary>
    /// 외부에 버튼을 눌렸다는 것을 알려 부모에서 처리 합니다.
    /// </summary>
    private void Claim()
        => OnClickedClaimBtn?.Invoke(this);
    

    public void OpenUI(MailData mailData)
    {
        gameObject.SetActive(true);
        InDate = mailData.inDate; 
        DrawMailInfo(mailData);
        DrawMailRewardItem(mailData);
    }
    
    
    /// <summary>
    /// 매일의 기본 정보를 그립니다.
    /// </summary>
    /// <param name="mailData"></param>
    private void DrawMailInfo(MailData mailData)
    {
        mailTitleText.text = mailData.title;
        mailContentText.text = mailData.content;
        mailExpirationDate.text = mailData.expirationDate.ToString("yyyy-MM-dd HH");
    }

    /// <summary>
    /// 메일에 들어있는 아이템 정보를 그립니다.
    /// </summary>
    /// <param name="mailData"></param>
    private void DrawMailRewardItem(MailData mailData)
    {
        //아이템
        List<MailCurrencyData> itemDatas = mailData.mailRewardItemDatas;

        foreach (var cell in mailRewardItemCellViews)
            cell.CloseUI();

        if (itemDatas != null && itemDatas.Count > 0)
        {
            for(int i = 0; i < itemDatas.Count; i++)
            {
                var itemData = itemDatas[i];
                MailRewardItemCellView cellView = mailRewardItemCellViews[i];
                cellView.OpenUI(itemData.itemID, itemData.amount);
            }
        }
    }


    public void CloseUI()
    {
        InDate = string.Empty;
        gameObject.SetActive(false);
    }
}
