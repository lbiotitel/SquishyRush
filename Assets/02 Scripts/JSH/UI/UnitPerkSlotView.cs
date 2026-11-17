using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 유닛 상세 UI에서 보유 재능 하나를 보여주는 칸입니다. (아이콘 + 이름)
/// 선택창의 PerkCellView와 달리 클릭, 등급, 설명이 없는 표시 전용입니다.
/// </summary>
public class UnitPerkSlotView : MonoBehaviour
{
    [SerializeField] private Image perkImage;


    /// <summary>
    /// 재능 데이터로 아이콘과 이름을 그립니다.
    /// </summary>
    public void Draw(PerkDataBase data)
    {
        perkImage.sprite = data.PerkIcon;
    }

    public void SetActive(bool active)
    {
        gameObject.SetActive(true);
        perkImage.gameObject.SetActive(active);
    }
}
