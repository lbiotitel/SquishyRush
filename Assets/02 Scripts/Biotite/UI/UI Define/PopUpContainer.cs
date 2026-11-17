using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PopUpContainer : IUIContainer
{
    private Transform uiCanvasTr;
    private Transform closedCanvasTr;

    private BaseUI frontUI;
    private Dictionary<Type, GameObject> openUIPool;
    private Dictionary<Type, GameObject> closedUIPool;
    
    //초기화
    public void Initialize(Transform uiCanvasTr, Transform closedCanvasTr)
    { 
        this.uiCanvasTr = uiCanvasTr;
        this.closedCanvasTr = closedCanvasTr;
        openUIPool = new Dictionary<Type, GameObject>();
        closedUIPool = new Dictionary<Type, GameObject>();
    }



    //특정 UI를 가져옵니다. 만약 UI가 이미 열려있다면 isOpen을 true로 반환합니다. 없다면 새로 만듭니다.
    private BaseUI GetUI<T>(out bool isOpen)
    {
        Type type = typeof(T);

        BaseUI res = null;
        GameObject uiObject = null;
        isOpen = false;

        if (openUIPool.TryGetValue(type, out uiObject))
        {
            if (uiObject != null)
            {
                res = uiObject.GetComponent<BaseUI>();
                isOpen = true;
                return res;
            }

            openUIPool.Remove(type);
        }
        else if (closedUIPool.TryGetValue(type, out uiObject))
        {
            if (uiObject != null)
            {
                res = uiObject.GetComponent<BaseUI>();
                closedUIPool.Remove(type);
                return res;
            }

            closedUIPool.Remove(type);
        }
        
        if (!GlobalManagers.Resource.TryGetResource<GameObject>($"{type}", out GameObject uiPrefab))
        {
            Logger.LogError($"{type}에 해당하는 ui 프리팹이 없습니다.");
            return null;
        }

        var uiObj = GameObject.Instantiate(uiPrefab);
        res = uiObj.GetComponent<BaseUI>();
        res.SetOwner(this);
        
        return res;
    }


    //특정 UI를 열고 만약 UI가 이미 열려있다면 열려있다는 로그를 남기고 아무것도 하지 않습니다.
    public BaseUI OpenUI<T>(BaseUIData data = null)
    {
        var ui = GetUI<T>(out bool isOpen);
        if (!ui) return null;
        if (isOpen) return ui;                

        if (!ui.IsInit) ui.Initialize(uiCanvasTr);
        ui.transform.SetParent(uiCanvasTr);
        ui.transform.SetSiblingIndex(uiCanvasTr.childCount);   
        ui.SetInfo(data);
        ui.gameObject.SetActive(true);
        ui.OpenUI();

        frontUI = ui;
        openUIPool.Add(typeof(T), ui.gameObject);
        return frontUI;
    }


    //UI를 닫습니다.
    public void CloseUI(BaseUI ui)
    {
        Type type = ui.GetType();

        Logger.Log($"{GetType()} => {type} Close UI 닫힘");
        ui.gameObject.SetActive(false);
        openUIPool.Remove(type);

        if (!closedUIPool.TryGetValue(type, out GameObject uiObject))
        {
            closedUIPool.Add(type, ui.gameObject);
        }

        ui.transform.SetParent(closedCanvasTr);

        frontUI = null;
        int lastChildIndex = uiCanvasTr.childCount - 1;

        if (lastChildIndex < 0) return;

        Transform lastChild = uiCanvasTr.GetChild(lastChildIndex);
        if (lastChild != null)
        {
            BaseUI tmp = lastChild.GetComponent<BaseUI>();
            if (tmp)
            {
                frontUI = tmp;
            }
        }
    }


    //활성화된 UI를 가져옵니다. 만약 해당 UI가 열려있지 않다면 null을 반환합니다.
    public BaseUI GetActiveUI(BaseUI ui)
    {
        Type type = ui.GetType();
        return openUIPool.TryGetValue(type, out GameObject uiObj) ? uiObj.GetComponent<BaseUI>() : null;
    }


    //가장 앞에 있는 UI를 가져옵니다. 만약 UI가 열려있지 않다면 null을 반환합니다.
    public BaseUI GetFrontUI()
        => frontUI;


    //가장 앞에 있는 UI를 닫습니다. 만약 UI가 열려있지 않다면 아무것도 하지 않습니다.
    public void CloseFrontUI()
    {
        if (frontUI) frontUI.CloseUI();
    }


    //모든 UI를 닫습니다.
    public void CloseAll()
    {
        while (frontUI != null)
        {
            frontUI.CloseUI();
        }
    }


    public void Release()
    {
        if (frontUI != null)
        {
            GameObject.Destroy(frontUI.gameObject);
            frontUI = null; 
        }

        foreach (var key in openUIPool)
        {
            if (key.Value != null)
            {
                GameObject.Destroy(key.Value);
            }
        }


        foreach (var key in closedUIPool)
        {
            if (key.Value != null)
            {
                GameObject.Destroy(key.Value);
            }
        }

        openUIPool.Clear();
        closedUIPool.Clear();
    }
}
