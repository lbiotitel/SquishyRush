using System.Collections.Generic;

/// <summary>
/// 시너지가 전투 시작에 한 번 실행하는 특수 효과.
///
/// SkillEffect를 상속하지 않는 이유는 시전자·캐스트 컨텍스트·성(星)을
/// 하나도 쓰지 않기 때문입니다. 상속하면 셋을 더미로 채워야 합니다.
/// 트리거형(부활, 처치 시 반응)은 이 인터페이스가 아니라 SkillTriggerSystem에 붙습니다.
/// </summary>
public interface ISynergySpecial
{
    void Apply(List<BattleUnit> targets, object source);
    void BindData(SpecialSkillData data);
}

public abstract class SynergySpecial<TData> : ISynergySpecial where TData : SpecialSkillData
{
    protected TData Data { get; private set; }

    public void BindData(SpecialSkillData data)
    {
        Data = data as TData;
        
        if(Data == null)
            Logger.LogError($"{GetType().Name}는 {typeof(TData).Name}를 기대하는데 " +
                            $"{(data == null ? "null" : data.GetType().Name)}이 들어왔습니다.");
    }
    
    public abstract void Apply(List<BattleUnit> targets, object source);
}
