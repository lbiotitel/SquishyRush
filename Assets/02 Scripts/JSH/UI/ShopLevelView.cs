
using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopLevelView : MonoBehaviour
{
    private const string MAX_TEXT = GlobalDefines.MAX_VALUE.MAX_LEVEL_TEXT;
    
    [Header("라벨")]
    [SerializeField] private TMP_Text shopTitleText;
    
    [Header("상점 경험치")] 
    [SerializeField] private TMP_Text shopLevelText;
    [SerializeField] private Slider expSlider;
    [SerializeField] private TMP_Text expText;
    [SerializeField] private float fillDuration = 0.25f;

    [Header("상점 확률")]
    [SerializeField] private TMP_Text[] rarityTexts = new TMP_Text[4];

    private RunContext ctx;
    private ShopSystem shop;

    private int shownLevel = -1;
    private Tween fillTween;
    private Tween punchTween;
    
    public void Init(RunContext ctx, ShopSystem shop)
    {
        if (this.ctx != null) this.ctx.OnShopExpChanged -= Refresh;

        this.ctx = ctx;
        this.shop = shop;

        shopTitleText.text = Localization.GetLocaliationText(Localization.LocalizationTable.UI, LocaliztionKeyCode.UI_SHOP_LEVEL_TEXT);
        
        expSlider.minValue = 0f;
        expSlider.maxValue = 1f;
        expSlider.wholeNumbers = false;
        expSlider.interactable = false;

        ctx.OnShopExpChanged += Refresh;

        shownLevel = -1;
        Refresh();
    }

    private void OnDestroy()
    {
        if (ctx != null) ctx.OnShopExpChanged -= Refresh;
        fillTween?.Kill();
        punchTween?.Kill();
    }

    public void Refresh()
    {
        if (ctx == null || shop == null) return;

        int level = ctx.IGShopLevel;
        bool isFirst = shownLevel < 0;
        bool leveledUp = !isFirst && level > shownLevel;

        DrawBadge(level, leveledUp);
        DrawExp(leveledUp, isFirst);
        
        if(level != shownLevel) DrawOdds(level);
        
        shownLevel = level;
    }

    #region 그리기

    private void DrawBadge(int level, bool leveledUp)
    {
        shopLevelText.text = level.ToString();

        if (!leveledUp) return;
        
        punchTween?.Complete();
        punchTween = shopLevelText.transform
            .DOPunchScale(Vector3.one * 0.3f, 0.3f, 6, 0.5f)
            .SetLink(gameObject);
    }

    private void DrawExp(bool leveledUp, bool immediate)
    {
        float target;

        if (ctx.IsMaxShopLevel)
        {
            expText.text = MAX_TEXT;
            target = 1f;
        }
        else
        {
            int req = ctx.ShopExpReq;
            expText.text = $"{ctx.ShopExp}/{req}";
            target = req > 0 ? (float)ctx.ShopExp / req : 0f;
        }
        
        fillTween?.Kill();

        if (immediate)
        {
            expSlider.value = target;
            return;
        }

        if (leveledUp && !ctx.IsMaxShopLevel)
        {
            fillTween = DOTween.Sequence()
                .Append(expSlider.DOValue(1f, fillDuration))
                .AppendCallback(() => expSlider.value = 0f)
                .Append(expSlider.DOValue(target, fillDuration))
                .SetLink(gameObject);
            return;
        }

        fillTween = expSlider.DOValue(target, fillDuration).SetLink(gameObject);
    }

    private void DrawOdds(int level)
    {
        int[] weights = shop.GetLevelSetting(level).RarityWeights;

        int total = 0;
        for (int i = 0; i < weights.Length; i++) total += weights[i];

        for (int i = 0; i < rarityTexts.Length; i++)
        {
            if (rarityTexts[i] == null) continue;

            int w = i < weights.Length ? weights[i] : 0;
            int percent = total > 0 ? Mathf.RoundToInt(w * 100f / total) : 0;
            rarityTexts[i].text = $"{percent}%";
        }
    }
    #endregion
}
