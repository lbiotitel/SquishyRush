/// <summary>
/// 획득 즉시 영구 적용되는 재능입니다. (스탯 증가 등)
/// 전투와 무관하게 보유하는 동안 항상 효과가 걸려 있습니다.
/// </summary>
public class InstantPerk : PerkBase
{
    /// <summary>
    /// 획득 즉시 효과를 적용합니다.
    /// </summary>
    public override void OnAcquired() => Execute();

    /// <summary>
    /// 스탯이 새로 만들어지면 지속 효과만 다시 겁니다. (골드는 다시 주지 않음)
    /// </summary>
    public override void OnStatRebuilt() => ExecutePersistent();

    /// <summary>
    /// 합성으로 물려받았을 때도 지속 효과만 새 주인에게 겁니다.
    /// </summary>
    public override void OnInherited() => ExecutePersistent();
    

    /// <summary>
    /// 재능을 잃을 때 적용했던 효과를 해제합니다.
    /// </summary>
    public override void OnRemoved() => Release();

}
