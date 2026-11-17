using System.Collections.Generic;


public class Skill
{
    private SkillData skillData;
    private BattleUnit owner;
    private List<SkillEffect> effects;

    // 발동마다 재사용합니다. 새로 만들면 전투 중 GC가 생깁니다.
    private readonly SkillCastContext context = new();

    private int castCount;

    public ETriggerType TriggerType => skillData.TriggerType;
    public SkillData Data => skillData;
    
    public bool IsExhausted => skillData.HasCastLimit && castCount >= skillData.MaxCastCount;

    public bool Init(string skillId, BattleUnit owner)
    {
        if(!GlobalManagers.Resource.TryGetResource(skillId, out skillData))
        {
            Logger.LogWarning($"SkillData '{skillId}' not found.");
            return false;
        }

        this.owner = owner;
        castCount = 0;

        effects = new List<SkillEffect>(skillData.EffectIds.Count);
        foreach (string effectId in skillData.EffectIds)
        {
            SkillEffect effect = SkillEffect.Init(effectId);
            if (effect != null) effects.Add(effect);
            else Logger.LogWarning($"SkillEffect '{effectId}' 로드 실패 ({skillId})");
        }

        if (effects.Count == 0)
        {
            Logger.LogWarning($"'{skillId}'에 유효한 이펙트가 하나도 없습니다.");
            return false;
        }

        return true;

    }

    /// <summary>
    /// ignoreAlive는 OnDeath 전용입니다.
    /// 사망 스킬은 정의상 죽은 뒤에 발동하므로 생존 검사에 걸리면 안 됩니다.
    /// </summary>
    public void TryCast(bool ignoreAlive = false)
    {
        //if (castCount >= 1) return;
        if (IsExhausted) return;
        if (owner == null) return;
        if (!ignoreAlive && !owner.IsAlive) return;

        castCount++;

        // 이펙트들은 순서대로 돌면서 같은 컨텍스트를 읽고 씁니다.
        // 앞의 Damage가 처치를 기록하면 뒤의 Special이 그것을 봅니다.
        context.Begin(owner);
        
        for (int i = 0; i < effects.Count; i++)
            effects[i].Run(owner, context);
    }

    public void ResetForNext()
    {
        for(int i = 0; i < effects.Count; i++)
            effects[i].ResetForNext();
        castCount = 0;
    }
}
