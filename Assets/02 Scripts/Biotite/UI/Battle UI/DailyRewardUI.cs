using System;
using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;


//출석체크를 확인하는 팝업 UI 입니다.
public class DailyRewardUI : BaseUI
{
    [Header("Button")]
    [SerializeField] private AnimButton attendanceCheckButton;
    [SerializeField] private AnimButton closeButton;
    
    [Header("Transform")]
    [SerializeField] private Transform content;

    private GameObject dailyRewardFrameUIPrefab;
    private List<DailyRewardCellView> dailyRewardNodeUIs;
    
    //보상 차트데이터 
    List<RewardData> dailyRewardData;

    private AttendanceService attendanceService;


    public override void Initialize(Transform anchor)
    {
        base.Initialize(anchor);
            
        attendanceService = GlobalManagers.Service.GetService<AttendanceService>();

        if (!GlobalManagers.Resource.TryGetResource<GameObject>(GlobalDefines.ResourceKey.UI.DAILY_REWARD_CELL_VIEW_PREFAB, out GameObject prefab))
        {
            Logger.LogError($"{GlobalDefines.ResourceKey.UI.DAILY_REWARD_CELL_VIEW_PREFAB}에 해당하는 프리팹이 없습니다.");
            return;
        }

        dailyRewardFrameUIPrefab = prefab;
        
        dailyRewardData = new List<RewardData>();
        
        for (int i = 0; i < GlobalDefines.MAX_VALUE.MAX_DAILY_REWARD_COUNT; i++)
            dailyRewardData.Add(GlobalManagers.Chart.GetDailyReward(i + 1));

        BindEvents();
        CreateDailyRewardFrame();
        DrawDailyRewardUI();
    }


    private void BindEvents()
    {
        closeButton.OnClicked += CloseUI;
        attendanceCheckButton.OnClicked += OnClickedAttendanceCheckButton;
    }


    private void OnDestroy()
    {
        closeButton.OnClicked  -= CloseUI;
        attendanceCheckButton.OnClicked -= OnClickedAttendanceCheckButton;
    }


    
    private void OnClickedAttendanceCheckButton()
        => MarkAttendance().Forget();


    /// <summary>
    /// 출석보상에 필요한 프레임UI를 미리 만들어 풀링합니다.
    /// </summary>
    private void CreateDailyRewardFrame()
    {
        dailyRewardNodeUIs = new List<DailyRewardCellView>();
        int count = GlobalDefines.MAX_VALUE.MAX_DAILY_REWARD_COUNT;

        for (int i = 0; i < count; i++)
        {
            GameObject go = Instantiate(dailyRewardFrameUIPrefab, content);
            DailyRewardCellView dailyRewardCellView = go.GetComponent<DailyRewardCellView>();

            string rewardID = dailyRewardData[i].RewardID;
            int amount = dailyRewardData[i].Amount;

            if (!GlobalManagers.Resource.TryGetResource<Sprite>($"{rewardID}.sprite", out Sprite sprite))
            {
                Logger.LogError($"{rewardID}.sprite 에 해당하는 스프라이트가 없습니다.");
                return;
            }

            Sprite itemImage = sprite;

            dailyRewardCellView.Initailize(itemImage, $"{i + 1}", amount.ToString());
            dailyRewardNodeUIs.Add(dailyRewardCellView);
        }
    }



    /// <summary>
    /// 저장된 출석체크 데이터를 이용하여 출석체크 UI를 그립니다
    /// </summary>
    private void DrawDailyRewardUI()
    {
        bool[] attendanceCheck = attendanceService.AttendanceChecked;
        bool isAttendance = attendanceService.IsAttendanceCheckedToday;

        for (int i = 0; i < attendanceCheck.Length; i++)
        {
            dailyRewardNodeUIs[i].SetCheckObject(attendanceCheck[i]);
        }

        attendanceCheckButton.gameObject.SetActive(!isAttendance);
    }


    
    /// <summary>
    /// 출석체크를 합니다.
    /// </summary>
    private async UniTask MarkAttendance() 
    {
        bool res = await Utils.WaitForAwaitMethodWithOverlayUI(attendanceService.MarkAttendance());
        if (res) DrawDailyRewardUI();
    }

}
