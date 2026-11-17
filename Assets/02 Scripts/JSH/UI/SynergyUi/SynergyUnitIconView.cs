using UnityEngine;
using UnityEngine.UI;

public class SynergyUnitIconView : MonoBehaviour
{
    [SerializeField] private Image bg;
    [SerializeField] private Image icon;
    
    [Header("활성화 창")]
    [SerializeField] private Sprite onBoardSprite;
    [SerializeField] private Sprite offBoardSprite;
    [SerializeField] private Color offIconColor = new Color(0.7f, 0.7f, 0.7f, 0.5f);

    public void Set(UnitData unit, Color frameColor, bool onBoard, Color dimColor)
    {
        icon.sprite = unit.UnitImage;

        if (onBoardSprite != null && offBoardSprite != null)
        {
            bg.sprite = onBoard ? onBoardSprite : offBoardSprite;
            bg.color = onBoard ? GlobalDefines.Rarity.GetRarityPastelColor(unit.UnitRarity) : Color.white;
            icon.color = onBoard ? Color.white : offIconColor;
            return;
        }
        
        bg.color = frameColor;
        
        icon.color = onBoard ? Color.white : dimColor;
        if (!onBoard) bg.color *= dimColor;
    }
}
