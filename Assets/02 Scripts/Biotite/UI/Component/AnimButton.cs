using DG.Tweening;
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


//간단한 애니메이션효과를 지닌 버튼 입니다.
public class AnimButton : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private float punchScale;
    [SerializeField] private float punchDuration;
    private bool isAnim = false;

    public event Action OnClicked;


    public void OnPointerClick(PointerEventData eventData)
    {
        if (isAnim) return;
        isAnim = true;

        transform.DOPunchScale(Vector3.one * punchScale, punchDuration)
            .OnComplete(() =>
            {
                OnClicked?.Invoke();
                isAnim = false;
            }).SetLink(gameObject);
    }

    private void OnDestroy()
    {
        OnClicked = null;
    }


    public void RemoveAllAction()
    {
        OnClicked = null;
    }
}
