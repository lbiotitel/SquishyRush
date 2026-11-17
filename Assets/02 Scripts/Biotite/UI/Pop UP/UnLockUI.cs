using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine.UI;
using UnityEngine;


public class UnLockUIData : BaseUIData
{
    public Sprite StageImage { get; set; }
}

//스테이지 잠금해제 연출 UI 입니다.
public class UnLockUI : BaseUI
{
    [Header("Button")]
    [SerializeField] private AnimButton closeButton;

    [Header("Image")] 
    [SerializeField] private Image stageImage;

    [Header("Object")] 
    [SerializeField] private GameObject lightEffectObject;
    
    [Header("Setting")]
    [SerializeField] private float lightRotateDuration = 6f;
    
    private Animator animator;

    private Tween lightRotateTween;

    public override void Initialize(Transform anchor)
    {
        base.Initialize(anchor);
        animator = GetComponent<Animator>();
        BindEvents();
    }


    public override void SetInfo(BaseUIData data)
    {
        base.SetInfo(data);
        
        UnLockUIData unLockUIData = data as UnLockUIData;

        if (unLockUIData != null)
        {
            stageImage.sprite = unLockUIData.StageImage;
        }
    }


    public override void OpenUI()
    {
        base.OpenUI();
        animator.enabled = true;
        closeButton.gameObject.SetActive(false);
        GlobalManagers.Audio.PlaySfx(GlobalDefines.ResourceKey.Audio.STAGE_UNLOCK_SFX);
    }


    private void BindEvents()
    {
        closeButton.OnClicked += CloseUI;
    }

    private void OnDestroy()
    {
        closeButton.OnClicked  -= CloseUI;
        KillLightTween();
    }

    public override void CloseUI()
    {
        base.CloseUI();
        KillLightTween();
    }


    private void PlayLightEffect()
    {
        KillLightTween();

        lightEffectObject.SetActive(true);
        lightEffectObject.transform.localRotation = Quaternion.identity;

        lightRotateTween = lightEffectObject.transform
            .DOLocalRotate(new Vector3(0f, 0f, -360f), lightRotateDuration, RotateMode.FastBeyond360)
            .SetEase(Ease.Linear)
            .SetLoops(-1, LoopType.Restart)
            .SetUpdate(true)              
            .SetLink(lightEffectObject);
    }
    
    
    private void KillLightTween()
    {
        if (lightRotateTween != null && lightRotateTween.IsActive())
        {
            lightRotateTween.Kill();
        }
        lightRotateTween = null;
    }


    
    /// <summary>
    /// 애니메이션을 끝날때 호출됩니다.
    /// </summary>
    public void OnAnimationFinished()
    {
        Debug.Log("[UnLock] OnAnimationFinished 호출");
        animator.enabled = false;
        closeButton.gameObject.SetActive(true);
        PlayLightEffect();
    }
    
}
