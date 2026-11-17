using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class UnitStatusUI : MonoBehaviour
{
    [Header("HP")] 
    [SerializeField] private Slider hpBar;
    [SerializeField] private Image hpDelayFill;
    [SerializeField] private float delayDuration = 0.3f;

    [SerializeField] private Image hpBg;
    [SerializeField] private Image hpTopLight;
    [SerializeField] private Image hpBottomLight;
    [SerializeField] private Color allyHpColor  = new Color32(0x06, 0xFE, 0x07, 0xFF);
    [SerializeField] private Color allyHpLight  = new Color32(0x06, 0x69, 0x07, 0xFF);
    [SerializeField] private Color enemyHpColor = new Color32(0xFE, 0x07, 0x06, 0xFF);
    [SerializeField] private Color enemyHpLight = new Color32(0x69, 0x07, 0x06, 0xFF);
    
    [Header("SG")]
    [SerializeField] private Slider sgBar;

    [Header("Star")] 
    [SerializeField] private GameObject[] stars;

    [Header("Shield")] 
    [SerializeField] private GameObject shieldRoot;
    [SerializeField] private TMP_Text shieldText;

    [Header("상태이상")] 
    [SerializeField] private Transform statusContainer;
    [SerializeField] private List<StatusIconSlot> iconSlots = new();
    [SerializeField] private StatusIconSlot iconSlotPrefab;
    [SerializeField] private int maxIcons = 8;

    private readonly List<StatusIconInfo> statusBuffer = new();
    
    private BattleUnit unit;
    private Tween delayTween;
    private bool dirty;

    public void Bind(BattleUnit unit)
    {
        Unbind();
        this.unit = unit;
        ApplyTeamColor(unit.IsAlly);

        unit.OnHpChanged += RefreshHp;
        unit.OnSGChanged += RefreshSg;
        unit.OnStarChanged += RefreshStar;
        unit.OnStatusChanged += MarkDirty;
        unit.OnDamageTaken += MarkDirtyOnDamage;
        
        RefreshHp(unit.CurrentHp, unit.MaxHp);
        RefreshSg(unit.SGRatio, 1f);
        RefreshStar(unit.Star);
        
        delayTween?.Kill();
        hpDelayFill.fillAmount = hpBar.value;
        RefreshStatus();
    }

    private void ApplyTeamColor(bool isAlly)
    {
        hpBg.color = isAlly ? allyHpColor : enemyHpColor;

        Color light = isAlly ? allyHpLight : enemyHpLight;
        hpTopLight.color = light;
        hpBottomLight.color = light;
    }

    private void RefreshHp(float cur, float max)
    {
        float ratio = max > 0f ? Mathf.Clamp01(cur / max) : 0f;
        hpBar.value = ratio;
        
        delayTween?.Kill();

        if (hpDelayFill.fillAmount < ratio)
            hpDelayFill.fillAmount = ratio;
        else
            delayTween = hpDelayFill.DOFillAmount(ratio, delayDuration)
                .SetEase(Ease.OutQuad)
                .SetLink(gameObject);
    }

    private void RefreshSg(float cur, float max)
        => sgBar.value = max > 0f ? Mathf.Clamp01(cur / max) : 0f;

    private void RefreshStar(int star)
    {
        for (int i = 0; i < stars.Length; i++)
            stars[i].SetActive(i < star);
    }

    public void HideStars()
    {
        for(int i = 0; i < stars.Length; i++)
            stars[i].SetActive(false);
    }

    private void MarkDirty() => dirty = true;
    private void MarkDirtyOnDamage(DamageReport _) => dirty = true;

    public void SetBattleBarVisible(bool visible)
    {
        hpBar.gameObject.SetActive(visible);
        sgBar.gameObject.SetActive(visible);
    }
    
    private void LateUpdate()
    {
        if (!dirty) return;
        dirty = false;
        RefreshStatus();
    }

    private void RefreshStatus()
    {
        if (unit == null) return;

        float shield = unit.TotalShield;
        shieldRoot.SetActive(shield > 0f);
        if(shield > 0f) shieldText.SetText("{0:0}", shield);

        unit.GetVisibleStatusEffects(statusBuffer);
        
        int total = statusBuffer.Count;
        bool overflow = total > maxIcons;

        int iconCount = overflow ? maxIcons - 1 : total; // 넘치면 마지막 칸을 "+N"에 양보
        int slotCount = overflow ? maxIcons : total;
        
        while(iconSlots.Count < slotCount)
            iconSlots.Add(Instantiate(iconSlotPrefab, statusContainer));

        for (int i = 0; i < iconSlots.Count; i++)
        {
            if(i < iconCount) iconSlots[i].Show(statusBuffer[i].Data.Icon, statusBuffer[i].Stack);
            else if (overflow && i == iconCount) iconSlots[i].ShowMore(total - iconCount);
            else iconSlots[i].Hide();
        }
    }

    public void Unbind()
    {
        if (unit == null) return;
        unit.OnHpChanged -= RefreshHp;
        unit.OnSGChanged -= RefreshSg;
        unit.OnStarChanged -= RefreshStar;
        unit.OnStatusChanged -= MarkDirty;
        unit.OnDamageTaken -= MarkDirtyOnDamage;
        unit = null;
        delayTween?.Kill();
    }

    private void OnDestroy() => Unbind();
}
