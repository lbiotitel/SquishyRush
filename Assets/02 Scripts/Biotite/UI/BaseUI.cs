using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


// UIData 클래스는 UI에 필요한 데이터를 담는 클래스입니다. 
public class BaseUIData
{
    public Action OnShow;
    public Action OnClose;
}


public class BaseUI : MonoBehaviour
{
    private IUIContainer owner;
    
    private Animation openAnimation;

    private Action onShow;
    private Action onClose;
    public bool IsInit { get; private set; } = false;
    
    /// <summary>
    /// 자신의 부모 컨테이너를 설정합니다.
    /// </summary>
    /// <param name="owner"></param>
    public void SetOwner(IUIContainer owner)
        => this.owner = owner;
    
    //UI를 초기화 하는 함수입니다. anchor는 UI가 붙을 부모 오브젝트입니다.
    public virtual void Initialize(Transform anchor)
    {
        Logger.Log($"{GetType()} 초기화");

        IsInit = true;
        transform.SetParent(anchor);
        var rectTr = GetComponent<RectTransform>();
        rectTr.localPosition = Vector3.zero;
        rectTr.localRotation = Quaternion.identity;
        rectTr.localScale = Vector3.one;
        rectTr.offsetMax = Vector2.zero;
        rectTr.offsetMin = Vector2.zero;
    }


    //UI에 필요한 설정을 하는 함수입니다. data는 UI에 필요한 데이터를 담고 있는 객체입니다.
    public virtual void SetInfo(BaseUIData data)
    {
        if (data == null) return;
        onShow = data.OnShow;
        onClose = data.OnClose;
    }

    public virtual void OpenUI()
    {
        if (openAnimation)
        {
            openAnimation.Play();
        }

        onShow?.Invoke();
        onShow = null;
    }
    
    public virtual void CloseUI()
    {
        onClose?.Invoke();
        onClose = null;
        owner.CloseUI(this);
    }
}
