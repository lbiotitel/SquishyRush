/// <summary>
/// 수정자가 판단에 쓰는 재료.
///
/// 인자를 나열하지 않고 구조체로 묶은 이유는, 나중에 필요한 정보가 늘어도
/// (치명타였는가, 몇 번째 타격인가) 여기에 필드만 추가하면 기존 구현체를
/// 고치지 않아도 되기 때문입니다.
/// </summary>
public readonly struct DamageContext
{
    public readonly BattleUnit Attacker;
    public readonly BattleUnit Target;
    public readonly EDamageSource Source;

    public DamageContext(BattleUnit attacker, BattleUnit target, EDamageSource source)
    {
        Attacker = attacker;
        Target = target;
        Source = source;
    }
}
