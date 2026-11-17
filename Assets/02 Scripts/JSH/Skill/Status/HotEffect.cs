//틱마다 회복시키는 상태이상
public class HotEffect : StatusEffect
{
    private float frozenAmount;

    public override void OnApply(BattleUnit owner)
    {
        if(!IsOwnerBased) frozenAmount = CalcAmount(owner);
    }

    public override void OnTick(BattleUnit owner)
    {
        if (!owner.IsAlive) return;
        if (owner.CurrentHp >= owner.MaxHp) return;
        
        float amount = (IsOwnerBased ? CalcAmount(owner) : frozenAmount) * Stack;
        if (amount <= 0f) return;
        
        owner.Heal(amount, Caster, Data);
        Logger.Log($"[재생] {owner.RuntimeId} <- {Data.StatusId} x{Stack} : {amount:F1} (남은 HP {owner.CurrentHp:F1})");
    }
}
