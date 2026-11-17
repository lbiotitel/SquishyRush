

public class SelfDestructPerk : PerkBase
{
    private TriggerHook castHook;
    private bool isActive;

    protected override void OnInitialized()
        => castHook = new TriggerHook(ETriggerType.OnCast, Owner, OnCast);

    public override void OnBattleEnter()
    {
        InGameManagers.Battle.Trigger.AddListener(castHook);
        
        isActive = true;
        Execute();
    }

    public override void OnBattleExit()
        => Deactivate();

    public override void OnRemoved()
        => Deactivate();

    private void OnCast()
    {
        Owner.Kill();
    }
        
    

    private void Deactivate()
    {
        InGameManagers.Battle?.Trigger?.RemoveListener(castHook);

        if (!isActive) return;
        isActive = false;
        Release();
    }
}
