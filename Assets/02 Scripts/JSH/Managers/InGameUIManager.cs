using UnityEngine;

public class InGameUIManager : MonoBehaviour
{
    [field: SerializeField] public Transform SheetUICanvas { get; private set; }
    [field: SerializeField] public Transform PopUpCanvas { get; private set; }
    [field: SerializeField] public Transform ClosedCanvas { get; private set; }

    public PopUpContainer PopUpContainer { get; private set; } = null;
    public SheetContainer SheetContainer { get; private set; } = null;

    //초기화
    public void Initialize()
    {
        PopUpContainer = new PopUpContainer();
        PopUpContainer.Initialize(PopUpCanvas, ClosedCanvas);
        SheetContainer = new SheetContainer();
        SheetContainer.Initialize(SheetUICanvas, ClosedCanvas);
    }

    
    
    /// <summary>
    /// 모든 팝업과 시트 UI를 닫습니다.
    /// </summary>
    public void CloseAllUI()
    {
        PopUpContainer.CloseAll();
        SheetContainer.CloseAll();
    }


    //해제
    public void Release()
    {
        SheetContainer.Release();
        PopUpContainer.Release();
    }
}
