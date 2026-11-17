using UnityEngine;

/// <summary>
/// 엑셀 SpecialSkill 시트 1행 = 이 애셋 1개. 임포터를 거치지 않고 손으로 만듭니다.
///
/// 스페셜 스킬은 필요한 수치의 개수도 의미도 전부 다르기 때문에,
/// Value / Value2 같은 공용 칸을 두지 않고 파생 SO가 자기 필드를 직접 선언합니다.
/// 인스펙터에 이름 붙은 필드로 뜨고, 타입도 float에 묶이지 않습니다.
///
/// 이 클래스는 '무엇을 하는가'를 전혀 모릅니다. 수치만 들고 있습니다.
/// ID를 실행 클래스로 바꾸는 일은 런타임 쪽(SpecialEffectRegistry)이 합니다.
/// 그래야 데이터 -> 런타임 방향의 역참조가 생기지 않습니다.
public abstract class SpecialSkillData : ScriptableObject
{
    [Header("식별")]
    [SerializeField]
    [Tooltip("SkillEffect 시트의 Special Skill ID와 같은 값. 실제 조회 키는 애셋 파일명입니다.")]
    private string specialSkillId;

    public string SpecialSkillId => specialSkillId;
    
}
