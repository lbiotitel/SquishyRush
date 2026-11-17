using BackEnd;

//에러를 핸들링 하는 클래스입니다.
public class BackEndErrorHandlerService
{
    public void Initialize()
    {
        Backend.ErrorHandler.OnMaintenanceError += OnMaintenanceError;
        Backend.ErrorHandler.OnTooManyRequestError += OnTooManyRequestError;
        Backend.ErrorHandler.OnDeviceBlockError += OnDeviceBlockError;
        Backend.ErrorHandler.OnTooManyRequestByLocalError += OnTooManyRequestByLocalError;
    }

    /// <summary>
    /// 등록했던 에러 핸들러들을 해제합니다.
    /// </summary>
    public void Release()
    {
        Backend.ErrorHandler.OnMaintenanceError -= OnMaintenanceError;
        Backend.ErrorHandler.OnTooManyRequestError -= OnTooManyRequestError;
        Backend.ErrorHandler.OnDeviceBlockError -= OnDeviceBlockError;
        Backend.ErrorHandler.OnTooManyRequestByLocalError -= OnTooManyRequestByLocalError;
    }

    private void OnMaintenanceError() => Notify(EBackEndErrorType.MaintenanceError);
    private void OnTooManyRequestError() => Notify(EBackEndErrorType.TooManyRequestError);
    private void OnDeviceBlockError() => Notify(EBackEndErrorType.DeviceBlockedError);
    private void OnTooManyRequestByLocalError() => Notify(EBackEndErrorType.TooManyRequestByLocalError);

    private void Notify(EBackEndErrorType type)
    {
        GlobalManagers.Event.NotifyEvent<OnBackEndErrorEventData>(new OnBackEndErrorEventData { ErrorType = type });
    }
}
