using System.Collections.Generic;

/// <summary>
/// BattleUnit의 일반공격 연출/타이밍 부분.
/// </summary>
public partial class BattleUnit
{
    /// <summary>
    /// 일반공격 발사체의 비행 시간(초)
    ///
    /// 공격마다 Managers.Resource를 뒤지지 않도록 Init에서 한 번만 찾아 둡니다.
    /// 값의 출처는 VisualEffectData.TravelTime 하나뿐입니다.
    /// 로직(명중 예약)과 연출(발사체 이동)이 같은 숫자를 읽으므로 둘이 어긋날 수 없습니다.
    /// </summary>
    public float AttackTravelTime { get; private set; }

    private string attackVfxId;

    // SkillEffect.PublishVfx와 같은 방식. 발행마다 새로 만들지 않아 GC가 없습니다.
    // VfxSystem이 Targets를 받자마자 좌표로 바꾸고 보관하지 않으므로 재사용해도 안전합니다.
    private readonly OnVfxRequestedEventData attackVfxEvent = new();
    private readonly List<BattleUnit> attackVfxTargets = new(1);

    private void InitNormalAttack()
    {
        AttackTravelTime = 0f;
        attackVfxId = CurrentUnitData?.NormalAttackVfxId;

        if (string.IsNullOrEmpty(attackVfxId)) return;

        if (!GlobalManagers.Resource.TryGetResource(attackVfxId, out VisualEffectData data))
        {
            Logger.LogWarning($"[BattleUnit] {CurrentUnitData.UnitId}의 일반공격 연출 '{attackVfxId}'를 찾지 못했습니다. 즉시 명중으로 처리합니다.");
            attackVfxId = null;
            return;
        }

        // Impact(제자리) 연출이면 기다릴 이유가 없으므로 0으로 둡니다.
        // 무엇이 발사체인지는 VisualEffectPreset만 판단합니다(판단 규칙이 두 곳에 생기지 않게).
        if (VisualEffectPreset.IsProjectile(data.MoveType))
            AttackTravelTime = data.TravelTime;
        
        //Logger.Log($"[AtkVfx] Init {CurrentUnitData?.UnitId} id='{attackVfxId}' travel={AttackTravelTime}");
    }

    /// <summary>
    /// 일반공격 발사체 연출 요청. Miss여도 부릅니다(빗나가는 발사체도 날아가야 하므로).
    /// </summary>
    private void PublishAttackVfx(BattleUnit target)
    {
        if (string.IsNullOrEmpty(attackVfxId) || target == null) return;
        
        attackVfxTargets.Clear();
        attackVfxTargets.Add(target);

        attackVfxEvent.VisualEffectId = attackVfxId;
        attackVfxEvent.Caster = this;
        attackVfxEvent.Targets = attackVfxTargets;
        
        GlobalManagers.Event.NotifyEvent(attackVfxEvent);
        //Logger.Log($"[AtkVfx] Publish {CurrentUnitData?.UnitId} id='{attackVfxId}' target={target?.RuntimeId}");
    }

    /// <summary>
    /// 일반공격이 적중한 순간. NormalAttack.Apply가 부릅니다.
    ///
    /// unitStatContainer는 private이라 NormalAttack이 직접 만질 수 없습니다.
    /// 컨테이너를 public으로 여는 대신 필요한 동작 하나만 열어 둡니다.
    /// </summary>
    public void NotifyAttackHit(BattleUnit target)
        => unitStatContainer.NotifyOwnerAttacked(this, target);
}
