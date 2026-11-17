using Cysharp.Threading.Tasks;
using DG.Tweening;
using System.Collections.Generic;
using BackEnd.Quobject.EngineIoClientDotNet.Modules;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public enum EResourceType
{
    Gold,
    Gem,
    Energy
}



// 유저가 가진 Resource를 표시합니다.
public class UserResourceUI : MonoBehaviour
{
    [Header("Text")]
    [SerializeField] private TMP_Text userGoldText;
    [SerializeField] private TMP_Text userGemText;
    [SerializeField] private TMP_Text userEnergyText;
    
    [Header("Anim Effect Property")]
    [SerializeField] private int effectCount = 10;
    [SerializeField] private float effectRandomRange = 200f;
    [SerializeField] private float effectAnimDuration = 0.2f;

    [Header("Button")]
    [SerializeField] private AnimButton energyButton;
    
    private Transform resourceMoverCanvas;
    private MemoryPool resourceMoverPool;


    //service
    private CurrencyService currencyService;
    private EnergyService energyService;

    

    private void Awake()
    {
        if (!GlobalManagers.Resource.TryGetResource<GameObject>(GlobalDefines.ResourceKey.UI.RESOURCE_MOVER_PREFAB,
                out GameObject prefab))
        {
            Logger.LogError($"{GlobalDefines.ResourceKey.UI.RESOURCE_MOVER_PREFAB}에 해당하는 프리팹이 없습니다.");
            return;
        }

        GameObject resourceMoverPrefab = prefab;
        resourceMoverPool = new MemoryPool(resourceMoverPrefab);
        
        resourceMoverCanvas = GlobalManagers.UI.PopUpCanvas;
        currencyService = GlobalManagers.Service.GetService<CurrencyService>();
        energyService = GlobalManagers.Service.GetService<EnergyService>();
        
        BindEvents();
        DrawUserResourceData();
    }

    
    private void BindEvents()
    {
        energyButton.OnClicked += OnClickedEnergyButton;
        GlobalManagers.Event?.AddEvent<OnUserResourceValueChangedEventData>(OnUserResourceValueChangedHandler);
    }

    
    private void OnDestroy()
    {
        energyButton.OnClicked -= OnClickedEnergyButton;
        resourceMoverPool.DestroyObjects();
        GlobalManagers.Event?.RemoveEvent<OnUserResourceValueChangedEventData>(OnUserResourceValueChangedHandler);
    }


    private void OnClickedEnergyButton()
        => GlobalManagers.UI.PopUpContainer.OpenUI<EnergyChargeUI>();


    
    
    /// <summary>
    /// 유저 재화가 변할때 호출되는 이벤트를 핸들링 합니다.
    /// </summary>
    /// <param name="e"></param>
    private void OnUserResourceValueChangedHandler(OnUserResourceValueChangedEventData e)
    {
        TMP_Text tagetText = GetTargetText(e.resourceType);
        if (e.showAnimEffect)
        {
            OnResourceValueChangedEffect(tagetText, e.spriteKey, e.prevValue, e.currentValue).Forget();
        }
        else
        {
            DrawUserResourceData();
        }
    }
    
    
    /// <summary>
    /// 현재 리소스 타입에 해당하는 text를 가져옵니다.
    /// </summary>
    /// <param name="resourceType"></param>
    /// <returns></returns>
    private TMP_Text GetTargetText(EResourceType resourceType)
    {
        switch (resourceType)
        {
            case EResourceType.Energy:
                return userEnergyText;
            case EResourceType.Gold:
                return userGoldText;
            case EResourceType.Gem:
                return userGemText;
        }

        return null;
    }
    
   
    /// <summary>
    /// 자원을 얻는 효과 연출을 실행합니다.
    /// </summary>
    /// <param name="targetText"></param>
    /// <param name="targetSpriteKey"></param>
    /// <param name="targetPrevAmount"></param>
    /// <param name="targetAmount"></param>
    private async UniTask OnResourceValueChangedEffect(TMP_Text targetText, string targetSpriteKey, int targetPrevAmount, int targetAmount)
    {
        if (!GlobalManagers.Resource.TryGetResource<Sprite>(targetSpriteKey, out Sprite sprite))
        {
            Logger.LogError($"{targetSpriteKey}에 해당하는 sprite가 없습니다.");
            return;
        }

        Sprite targetResourceSprite = sprite;

        if (targetPrevAmount < targetAmount)
        {
            List<Transform> resourceTransforms = new List<Transform>();
            Vector3 startCenterPos = new Vector3(Screen.width / 2f, Screen.height / 2f, 0f);
            Vector3 targetPos = targetText.transform.position;

            for (int i = 0; i < effectCount; i++)
            {
                GameObject go = resourceMoverPool.GetObject();
                go.transform.SetParent(resourceMoverCanvas.transform);
                go.GetComponent<Image>().sprite = targetResourceSprite;

                Vector3 randomOffset = new Vector3(
                    Random.Range(-effectRandomRange, effectRandomRange),
                    Random.Range(-effectRandomRange, effectRandomRange),
                    0f
                );

                go.transform.position = startCenterPos;
                go.transform.DOMove(startCenterPos + randomOffset, effectAnimDuration);
                resourceTransforms.Add(go.transform);
            }

            Sequence moveSequence = DOTween.Sequence();

            int targetTextAmount = targetPrevAmount;
            int increaseGap = (targetAmount - targetPrevAmount) / effectCount;

            foreach (var goldTr in resourceTransforms)
            {
                Vector3 startPos = goldTr.position;

                float delay = Random.Range(0f, 0.05f);

                Tween pathTween = goldTr.DOMove(targetPos, effectAnimDuration)
                    .SetEase(Ease.InQuad)
                    .SetDelay(delay)
                    .OnComplete(() =>
                    {
                        targetTextAmount += increaseGap;
                        targetText.text = targetTextAmount.ToString();
                        GlobalManagers.Audio.PlaySfx(GlobalDefines.ResourceKey.Audio.RESOURCE_GET_SFX);
                        resourceMoverPool.ReturnObject(goldTr.gameObject);
                    });


                moveSequence.Join(pathTween);
            }

            await moveSequence.AsyncWaitForCompletion();

        }

        DrawUserResourceData();
    }



    /// <summary>
    /// 유저가 가진 자원 수를 그립니다.
    /// </summary>
    private void DrawUserResourceData()
    {
        userGoldText.text = $"{currencyService.Gold}";
        userGemText.text = $"{currencyService.Gem}";
        userEnergyText.text =
            $"{energyService.UserEnergy}/{GlobalDefines.MAX_VALUE.MAX_REGEN_ENERGY}";
    }

}
