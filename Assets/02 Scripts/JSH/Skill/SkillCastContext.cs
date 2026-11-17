using System.Collections.Generic;

/// <summary>
/// 스킬 1회 발동 동안만 유효한 공유 정보입니다.
///
/// 왜 필요한가:
/// 사망 처리는 BattleManager.Tick의 CheckDeath에서 일어납니다. 즉 스킬이 대상을 치명타로 죽여도
/// 그 프레임 안에서는 Die()가 아직 안 돌았고, AreaResolver는 IsAlive가 false인 칸을 걸러냅니다.
/// 그래서 "이 스킬로 누굴 죽였는가"는 뒤따라오는 이펙트가 보드를 다시 뒤져서는 알 수 없습니다.
/// 앞선 이펙트가 남긴 기록을 읽는 수밖에 없고, 그 기록을 두는 자리가 여기입니다.
///
/// Skill이 인스턴스 하나를 들고 재사용합니다. 발동마다 새로 만들지 않아 GC가 없습니다.
/// </summary>
public class SkillCastContext
{
    private readonly List<BattleUnit> killed = new();
    
    //이번 발동의 시전자
    public BattleUnit Caster { get; private set; }
    
    /// <summary>이번 발동 중 치명상을 입은 대상들. Die()는 아직 안 돌았을 수 있습니다.</summary>
    public IReadOnlyList<BattleUnit> Killed => killed;

    public int KillCount => killed.Count;

    public bool HasKill => killed.Count > 0;

    /// <summary>Skill.TryCast가 이펙트 루프 직전에 부릅니다.</summary>
    public void Begin(BattleUnit caster)
    {
        Caster = caster;
        killed.Clear();
    }

    /// <summary>
    /// 피해를 준 이펙트가 대상이 쓰러졌음을 보고합니다.
    /// 같은 대상이 여러 이펙트에 맞아도 한 번만 기록됩니다.
    /// </summary>
    public void ReportKill(BattleUnit victim)
    {
        if (victim == null) return;
        if (killed.Contains(victim)) return;
        
        killed.Add(victim);
    }
}
