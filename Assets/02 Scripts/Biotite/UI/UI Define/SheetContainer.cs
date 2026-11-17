using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


//시트 Ui를 괸리하는 클래스입니다.
public class SheetContainer : IUIContainer
{
    private Transform uiCanvasTr;
    private Transform closedCanvasTr;

    private Dictionary<Type, GameObject> closedUIPool;
    private BaseUI currentUI = null;
    
    public void Initialize(Transform uiCanvasTr, Transform closedCanvasTr)
    {
        this.uiCanvasTr = uiCanvasTr;
        this.closedCanvasTr = closedCanvasTr;
        closedUIPool = new Dictionary<Type, GameObject>();
    }



    /// <summary>
    /// ui를 가져옵니다 없다면 새로 생성해서 리턴합니다.
    /// </summary>
    /// <param name="isOpen"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    private BaseUI GetUI<T>(out bool isOpen)
    {
        Type type = typeof(T);

        BaseUI res = null;
        GameObject uiObject = null;
        isOpen = false;

        if (currentUI != null && currentUI.GetType() == type)
        {
            isOpen = true;
            res = currentUI;
            return res;
        }
        
        if (closedUIPool.TryGetValue(type, out uiObject))
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


    /// <summary>
    /// 해당하는 ui를 엽니다 없다면 새로 생성하고 엽니다.
    /// </summary>
    /// <param name="data"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public BaseUI OpenUI<T>(BaseUIData data = null)
    {
        var ui = GetUI<T>(out bool isOpen);
        if (!ui) return null;
        if (isOpen) return ui;

        if (currentUI != null)
            CloseUI(currentUI);

        if (!ui.IsInit) ui.Initialize(uiCanvasTr);
        ui.transform.SetParent(uiCanvasTr);
        ui.SetInfo(data);
        ui.gameObject.SetActive(true);
        ui.OpenUI();

        currentUI = ui;
        return currentUI;
    }

    
    //입력받은 ui를 닫습니다.
    public void CloseUI(BaseUI ui)
    {
        Type type = ui.GetType();
        ui.gameObject.SetActive(false);

        if (!closedUIPool.ContainsKey(type))
            closedUIPool.Add(type, ui.gameObject);

        ui.transform.SetParent(closedCanvasTr);
        currentUI = null;
    }


    public BaseUI GetCurrentUI()
        => currentUI;

    
    
    /// <summary>
    /// 모든 시트 ui를 닫습니다.
    /// </summary>
    public void CloseAll()
    {
        if (currentUI != null)
        {
            CloseUI(currentUI);
        }
    }


    public void Release()
    {
        if (currentUI != null)
            GameObject.Destroy(currentUI.gameObject);

        currentUI = null;

        foreach (var key in closedUIPool)
        {
            if (key.Value != null)
            {
                GameObject.Destroy(key.Value);
            }
        }

        closedUIPool.Clear();
    }
}
