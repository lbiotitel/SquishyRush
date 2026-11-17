using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;



//일일퀘스트 Ui 입니다.
public class DailyQuestUI : BaseUI
{
    [Header("Transform")]
    [SerializeField] private Transform contentTr;

    [Header("Button")] 
    [SerializeField] private AnimButton closeButton;

    private List<DailyQuestCellView>  dailyQuestCellViews;
    private GameObject dailyQuestCellViewPrefab;
    
    private IReadOnlyDictionary<string, DailyQuestData>  dailyQuestDatas; 
    
    private DailyQuestService dailyQuestService;
    

    public override void Initialize(Transform anchor)
    {
        base.Initialize(anchor);

        dailyQuestDatas = GlobalManagers.Chart.GetDailyQuestDatas();
        dailyQuestService = GlobalManagers.Service.GetService<DailyQuestService>();
        
        if(!GlobalManagers.Resource.TryGetResource<GameObject>(GlobalDefines.ResourceKey.UI.DAILY_QUEST_CELL_VIEW_PREFAB, out  GameObject prefab))
        {
            Logger.LogError($"{GlobalDefines.ResourceKey.UI.DAILY_QUEST_CELL_VIEW_PREFAB}에 해당하는 프리팹이 없습니다.");
            return;
        }
        
        dailyQuestCellViewPrefab = prefab;
        
        CreateDailyQuestCellView();
        BindEvent();
    }


    private void BindEvent()
    {
        closeButton.OnClicked += CloseUI;
    }


    private void OnDestroy()
    {
        closeButton.OnClicked -= CloseUI;
        
        foreach(var cellVeiw in dailyQuestCellViews)
            cellVeiw.OnClaimClicked -= OnClickedClaimButton;
    }


    /// <summary>
    /// dailyQuests 수 만큼 DailyQuestCellView를 미리 풀링해서 저장합니다.
    /// </summary>
    private void CreateDailyQuestCellView()
    {
        dailyQuestCellViews = new List<DailyQuestCellView>();
        
        foreach (var dailyQuest in dailyQuestDatas)
        {
            GameObject go = Instantiate(dailyQuestCellViewPrefab,  contentTr);
            DailyQuestCellView dailyQuestCellView = go.GetComponent<DailyQuestCellView>();
            dailyQuestCellView.Initialize(dailyQuest.Key ,dailyQuest.Value);
            dailyQuestCellView.OnClaimClicked += OnClickedClaimButton;
            dailyQuestCellViews.Add(dailyQuestCellView);
        }
    }

    
    /// <summary>
    /// 퀘스트 cellview를 다시그립니다.
    /// </summary>
    private void DrawDailyQuestCellView()
    {
        foreach (var cellView in dailyQuestCellViews)
        {
            cellView.DrawQuestCellView();
        }
    }


    public override void OpenUI()
    {
        DrawDailyQuestCellView();
        base.OpenUI();
    }


    /// <summary>
    /// 보상을 받고 데이터를 저장합니다.
    /// </summary>
    /// <param name="questID"></param>
    private void OnClickedClaimButton(string questID)
    {
        dailyQuestService.ClaimQuestReward(questID).Forget();
    }
}
