using UnityEngine;
using UnityEngine.UI;

public enum ERoundSlotState
{
    Cleared,
    Current,
    Upcoming
}

public class RoundSlotView : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private float clearedAlpha = 0.4f;
    [SerializeField] private float currentScale = 1.2f;

    public void Set(Sprite sprite, ERoundSlotState state)
    {
        icon.enabled = true;
        icon.sprite = sprite;

        Color c = icon.color;
        c.a = state == ERoundSlotState.Cleared ? clearedAlpha : 1f;
        icon.color = c;
        
        transform.localScale = Vector3.one * (state == ERoundSlotState.Current ? currentScale : 1f);
    }
}
