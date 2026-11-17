using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StageListUI : MonoBehaviour
{
    [Header("Text")] 
    [SerializeField] private TMP_Text stageName;
    
    [Header("Button")]
    [SerializeField] private AnimButton stageNextBtn;
    [SerializeField] private AnimButton stagePrevBtn;
    
    
    [Header("Image")]
    [SerializeField] private Image mainBG;

    [Header("Transform")]
    [SerializeField] private Transform stagePos;

    [Header("Object")] 
    [SerializeField] private GameObject lockObject; 

    private List<LobbyStageData> lobbyStageDatas;
    private int currentStageIndex = 0;

    private List<GameObject> lobbyStageUIs;

    private ProgressService progressService;
    
    
    private void Start()
    {
        progressService = GlobalManagers.Service.GetService<ProgressService>();
        BindData();
        CreateStageUI();
        BindEvent();
        DrawStage();
        
        if (progressService?.ShowUnLockUI == true)
        {
            OpenStageUnLockUI();
        }
    }

    
    /// <summary>
    /// 데이터를 바인딩 합니다.
    /// </summary>
    private void BindData()
    {
        var data = GlobalManagers.Resource.GetResources<LobbyStageData>();
        var sortedData = data.OrderBy((x) => x.StageLevel);
        lobbyStageDatas = sortedData.ToList();
    }

    

    private void OnEnable()
    {
        if (progressService?.ShowUnLockUI == true)
        {
            OpenStageUnLockUI();
        }
    }

    
    
    /// <summary>
    /// 스테이지 UNLOCK UI를 엽니다.
    /// </summary>
    private void OpenStageUnLockUI()
    {
        progressService.CompleteShowClearUI();
        EStageType unLockStageType = progressService.UnLockStageType;
        LobbyStageData  currentStage = GetLobbyStageData(unLockStageType);
            
        UnLockUIData unLockUIData = new UnLockUIData()
        {
            StageImage = currentStage.StageMainImage
        };
        GlobalManagers.UI.PopUpContainer.OpenUI<UnLockUI>(unLockUIData);
    }


    /// <summary>
    /// 스테이지 타입에 해당하는 스테이지 데이터를 가져옵니다.
    /// </summary>
    /// <param name="stageType"></param>
    /// <returns></returns>
    private LobbyStageData GetLobbyStageData(EStageType stageType)
    {
        return lobbyStageDatas.Find(x => x.StageType == stageType);
    }


    /// <summary>
    /// 스테이지 ui 프리팹을 미리 풀링 합니다.
    /// </summary>
    private void CreateStageUI()
    {
        lobbyStageUIs = new List<GameObject>();
        
        foreach (var stage in lobbyStageDatas)
        {
            GameObject go = Instantiate(stage.StageUIPrefab,transform);
            go.transform.position = stagePos.position;
            go.transform.SetParent(stagePos.transform);
            lobbyStageUIs.Add(go);
        }
    }


    
    private void BindEvent()
    {
        stageNextBtn.OnClicked += NextStage;
        stagePrevBtn.OnClicked += PrevStage;
    }

    private void OnDestroy()
    {
        stageNextBtn.OnClicked -= NextStage;
        stagePrevBtn.OnClicked -= PrevStage;
    }
    
    
    /// <summary>
    /// 현재 선택된 스테이지 타입을 불러옵니다.
    /// </summary>
    /// <returns></returns>
    public EStageType GetCurrentStageType()
        => lobbyStageDatas[currentStageIndex].StageType;
    
    
    /// <summary>
    /// 다음 스테이지의 데이터에 위치합니다..
    /// </summary>
    private void NextStage()
    {
        GlobalManagers.Audio.PlaySfx(GlobalDefines.ResourceKey.Audio.BUTTON_CLICK_SFX);
        currentStageIndex = Mathf.Min(currentStageIndex + 1, lobbyStageDatas.Count - 1);
        DrawStage();
    }
    
    /// <summary>
    /// 이전의 스테이지 데이터에 위치합니다.
    /// </summary>
    private void PrevStage()
    {
        GlobalManagers.Audio.PlaySfx(GlobalDefines.ResourceKey.Audio.BUTTON_CLICK_SFX);
        currentStageIndex = Mathf.Max(currentStageIndex - 1, 0);
        DrawStage();
    }
    
    
    
    //현재 선택된 스테이지의 데이터를 참조하여 UI에 그립니다.
    private void DrawStage()
    { 
        LobbyStageData currentStage = lobbyStageDatas[currentStageIndex];
        
        foreach(var stageUI in lobbyStageUIs)
            stageUI.SetActive(false);
        
        if (currentStage != null)
        {
            bool isOpen = progressService.GetStageProgress(currentStage.StageType);
            lockObject.SetActive(!isOpen);
            
            stageName.text =
                Localization.GetLocaliationText(Localization.LocalizationTable.UI, currentStage.StageNameKey);
            lobbyStageUIs[currentStageIndex].SetActive(true);
            mainBG.color = currentStage.StageMainColor;
        }
    }

}
