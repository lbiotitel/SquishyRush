using BackEnd;
using Cysharp.Threading.Tasks;
using SuperSocket.ClientEngine;
using System;





//출석체크에 관한 로직 클래스 입니다.
public class AttendanceService : IService
{
    private UserServerAttendanceData userServerAttendanceData;
    private ProfileService profileService;
    private RewardService rewardService;
    
    public void Initialize()
    {
        profileService = GlobalManagers.Service.GetService<ProfileService>();
        rewardService = GlobalManagers.Service.GetService<RewardService>();
        userServerAttendanceData = GlobalManagers.UserServerData.GetData<UserServerAttendanceData>();

        CheckAttendance();
    }

    public void Release()
    {
        
    }


    public bool IsAttendanceCheckedToday => userServerAttendanceData.IsAttendanceCheckedToday;
    public bool[] AttendanceChecked => userServerAttendanceData.AttendanceChecked;



    /// <summary>
    /// 이미 출석을 했는데 마지막 로그인 시간과 다르다면 초기화 합니다.
    /// </summary>
    private void CheckAttendance()
    {
        DateTime now = DateTime.UtcNow;
        DateTime lastLoginTime = profileService.LastSavedTime;

        if (now.Month != lastLoginTime.Month)
        {
            userServerAttendanceData.ClearAttendanceChecked();
            userServerAttendanceData.IsAttendanceCheckedToday = false;
        }
        if (now.Day != lastLoginTime.Day)
        {
            int dayIndex = Array.IndexOf(userServerAttendanceData.AttendanceChecked, false);
            if (dayIndex == -1) return;
            userServerAttendanceData.IsAttendanceCheckedToday = false;
        }
    }


    /// <summary>
    /// 출석체크를 진행합니다.
    /// </summary>
    /// <returns></returns>
    public async UniTask<bool> MarkAttendance()
    {
        if (userServerAttendanceData == null)
        {
            Logger.LogError("출석 데이터가 존재하지 않습니다");
            return false;
        }

        //이미 출석체크 완료
        if (userServerAttendanceData.IsAttendanceCheckedToday) return false;
        
        int dayIndex = Array.IndexOf(userServerAttendanceData.AttendanceChecked, false);

        if (dayIndex == -1) return false;
        userServerAttendanceData.SetAttendanceChecked(dayIndex, true);
        userServerAttendanceData.IsAttendanceCheckedToday = true;
        
        bool res = await userServerAttendanceData.SaveDataAsync();

        if (res)
        {
            RewardData rewardData = GlobalManagers.Chart.GetDailyReward(dayIndex + 1);
            rewardService.GiveReward(rewardData);
            
            GlobalManagers.Event.NotifyEvent<OnDoDailyQuestEventData>(new OnDoDailyQuestEventData()
            {
                QuestType = EQuestType.AttendanceCheck
            });

            Logger.Log("춣석체크 성공");
            return true;
        }
        
        
        Logger.Log("출석체크 실패");
        return false;
    }

}
