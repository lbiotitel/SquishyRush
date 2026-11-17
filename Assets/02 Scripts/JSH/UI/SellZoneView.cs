using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class SellZoneView : MonoBehaviour
{
    [SerializeField] private RectTransform zoneRect;
    [SerializeField] private GameObject root;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private Image background;
    [SerializeField] private Color normalColor;
    [SerializeField] private Color hoverColor;

    public void ShowUI(int price)
    {
        root.SetActive(true);
        priceText.text = price.ToString();
        SetHover(false);
    }

    public void HideUI() => root.SetActive(false);

    public void SetHover(bool hover)
    {
        background.color = hover ? hoverColor : normalColor;
        zoneRect.localScale = Vector3.one; 
    }
    
    public bool Contains(Vector2 screenPos, Camera uiCamera)
        => root.activeSelf
        && RectTransformUtility.RectangleContainsScreenPoint(zoneRect, screenPos, uiCamera);
}
