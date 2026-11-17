// 말랑 돌격대 - 스킬 데이터 enum 정의
// 엑셀(말랑_돌격대_Skill_Data) Enum 시트와 1:1 대응합니다.
// 숫자를 명시한 이유: 값 순서가 바뀌면 이미 저장된 ScriptableObject의 직렬화 값이 통째로 어긋납니다.
// 새 값은 반드시 "뒤에 추가"만 하세요.

/// <summary>
/// 스킬이 발동되는 시점.
/// </summary>
public enum ETriggerType
{
    None = -1,
    OnBattleStart = 0,
    OnCast = 1,
    OnHit = 3,
    OnKill = 4,
    OnDeath = 5,
    OnAttack = 6,
    
    OnHeal = 10,
}

/// <summary>
/// 액티브 패시브 구분
/// </summary>
public enum ESkillType
{
    Active,
    Passive
}


/// <summary>
/// SkillEffect 한 행이 수행하는 주 행동.
/// </summary>
public enum EEffectType
{
    Damage = 0,
    Heal = 1,
    ApplyStatus = 2,
    Special = 3,
    StatModify = 4,
}

/// <summary>
/// 효과를 적용할 진영.
/// </summary>
public enum ETargetType
{
    Enemy = 0,
    Team = 1,
    Both = 2,
}

/// <summary>
/// 앵커(범위의 기준점)를 고르는 방식. AreaType이 All이면 무시됩니다.
/// </summary>
public enum ETargetSelectType
{
    Self = 0,
    Front = 1,
    Back = 2,
    Random = 3,
    LowestHp = 4,
    
}

/// <summary>
/// 앵커를 기준으로 한 범위 형태.
/// </summary>
public enum EAreaType
{
    S_Cross = 0,
    Side = 1,
    Single = 2,
    Column = 3,
    Row = 4,
    All = 5,
    Square = 6,
}

/// <summary>
/// 수치 계산 방식. 최종수치 = Value + (StatType 스탯 * ModifierValue / 100)
/// 런타임 스탯 합성 방식인 EStatModifireType(Add/PercentAdd/PercentMult)과는 다른 개념입니다.
/// 이쪽은 "기준 스탯을 쓸 것인가"만 정하고, 어떤 스탯인지는 별도의 StatType 컬럼이 정합니다.
/// </summary>
public enum EValueModifierType
{
    /// <summary>이 행에서는 수치를 쓰지 않음. ApplyStatus / Special 행이 사용.</summary>
    None = 0,
    /// <summary>기준스탯 없음. Value만 적용.</summary>
    Flat = 1,
    /// <summary>StatType 스탯의 %.</summary>
    StatPercent = 2,
}

/// <summary>
/// UI 색상 표기용 분류. 실제 동작은 EStatusEffectType이 결정합니다.
/// </summary>
public enum EStatusType
{
    Buff = 0,
    Debuff = 1,
}

/// <summary>
/// 상태이상이 실제로 무엇을 하는가.
/// </summary>
public enum EStatusEffectType
{
    StatModifier = 0,
    DamageOverTime = 1,
    HealOverTime = 2,
    Shield = 3,
    Control = 4,
    Immunity = 5,
    Reflect = 6,
    DamageReduction = 7,
    OnDamagedBonus = 8,
    Revive = 9,
    StackOnAttack = 10,
    ApplyStatusOnHit = 11,
    HealAmp = 12,
}

/// <summary>
/// 상태이상의 지속 방식.
/// </summary>
public enum EDurationType
{
    Time = 0,
    Battle = 1,
    Count = 2,
}

/// <summary>
/// 같은 상태가 다시 적용될 때의 처리.
/// </summary>
public enum EStackBehavior
{
    Add = 0,
    Refresh = 1,
    Replace = 2,
    Ignore = 3,
    
    /// <summary>
    /// 시전자별로 별개 인스턴스. 같은 시전자가 다시 걸면 남은 시간만 갱신(틱 타이머 유지). 재생용.
    /// MaxStack은 동시에 걸 수 있는 시전자 수이고, 넘치면 남은 시간이 가장 짧은 것을 밀어냅니다.
    /// </summary>
    IndependentRefresh = 4,
    
    /// <summary>
    /// 시전자별로 별개 인스턴스. 같은 시전자가 다시 걸면 무시. 출혈용.
    /// 한 시전자가 끊김 없이 유지하지 못하게 해서 시전자당 딜에 상한을 둡니다.
    /// MaxStack은 IndependentRefresh와 같습니다.
    /// </summary>
    IndependentIgnore = 5,
}

//데미지 타입
public enum EDamageSource
{
    Attack,
    Skill,
    DoT,
    Reflect,
    Environment,
}

public enum EStatusVfxMode
{
    None = 0,
    OnTick = 1,  // 화상·독·출혈·재생: 틱마다 한 번
    WhileActive = 2,   // 스턴·바인드·동상·감전·실명: 걸린 내내
    OnTrigger = 3,    // 부활: 발동 순간 한 번
}

public static class DamageSourceExtensions
{
    /// <summary>유닛이 '직접' 가한 피해인가. 감전·가시 판정의 기준입니다.</summary>
    public static bool IsDirect(this EDamageSource s)
        => s == EDamageSource.Attack || s == EDamageSource.Skill;
}

public static class EStackBehaviorExtensions
{
    //시전자별 인스턴스 방식인지
    public static bool IsIndependent(this EStackBehavior behavior)
        => behavior == EStackBehavior.IndependentRefresh || behavior == EStackBehavior.IndependentIgnore;
}