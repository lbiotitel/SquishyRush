using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TraitTagView : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text label;

    public void Set(Sprite sprite, string text)
    {
        gameObject.SetActive(true);
        icon.sprite = sprite;
        label.text = text;
    }

    public void Hide() => gameObject.SetActive(false);
}
