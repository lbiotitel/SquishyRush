using System.Collections;
using System.Collections.Generic;
using UnityEngine;



//백엔드 에러에 관한 ui 처리 입니다.
public class BackEndErrorUIHandler
{
    private ConfirmUIData maintenanceData; //점검
    private ConfirmUIData tooManyRequestData; //너무 많은 요청
    private ConfirmUIData deviceBlockedData; //차단된 디바이스
    private ConfirmUIData firstLoadFailedData; //초기화실패
    private ConfirmUIData requestFailedData; //요청 실패
    private ConfirmUIData loginFailedData; //로그인 실패
    private ConfirmUIData chartLoadFailedData; //차트 로드 실패
    private ConfirmUIData userDataLoadFailedData; //유저 데이터 로드 실패
    
    public void Initialize()
    {
        GlobalManagers.Event.AddEvent<OnBackEndErrorEventData>(OnBackEndErrorEventHandler);
        CreatePopUpDatas();
    }

    public void Release()
    {
        GlobalManagers.Event.RemoveEvent<OnBackEndErrorEventData>(OnBackEndErrorEventHandler);    
    }


    /// <summary>
    /// Confirm ui 팝업에 들어갈 데이터르들을 미리 만들어 캐싱 합니다.
    /// </summary>
    private void CreatePopUpDatas()
    {
        maintenanceData = new ConfirmUIData()
        {
            ConfirmType = EConfirmType.OK,
            LocalizationTableId = Localization.LocalizationTable.Error,
            Title = LocaliztionKeyCode.UI_MAINTENANCE_ERROR_TITLE_TEXT,
            Desc = LocaliztionKeyCode.UI_MAINTENANCE_ERROR_DESC_TEXT,
            OkText = LocaliztionKeyCode.UI_ERROR_OK_TEXT,
            OkAction = () => Application.Quit(0) 
        };

        tooManyRequestData = new ConfirmUIData()
        {
            ConfirmType = EConfirmType.OK,
            LocalizationTableId = Localization.LocalizationTable.Error,
            Title =  LocaliztionKeyCode.UI_TOOMANYREQUEST_ERROR_TITLE_TEXT,
            Desc = LocaliztionKeyCode.UI_TOOMANYREQUEST_ERROR_DESC_TEXT,
            OkText = LocaliztionKeyCode.UI_ERROR_OK_TEXT,
        };

        deviceBlockedData = new ConfirmUIData()
        {
            ConfirmType = EConfirmType.OK,
            LocalizationTableId = Localization.LocalizationTable.Error,
            Title = LocaliztionKeyCode.UI_DEVICEBLOCKED_ERROR_TITLE_TEXT,
            Desc =  LocaliztionKeyCode.UI_DEVICEBLOCKED_ERROR_DESC_TEXT,
            OkText = LocaliztionKeyCode.UI_ERROR_OK_TEXT,
            OkAction = () => Application.Quit(0) 
        };

        firstLoadFailedData = new ConfirmUIData()
        {
            ConfirmType = EConfirmType.OK,
            LocalizationTableId = Localization.LocalizationTable.Error,
            Title = LocaliztionKeyCode.UI_FIRSTLOADFAILED_ERROR_TITLE_TEXT,
            Desc = LocaliztionKeyCode.UI_FIRSTLOADFAILED_ERROR_DESC_TEXT,
            OkText = LocaliztionKeyCode.UI_ERROR_OK_TEXT,
            OkAction = () => Application.Quit(0)
        };

        requestFailedData = new ConfirmUIData()
        {
            ConfirmType = EConfirmType.OK,
            LocalizationTableId = Localization.LocalizationTable.Error,
            Title = LocaliztionKeyCode.UI_REQUEST_FAILED_ERROR_TITLE_TEXT,
            Desc = LocaliztionKeyCode.UI_REQUEST_FAILED_ERROR_DESC_TEXT,
            OkText = LocaliztionKeyCode.UI_ERROR_OK_TEXT,
        };
        
        loginFailedData = new ConfirmUIData
        {
            Title = LocaliztionKeyCode.UI_LOGIN_ERROR_TITLE,
            Desc = LocaliztionKeyCode.UI_LOGIN_ERROR_DESC,
            OkText = LocaliztionKeyCode.UI_OK_TEXT,
            ConfirmType = EConfirmType.OK
        };

        chartLoadFailedData = new ConfirmUIData
        {
            Title = LocaliztionKeyCode.UI_CHART_LOAD_FAILED_TITLE_TEXT,
            Desc = LocaliztionKeyCode.UI_CHART_LOAD_FAILED_DESC_TEXT,
            OkAction = () => Application.Quit(0),
            OkText = LocaliztionKeyCode.UI_OK_TEXT,
            ConfirmType = EConfirmType.OK
        };

        userDataLoadFailedData = new ConfirmUIData
        {
            Title = LocaliztionKeyCode.UI_USER_DATA_LOAD_FAILED_TITLE_TEXT,
            Desc = LocaliztionKeyCode.UI_USER_DATA_LOAD_FAILED_DESC_TEXT,
            OkAction = () => Application.Quit(0),
            OkText = LocaliztionKeyCode.UI_OK_TEXT,
            ConfirmType = EConfirmType.OK
        };
    }


    /// <summary>
    /// 에러에 따른 ui 처리를 합니다.
    /// </summary>
    /// <param name="eventData"></param>
    private void OnBackEndErrorEventHandler(OnBackEndErrorEventData e)
    {
        ConfirmUIData data = null;

        switch (e.ErrorType)
        {
            case EBackEndErrorType.ChartDataLoadFailed:
                data = chartLoadFailedData;
                break;
            case EBackEndErrorType.DeviceBlockedError:
                data = deviceBlockedData;
                break;
            case EBackEndErrorType.LoginFailed:
                data = loginFailedData;
                break;
            case EBackEndErrorType.UserDataLoadFailed:
                data = userDataLoadFailedData;
                break;
            case EBackEndErrorType.FirstLoadFailed:
                data = firstLoadFailedData;
                break;
            case EBackEndErrorType.MaintenanceError:
                data = maintenanceData;
                break;
            case EBackEndErrorType.RequestFailed:
                data = requestFailedData;
                break;
            case EBackEndErrorType.TooManyRequestByLocalError:
                data = tooManyRequestData;
                break;
            case EBackEndErrorType.TooManyRequestError:
                data = tooManyRequestData;
                break;
        }
        
        OpenErrorConfirmUI(data);
    }

    
    /// <summary>
    /// 에러 팝업 ui를 엽니다.
    /// </summary>
    /// <param name="data"></param>
    private void OpenErrorConfirmUI(ConfirmUIData data)
    {
        GlobalManagers.UI.PopUpContainer.OpenUI<ConfirmUI>(data);
    }
}
