using BackEnd;
using UnityEngine;
using Cysharp.Threading.Tasks;




//서버에 관한 로직을 담당합니다.
public class BackEndManager 
{
    public UserLoginBackEnd Login { get; private set; }
    public UserGameDataBackEnd UserGame { get; private set; }
    public UserRankingBackEnd UserRanking { get; private set; }
    public ChartBackEnd Chart { get; private set; }
    public GachaBackEnd Gacha { get; private set; }
    public MailBackEnd Mail { get; private set; }
    public BackEndTimer BackEndTimer { get; private set; }
    
    private BackEndErrorHandlerService backEndErrorHandler;
    private bool isInit = false;

    /// <summary>
    /// 백엔드를 초기화하면서 관련된 로직 클래스들을 생성합니다.
    /// </summary>
    /// <returns></returns>
    public bool Initialize()
    {
        if (!isInit)
        {
            var bro = Backend.Initialize();
            if (!bro.IsSuccess()) return false;   
            isInit = true;
        }
        
        backEndErrorHandler = new BackEndErrorHandlerService();
        backEndErrorHandler.Initialize();

        BackEndTimer = new BackEndTimer();
        Login = new UserLoginBackEnd();
        UserGame = new UserGameDataBackEnd();
        UserRanking = new UserRankingBackEnd();
        Chart = new ChartBackEnd();
        Gacha = new GachaBackEnd();
        Mail = new MailBackEnd();
        return true;
    }
    
    
    /// <summary>
    /// 실시간 알림 핸들러를 등록합니다.
    /// </summary>
    public bool SetNotificationHandler() {
        
        Backend.Notification.OnAuthorize = (bool isSuccess, string reason) => 
        {
            if (isSuccess) 
            {
                Logger.Log("실시간 알림서버 접속 성공");
            }
            else
            {
                Logger.LogError($"실시간 알림 서버 접속에 실패했습니다. {reason}");
            }
        };

        Backend.Notification.OnNewPostCreated = (BackEnd.Socketio.PostRepeatType postRepeatType, string title, string content, string author) =>
        {
            UniTask.Post(() =>
            {
                Logger.Log("새로운 우편 도착!");
                GlobalManagers.Event.NotifyEvent(new OnNewPostCreatedEventData());
            });
        };
             
        Backend.Notification.Connect();

        return true;
    }
    
    
    public void Release()
    {
        Backend.Notification.DisConnect();
        Backend.Notification.OnAuthorize = null;
        Backend.Notification.OnNewPostCreated = null;
        
        backEndErrorHandler?.Release();
        BackEndTimer = null;
        backEndErrorHandler = null;
        Login = null;
        UserGame = null;
        UserRanking = null;
        Chart = null;
        Gacha = null;
        Mail = null;
    }
}
