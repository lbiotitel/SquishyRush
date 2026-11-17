using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


//스킬 컴포넌트 뷰에 대한 정의 입니다.
public class SkillViewComponent : MonoBehaviour, IPointerClickHandler
{
    [Header("Image")] [SerializeField] private Image skillImage;
    
    private readonly SkillToolTipUIData skillToolTipUIData = new  SkillToolTipUIData();
    private SkillData currentSkillData;
    private UnitStat currentStat;
    private int currentStar = -1;
    
    public void Initialize(string skillId, UnitStat stat = null, int star = -1)
    {
        if (!GlobalManagers.Resource.TryGetResource<SkillData>(skillId, out SkillData skillData))
        {
            Logger.LogError($"{skillId}에 해당하는 스킬 데이터가 없습니다.");
            return;
        }
        
        currentSkillData = skillData;
        currentStat = stat;
        currentStar = star;
        skillImage.sprite = currentSkillData.SkillIcon;
    }



    public void OnPointerClick(PointerEventData eventData)
    {
        OpenToolTip();
    }
    

    private void OpenToolTip()
    {
        skillToolTipUIData.OpenPos = transform.position;
        skillToolTipUIData.Skill = currentSkillData;
        skillToolTipUIData.Stat = currentStat;
        skillToolTipUIData.Star = currentStar;

        GlobalManagers.UI.PopUpContainer.OpenUI<SkillToolTipUI>(skillToolTipUIData);
    }
    
}
