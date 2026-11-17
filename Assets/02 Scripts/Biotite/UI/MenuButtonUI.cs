using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;



//로비에 있는 버튼 UI를 정의 합니다.
public class MenuButtonUI : MonoBehaviour,IPointerClickHandler
{
    [Header("Animation")]
    [SerializeField] private float punchScale;
    [SerializeField] protected float punchDuration;

    [SerializeField] private TMP_Text menuText;
    private bool isClicked = false;
    private Animator animator;

    public event Action<MenuButtonUI> OnClicked;
    

    public void Initialize(string menuName)
    {
        animator = GetComponent<Animator>();
        animator.keepAnimatorStateOnDisable = true; 
        menuText.text = menuName;
    }

    public void ResetAnimation()
    {
        isClicked = false;
        animator.SetBool("Idle", true);
        animator.SetBool("Clicked",false);
    }

    public void ClickedAnimation()
    {
        isClicked = true;
        animator.SetBool("Idle", false);
        animator.SetBool("Clicked", true);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (isClicked) return;
        transform.DOPunchScale(Vector3.one * punchScale, punchDuration)
            .OnComplete(() =>
            {
                OnClicked?.Invoke(this);
            });
    }
}
