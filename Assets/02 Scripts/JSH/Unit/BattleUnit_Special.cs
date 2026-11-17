/// <summary>
/// 스페셜 스킬이 필요로 하는 BattleUnit 조작만 모은 partial입니다.
///
/// 본체(BattleUnit.cs)에 섞지 않은 이유는 두 가지입니다.
///  - 전투 루프가 쓰는 API와 스킬이 쓰는 API를 파일 단위로 갈라두면 각각 읽기 쉽습니다.
///  - 스페셜이 늘어날 때 어디가 늘어나는지 한눈에 보입니다.
///
/// ★ BattleUnit.cs의 선언을 'public partial class BattleUnit' 으로 한 단어만 바꿔주세요.
/// </summary>
public partial class BattleUnit
{
    #region 사망

    /// <summary>
    /// 체력과 무관하게 즉시 사망 처리합니다. 자폭(SP_0003)과 처형(SP_0010)이 씁니다.
    ///
    /// 평소 사망은 BattleManager.Tick의 CheckDeath가 잡지만,
    /// 이 경로는 HP를 깎는 것이 아니라 사망 그 자체가 목적이라 직접 닫습니다.
    /// Die()가 OnDeath 트리거를 올리므로 유산 스킬이 이어서 발동합니다.
    /// </summary>
    public void Kill()
    {
        if (isDead) return;
 
        hp.Clear();
        Die();

        isDead = !IsAlive;
    }

    #endregion

    #region SG

    /// <summary>
    /// SG를 최대치의 percent%만큼 채웁니다.
    ///
    /// 게이지가 가득 차도 여기서 스킬을 발동시키지는 않습니다.
    /// OnCast 판정은 TickAttack 한 곳에만 두어, 스킬이 스킬을 부르는 즉시 연쇄를 막고
    /// 발동 시점을 공격 타이밍에 고정합니다(리플레이 재현성).
    /// 결과적으로 충전된 스킬은 다음 일반 공격에서 나갑니다.
    /// </summary>
    public void RefillSG(float percent)
    {
        if(percent <= 0 || !IsAlive) return;
        sg.Modify(Stat.GetStat(EStatType.SG) * percent * 0.01f);
    }

    /// <summary>SG 게이지를 최대치의 percent%만큼 깎습니다. RefillSG의 짝입니다.</summary>
    public void ReduceSG(float percent)
    {
        if (percent <= 0f || !IsAlive) return;
        
        sg.Modify(-Stat.GetStat(EStatType.SG) * percent * 0.01f);
    }

    #endregion

    #region 상태이상 조회 / 해제

    //상태이상이 걸려 있는지
    public bool HasStatus(StatusEffectData data)
        => unitStatContainer.Find(data) != null;

    //해당 상태이상의 현재 스택
    public int GetStatusStack(StatusEffectData data)
        => unitStatContainer.GetStack(data);

    //해당 상태이상 즉시 해제
    public void RemoveStatus(StatusEffectData data)
        => unitStatContainer.Remove(data);

    public void ClearDebuffs() => unitStatContainer.ClearDebuffs();

    #endregion

    #region 스킬 교체

    private Skill originalSkill;

    /// <summary>
    /// 이번 전투 동안만 스킬을 바꿉니다. 스킬 카피가 씁니다.
    /// 라운드가 끝나면 ResetForNext가 원래 스킬로 되돌립니다.
    /// 실패하면 null을 돌려주고 기존 스킬을 그대로 둡니다.
    /// </summary>
    public Skill ReplaceSkillForBattle(string skillId)
    {
        Skill copied = new Skill();
        if (!copied.Init(skillId, this)) return null;

        originalSkill ??= skill;
        skill = copied;
        return copied;
    }

    private void RestoreOriginalSkill()
    {
        if (originalSkill == null) return;

        skill = originalSkill;
        originalSkill = null;
    }

    #endregion
}
