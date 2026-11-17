using UnityEngine;

/// <summary>
/// SP_0012 (플레임) — 대상의 화상이 기준 스택 이상이면, 화상을 전부 걷어내고
/// 시전자 공격력의 hellfireAtkPercent% 만큼 지옥불 피해를 즉시 한 번 넣습니다.
///
/// 지옥불은 지속되지 않습니다. 터지고 끝나는 일회성 폭발입니다.
/// 그래서 StatusEffect 애셋으로 부여하지 않고 여기서 바로 피해를 냅니다.
/// 0초짜리 상태이상을 만들어 틱 한 번 돌리고 지우는 것보다 단순하고, 틱 타이밍에 흔들리지 않습니다.
///
/// 다만 '상태이상이 출처인 피해'라는 성질은 유지해야 합니다.
/// EDamageSource.DoT로 넣으면 DamageFormula가 StatusEffectDmg를 곱해줍니다.
/// (아이콘을 한 번 번쩍이고 싶다면 그건 BattleUnitView 쪽 연출로 처리할 일입니다.)
/// </summary>
[CreateAssetMenu(fileName = "SP_0012", menuName = "Data/Special/ConvertBurnToHellfire")]
public class ConvertBurnToHellfireSkillData : SpecialSkillData
{
    [Header("상태이상")]
    [Tooltip("소모할 상태이상 ID (화상)")]
    [SerializeField] private string burnStatusId;

    [SerializeField, Min(0f)] [Tooltip("시전자 공격력 대비 지옥불 피해 비율(%)")]
    private float hellfireAtkPercent = 500f;
    
    [Header("수치")] [Min(1)] [Tooltip("전환에 필요한 최소 화상 스택")]
    [SerializeField] private int requiredBurnStack = 5;

    [Tooltip("켜면 소모한 화상 스택이 기준의 몇 배인지에 비례해 피해가 커집니다.")]
    [SerializeField] private bool scaleByConsumedStack = false;
    
    public string BurnStatusId => burnStatusId;
    public float HellfireAtkPercent => hellfireAtkPercent;
    public int  RequiredBurnStack => requiredBurnStack;
    public bool ScaleByConsumedStack => scaleByConsumedStack;
}
