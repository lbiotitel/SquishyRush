using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 시너지 툴팁의 단계 한 줄. (번호 동그라미 + 설명)
/// 켜진 단계만 초록 박스와 초록 글자로 보여줍니다.
/// </summary>
public class SynergyTierRowView : MonoBehaviour
{
    [SerializeField] private Image rowBg;
    [SerializeField] private Image numberBadge;
    [SerializeField] private TMP_Text numberText;
    [SerializeField] private TMP_Text descText;
    
    [Header("색")]
    [SerializeField] private Color badgeOn = new Color32(0x6B, 0xD6, 0x5A, 0xFF);
    [SerializeField] private Color badgeOff = new Color32(0xE6, 0xD2, 0xAA, 0xFF);
    [SerializeField] private Color numberOn = Color.white;
    [SerializeField] private Color numberOff = new Color32(0x3B, 0x2A, 0x20, 0xFF);
    [SerializeField] private Color descOn = new Color32(0x24, 0x70, 0x2F, 0xFF);
    [SerializeField] private Color descOff = new Color32(0x9C, 0x86, 0x70, 0xFF);

    public void Set(int requiredCount, string desc, bool isOn)
    {
        rowBg.enabled = isOn;
        numberBadge.color = isOn ? badgeOn : badgeOff;
        numberText.color = isOn ? numberOn : numberOff;
        numberText.text = requiredCount.ToString();
        descText.color = isOn? descOn : descOff;
        descText.text = desc;
    }
}
