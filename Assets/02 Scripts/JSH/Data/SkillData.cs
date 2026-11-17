using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 엑셀 Skill 시트 1행 = 이 애셋 1개.
/// EffectGroupID가 같은 SkillEffect 행들은 effects 리스트로 들어옵니다.
/// </summary>
[CreateAssetMenu(menuName = "Data/Skill")]
public class SkillData : ScriptableObject
{
    [Header("식별")]
    [SerializeField] private string skillID;
    [SerializeField] private string skillNameKey;
    [SerializeField] private string skillDescKey;
    
    [SerializeField] private string skillName;
    [SerializeField, TextArea] private string skillDesc;

    [SerializeField] private string skillIconKey;
    [SerializeField] private Sprite skillIcon;
    [SerializeField] private ESkillType skillType;
    
    [Header("발동")] 
    [SerializeField] private ETriggerType triggerType = ETriggerType.OnCast;
    [SerializeField] private int maxCastCount = -1;
    
    [Header("효과")]
    [SerializeField] private List<string> effectIds = new();
    
    public string SkillID => skillID;
    public string SkillNameKey => skillNameKey;
    public string SkillDescKey => skillDescKey;
    
    public string SkillName => skillName;
    public string SkillDesc => skillDesc;

    public string SkillIconKey => skillIconKey;
    public Sprite SkillIcon => skillIcon;
    public ESkillType SkillType => skillType;
    
    public ETriggerType TriggerType => triggerType;
    public int MaxCastCount => maxCastCount; //-1이면 제한 없음//
    
    public List<string> EffectIds => effectIds;

    public bool HasCastLimit => maxCastCount > 0;

#if UNITY_EDITOR
    /// <summary>엑셀 임포터 전용. 런타임에서 호출하지 마세요.</summary>
    public void Editor_SetFromImport(
        string skillID, string skillNameKey, string skillDescKey,
        string skillName, string skillDesc, string skillIconKey, Sprite skillIcon,
        ETriggerType triggerType, int maxCastCount,
        List<string> groupIds)
    {
        this.skillID = skillID;
        this.skillNameKey = skillNameKey;
        this.skillDescKey = skillDescKey;
        if (!string.IsNullOrEmpty(skillName)) this.skillName = skillName;
        if (!string.IsNullOrEmpty(skillDesc)) this.skillDesc = skillDesc;
        this.skillIconKey = skillIconKey;
        if (skillIcon != null) this.skillIcon = skillIcon;
        this.triggerType = triggerType;
        this.maxCastCount = maxCastCount;
        effectIds = groupIds;
    }
#endif
}

