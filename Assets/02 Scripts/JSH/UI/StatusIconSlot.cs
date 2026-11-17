using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StatusIconSlot : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text stackText;

    // 마지막으로 그린 값. 같은 값이 다시 들어오면 아무것도 안 함 → 캔버스 재계산 방지
    private Sprite lastSprite;
    private int lastStack = -1;
    private int lastMore = -1; // -1이면 일반 아이콘 모드, 0 이상이면 "+N" 모드
    private bool isShown;

    // 프리팹에서 켜진 상태로 생성되면 Awake가 돌아 true, 꺼진 상태면 Awake가 안 돌아 기본값 false
    private void Awake() => isShown = gameObject.activeSelf;

    public void Show(Sprite sprite, int stack)
    {
        Activate();

        if (lastMore != -1)
        {
            lastMore = -1;
            icon.enabled = true;
            lastStack = -1;
        }
        

        if (sprite != lastSprite)
        {
            icon.sprite = sprite;
            lastSprite = sprite;
        }

        if (stack != lastStack)
        {
            lastStack = stack;
            bool many = stack > 1;
            if (stackText.gameObject.activeSelf != many) stackText.gameObject.SetActive(many);
            if (many) stackText.SetText("{0}", stack);
        }
    }

    /// <summary>"+N" 모드: 아이콘을 숨기고 가려진 개수만 표시</summary>
    public void ShowMore(int hiddenCount)
    {
        Activate();
        if (hiddenCount == lastMore) return;

        if (lastMore == -1)
        {
            icon.enabled = false;
            lastSprite = null;  // 일반 모드로 돌아갈 때 스프라이트를 다시 넣게 함
            if (!stackText.gameObject.activeSelf) stackText.gameObject.SetActive(true);
        }

        lastMore = hiddenCount;
        lastStack = -1;
        stackText.SetText("+{0}", hiddenCount);
    }

    public void Hide()
    {
        if (!isShown) return;
        isShown = false;
        gameObject.SetActive(false);
    }

    private void Activate()
    {
        if (isShown) return;
        gameObject.SetActive(true);
        isShown = true;
    }
}
