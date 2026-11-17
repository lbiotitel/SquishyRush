using System;
using System.Collections.Generic;
using UnityEngine;


//UI를 관리하는 클래스입니다. 
public class UIManager : IManager
{

    public Transform SheetUICanvas { get; private set; }
    public Transform PopUpCanvas  { get; private set; }
    public Transform ClosedCanvas  { get; private set; }
    
    
    public PopUpContainer PopUpContainer { get; private set; } = null;
    public SheetContainer SheetContainer { get; private set; } = null;
    
    private BackEndErrorUIHandler backEndErrorUiHandler;

    //초기화
    public bool Initialize()
    {
        SheetUICanvas =  GameObject.Find("SheetUICanvas").transform;
        PopUpCanvas = GameObject.Find("PopUpUICanvas").transform;
        ClosedCanvas = GameObject.Find("ClosedUICanvas").transform;
        
        if (SheetUICanvas == null || PopUpCanvas == null || ClosedCanvas == null)
        {
            Logger.LogError("UI Manage 에서 해당하는 캔버스들을 찾을 수 없습니다.");
            return false;
        }

        backEndErrorUiHandler = new BackEndErrorUIHandler();
        backEndErrorUiHandler.Initialize();
        
        
        PopUpContainer = new PopUpContainer();
        PopUpContainer.Initialize(PopUpCanvas, ClosedCanvas);
        
        
        SheetContainer = new SheetContainer();
        SheetContainer.Initialize(SheetUICanvas, ClosedCanvas);

        return true;
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
        backEndErrorUiHandler.Release();
    }

}
