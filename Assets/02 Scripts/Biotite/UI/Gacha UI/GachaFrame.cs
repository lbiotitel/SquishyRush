using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


//가챠 UI에서 사용하는 프레임 UI입니다.
public class GachaFrame : MonoBehaviour
{
    [Header("Image")]
    [SerializeField] private Image unitImage;
    [SerializeField] private Image unitRairyBG;
    
    [Header("Text")]
    [SerializeField] private TMP_Text unitNameText;

    [Header("Effect")] 
    [SerializeField] private float lightEffectRotationSpeed;
    [SerializeField] private GameObject lightEffect;
    
    public bool IsInit { get; private set; } = false;
    public ERarity UnitRarity { get; private set; }

    public void Initialize(Sprite unitImage, string unitName, ERarity unitRarity)
    {
        HideLightEffect();
        
        IsInit = true;
        this.unitImage.sprite = unitImage;
        UnitRarity = unitRarity;
        unitNameText.text = unitName;
        unitRairyBG.color = GlobalDefines.Rarity.GetRarityPastelColor(unitRarity);
    }

    
    /// <summary>
    /// 레전드 연출 이팩트를 수행합니다.
    /// </summary>
    public void ShowLightEffect()
    {
        lightEffect.SetActive(true);
        lightEffect.transform.DOKill();                       
        lightEffect.transform.localScale = Vector3.zero;

        lightEffect.transform.DOScale(1f, 0.6f)
            .SetEase(Ease.OutBack)
            .SetLink(lightEffect)                           
            .OnComplete(() =>
            {
                lightEffect.transform.DORotate(new Vector3(0, 0, 360), 360f / lightEffectRotationSpeed, RotateMode.FastBeyond360)
                    .SetEase(Ease.Linear)
                    .SetLoops(-1, LoopType.Restart)
                    .SetLink(lightEffect);
            });
    }
    
    
    /// <summary>
    /// 빛연출을 숨김
    /// </summary>
    private void HideLightEffect()
    {
        lightEffect.transform.DOKill();
        lightEffect.SetActive(false);
    }

    public void OpenUI()
        => gameObject.SetActive(true);

    public void CloseUI()
    {
        HideLightEffect();
        IsInit = false;
        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (lightEffect != null) lightEffect.transform.DOKill();
    }
}
