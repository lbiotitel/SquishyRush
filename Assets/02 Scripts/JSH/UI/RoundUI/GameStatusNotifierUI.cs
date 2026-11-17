using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum EGameStatus
{
    BattleStart,
    Victory,
    Defeat
}

public class GameStatusNotifierUI : MonoBehaviour
{
    [Serializable]
    public struct StatusEntry
    {
        public EGameStatus Status;
        public Sprite BannerSprite;
        public Sprite IconSprite;
        public string TextKey;
    }

    [SerializeField] private Image bannerImage;
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text text;
    [SerializeField] private Animator animator;
    [SerializeField] private StatusEntry[] entries;
    
    private static readonly int OpenHash = Animator.StringToHash("Open");

    public void Show(EGameStatus status)
    {
        if (!TryGetEntry(status, out StatusEntry entry))
        {
            Logger.LogWarning($"[Notifier] {status} 항목이 지정되지 않았습니다.");
            return;
        }

        bannerImage.sprite = entry.BannerSprite;
        iconImage.sprite = entry.IconSprite;
        text.text = Localization.GetLocaliationText(Localization.LocalizationTable.UI, entry.TextKey);
        
        gameObject.SetActive(true);
        animator.Play(OpenHash, 0, 0f);
    }
    
    public void Hide() => gameObject.SetActive(false);

    private bool TryGetEntry(EGameStatus status, out StatusEntry entry)
    {
        foreach (StatusEntry e in entries)
        {
            if (e.Status != status) continue;
            entry = e;
            return true;
        }

        entry = default;
        return false;
    }
}
