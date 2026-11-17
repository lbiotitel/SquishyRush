using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;



//랭킹에 관한 UI 정의 입니다.
public class LeaderBoardUI : BaseUI
{
    [Header("Top 3 Ranking Node")] 
    [SerializeField] private LeaderBoardTopUI ranking1st;
    [SerializeField] private LeaderBoardTopUI ranking2st;
    [SerializeField] private LeaderBoardTopUI ranking3st;

    [Header("My Ranking")] [SerializeField]
    private LeaderBoardNodeUI myRankingNodeUI;

    [SerializeField] private Transform scrollTransform;
    private LeaderBoardNodeUI rankingNodeUIPrefab;
    private List<LeaderBoardNodeUI> rankingNodeUIs;
    
    private RankingService rankingService;

    public override void Initialize(Transform anchor)
    {
        base.Initialize(anchor);

        if (!GlobalManagers.Resource.TryGetResource<GameObject>(GlobalDefines.ResourceKey.UI.LEADER_BOARD_NODE_UI_PREFAB,
                out GameObject prefab))
        {
            Logger.LogError($"{GlobalDefines.ResourceKey.UI.LEADER_BOARD_NODE_UI_PREFAB}에 해당하는 프리팹이 없습니다.");
            return;
        }

        rankingNodeUIPrefab = prefab.GetComponent<LeaderBoardNodeUI>();
        rankingService = GlobalManagers.Service.GetService<RankingService>();

        ranking1st.CloseUI();
        ranking2st.CloseUI();
        ranking3st.CloseUI();

        CreateRankingNode();
    }
    

    
    //미리 랭킹 노드들 풀링
    private void CreateRankingNode()
    {
        rankingNodeUIs = new List<LeaderBoardNodeUI>();
        for (int i = 0; i < GlobalDefines.GameConstants.RANK_COUNT; i++)
        {
            LeaderBoardNodeUI rankingNodeUI = Instantiate(rankingNodeUIPrefab, scrollTransform);
            rankingNodeUI.CloseUI();
            rankingNodeUIs.Add(rankingNodeUI);
        }
    }
    
    
    /// <summary>
    /// 랭킹 노드 ui를 전부 비활성화 시킵니다.
    /// </summary>
    private void DisableRankingNode()
    {
        for (int i = 0; i < GlobalDefines.GameConstants.RANK_COUNT; i++)
        {
            rankingNodeUIs[i].CloseUI();
        }
    }
    
    

    //자신의 랭킹 데이터를 그립니다.
    private void DrawMyRank()
    {
        UserRankData myRank = rankingService.MyRankData;
        myRankingNodeUI.OpenUI(myRank);
    }

    
    //유저 랭킹 데이터를 UI에 그립니다.
    private void DrawUserRank()
    {
        DisableRankingNode();
        ranking1st.CloseUI();
        ranking2st.CloseUI();
        ranking3st.CloseUI();

        var userRankList = rankingService.UserRankDatas;

        if (userRankList != null && userRankList.Count > 0)
        {
            if (userRankList.Count >= 1)
                ranking1st.OpenUI(userRankList[0]);
            if (userRankList.Count >= 2)
                ranking2st.OpenUI(userRankList[1]);
            if (userRankList.Count >= 3)
                ranking3st.OpenUI(userRankList[2]);

            for (int i = 3; i < userRankList.Count; i++)
            {
                rankingNodeUIs[i - 3].OpenUI(userRankList[i]);
            }
        }
    }


    public override void OpenUI()
    {
        base.OpenUI();
        DrawMyRank();
        DrawUserRank();
        GlobalManagers.Event.NotifyEvent<OnDoDailyQuestEventData>(new OnDoDailyQuestEventData()
        {
            QuestType = EQuestType.ShowRanking
        });
    }
}
