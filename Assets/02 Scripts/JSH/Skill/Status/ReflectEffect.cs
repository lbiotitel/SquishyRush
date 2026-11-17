/// <summary>
/// 받은 피해의 일부를 공격자에게 되돌립니다. 가시.
///
/// 스택은 배수가 아니라 남은 반사 횟수입니다. 반사할 때마다 1 소모합니다.
/// 반사량은 항상 원본 피해의 ModifierValue% 입니다.
/// 실드가 얼마나 막았든 원본 기준인 이유는, 실드 두꺼운 탱커가 가시를 들면
/// 반사가 0에 수렴해 버리기 때문입니다(화분돌이가 실드와 가시를 같이 줍니다).
/// </summary>
public class ReflectEffect : StatusEffect
{
    public override bool IsConsumed => Stack <= 0;
    
    //반사량 돌려주고 횟수 1 소모
    public float Consume(float originDamage)
    {
        if (Stack <= 0 || originDamage <= 0f) return 0f;

        float reflect = originDamage * RawValue() * 0.01f;
        SetStack(Stack - 1);

        return reflect;
    }
}
