using System;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;



//깜빡이는 ui text 효과를 가진 text를 정의합니다.
[RequireComponent(typeof(CanvasGroup))]
public class BlinkText : MonoBehaviour
{
    [SerializeField] private float blinkDuration = 1;
    private CanvasGroup targetCanvas;
    private bool isBlink = false;
    private bool onOff = false;
    private bool stop = false;

    private void Start()
    {
        targetCanvas = GetComponent<CanvasGroup>();
    }

    private void Update()
    {
        if (stop) return;
        if (isBlink) return;
        isBlink = true;
        if (!onOff) targetCanvas.DOFade(1, blinkDuration).OnComplete(() => { onOff = true; isBlink = false; });
        else targetCanvas.DOFade(0, blinkDuration).OnComplete(() => { onOff = false; isBlink = false; });
    }

    private void OnDestroy()
    {
        stop = true;
        targetCanvas?.DOKill();  
    }
}
