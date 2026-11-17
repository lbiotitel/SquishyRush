using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;


//토글 스위치 ui 기능을 정의 합니다.
public class ToggleSwitch : MonoBehaviour,IPointerClickHandler
{
    [SerializeField] private GameObject toggleOffObject;
    [SerializeField] private GameObject toggleOnObject;

    public event Action<bool> OnToggleStateChanged;
    private bool isOn = false;

    public void OnPointerClick(PointerEventData eventData)
    {
        SetToggleState(!isOn);
        OnToggleStateChanged?.Invoke(isOn);
    }

    public void SetToggleState(bool state)
    {
        isOn = state;
        UpdateToggleVisual();
    }


    private void UpdateToggleVisual()
    { 
        if (toggleOffObject != null && toggleOnObject != null)
        {
            toggleOffObject.SetActive(!isOn);
            toggleOnObject.SetActive(isOn);
        }
    }


}
