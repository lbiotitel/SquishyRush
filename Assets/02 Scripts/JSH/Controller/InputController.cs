using System;
using UnityEngine;
using UnityEngine.EventSystems;

public enum EPressedType
{
    PressDown,
    PressUp,
    ShortPress,
    LongPress
}

public class InputController : MonoBehaviour
{
    [Header("Press")]
    [SerializeField] private float longPressTime = 0.35f;
    [SerializeField] private float dragCancelDistance = 20f;

    private float pressStartTime;
    private Vector2 pressStartScreenPos;
    
    private bool isPressing = false;    
    private bool longPressed = false;   
    private bool dragCanceled = false;   
    
    public event Action<EPressedType, Vector2> OnPressEvent;

    private void Update()
    {
        if (Input.GetMouseButtonDown(0)) OnPressDown();
        else if (Input.GetMouseButton(0)) OnPressHold();

        if (Input.GetMouseButtonUp(0)) OnPressUp();
    }

    private void OnPressDown()
    {
        isPressing = false;
        longPressed = false;
        dragCanceled = false;
        isPressing = true;
        
        pressStartTime = Time.unscaledTime;
        pressStartScreenPos = Input.mousePosition;
        OnPressEvent?.Invoke(EPressedType.PressDown, Input.mousePosition);
        Logger.Log("손을 누르기 시작");
    }

    private void OnPressHold()
    {
        if (!isPressing || longPressed || dragCanceled) return;

        Vector2 delta = (Vector2)Input.mousePosition - pressStartScreenPos;
        if (delta.sqrMagnitude > dragCancelDistance * dragCancelDistance)
        {
            dragCanceled = true;
            Logger.Log("범위 벗어남");
            return;
        }

        if (Time.unscaledTime - pressStartTime >= longPressTime)
        {
            longPressed = true;
            Logger.Log("길게 누름");
            OnPressEvent?.Invoke(EPressedType.LongPress, Input.mousePosition);
        }
    }

    private void OnPressUp()
    {
        if (!isPressing) return;
        
        if (!longPressed && !dragCanceled)
        {
            Logger.Log("짧게 누름");
            OnPressEvent?.Invoke(EPressedType.ShortPress, Input.mousePosition);
        }

        Logger.Log("손을 뗌");
        OnPressEvent?.Invoke(EPressedType.PressUp, Input.mousePosition);
        isPressing = false;
    }
}