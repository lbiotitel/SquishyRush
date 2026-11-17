using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;


//아티팩트 관리 창입니다.
public class ArtifactListUI : BaseUI
{
    [Header("ScrollRect")]
    [SerializeField] private ScrollRect scrollRect;
    
    [Header("Transform")]
    [SerializeField] private Transform ownedArtifactTr;
    [SerializeField] private Transform notOwnedArtifactTr;

    private RectTransform rectTransform;
    private RectTransform ownedArtifactRect;
    private RectTransform  notOwnedArtifactRect;
    
    private GameObject artifactCellViewPrefab;

    private List<ArtifactData> artifactDatas;
    private List<ArtifactListCellView> artifactCellViews;

    private ArtifactService artifactService;
    

    public override void Initialize(Transform anchor)
    {
        base.Initialize(anchor);

        GlobalManagers.Event.AddEvent<OnArtifactUpgradeEventData>(HandleArtifactUpgradeEvent);
        
        rectTransform = GetComponent<RectTransform>();
        ownedArtifactRect = ownedArtifactTr.GetComponent<RectTransform>();
        notOwnedArtifactRect = notOwnedArtifactTr.GetComponent<RectTransform>();

        if (!GlobalManagers.Resource.TryGetResource<GameObject>(GlobalDefines.ResourceKey.UI.ARTIFACT_CELL_VIEW_PREFAB,
                out GameObject prefab))
        {
            Logger.LogError($"{GlobalDefines.ResourceKey.UI.ARTIFACT_CELL_VIEW_PREFAB}에 해당하는 프리팹이 없습니다.");
            return;
        }

        artifactCellViewPrefab = prefab;
        artifactService = GlobalManagers.Service.GetService<ArtifactService>();
        
        BindData();
        CreateArtifactCellViews();
    }


    private void OnDestroy()
    {
        GlobalManagers.Event?.RemoveEvent<OnArtifactUpgradeEventData>(HandleArtifactUpgradeEvent);
    }


    /// <summary>
    /// 아티팩트 데이터를 정렬후 캐싱합니다.
    /// </summary>
    private void BindData()
    {
        var data = GlobalManagers.Resource.GetResources<ArtifactData>();
        if (data == null)
        {
            Logger.LogError("아티팩트 데이터를 가져오지 못했습니다.");
            return;
        }

        var sortedData = data
            .OrderBy(d => d.ArtifactRarity)
            .ThenBy(d => d.ArtifactID)
            .ToList();

        artifactDatas = sortedData;
    }


    /// <summary>
    /// 아티팩트 레벨업이 성공할시 레벨업 한 아티팩트만 다시 그립니다.
    /// </summary>
    /// <param name="e"></param>
    private void HandleArtifactUpgradeEvent(OnArtifactUpgradeEventData e)
    {
        foreach (var cellView in artifactCellViews)
        {
            if (cellView.ArtifactID == e.ArtifactID)
            {
                cellView.DrawArtifactData();
                return;
            }
        }
    }


    public override void OpenUI()
    {
        base.OpenUI();
        DrawArtifactNodeUIs();
    }


    /// <summary>
    /// 아티팩트 cellview들을 생성해서 풀링해 둡니다.
    /// </summary>
    private void CreateArtifactCellViews()
    {
        artifactCellViews = new List<ArtifactListCellView>();
        int count = artifactDatas.Count;

        for (int i = 0; i < count; i++)
        {
            GameObject go = Instantiate(artifactCellViewPrefab, ownedArtifactTr);
            ArtifactListCellView artifactListCellView = go.GetComponent<ArtifactListCellView>();
            ArtifactObject artifactObject = new ArtifactObject();
            artifactObject.Initialize(artifactDatas[i]);
            artifactListCellView.Initailize(artifactObject);
            artifactCellViews.Add(artifactListCellView);
        }
    }

    

    /// <summary>
    /// 아티팩트 노드 UI들에게 정보를 표기하도록 합니다
    /// </summary>
    private void DrawArtifactNodeUIs()
    {
        foreach (var node in artifactCellViews)
        {
            bool isOwned = artifactService.IsOwned(node.ArtifactID);
            node.transform.SetParent(isOwned == true ? ownedArtifactTr : notOwnedArtifactTr);
            node.DrawArtifactData();
        }
        
        LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
        LayoutRebuilder.ForceRebuildLayoutImmediate(ownedArtifactRect);
        LayoutRebuilder.ForceRebuildLayoutImmediate(notOwnedArtifactRect);

        scrollRect.verticalNormalizedPosition = 1f;
    }
}
