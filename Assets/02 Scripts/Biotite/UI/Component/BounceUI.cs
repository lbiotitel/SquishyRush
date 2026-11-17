using System;
using UnityEngine;
using DG.Tweening;

public class BounceUI : MonoBehaviour
{
    [SerializeField] private float jumpForce = 50f;
    [SerializeField] private float jumpDuration = 0.5f;

    private Vector3 originLocalPos; 
    private Tween bounceTween;

    private void Awake()
    {
        originLocalPos = transform.localPosition;
    }

    private void OnEnable()
    {
        transform.localPosition = originLocalPos;

        bounceTween = transform.DOBlendableLocalMoveBy(new Vector3(0, jumpForce, 0), jumpDuration)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.OutQuad)
            .SetLink(gameObject);
    }

    private void OnDisable()
    {
        bounceTween?.Kill();
        transform.localPosition = originLocalPos;
    }

    private void OnDestroy()
    {
        bounceTween?.Kill();
    }
}