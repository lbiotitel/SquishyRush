
public abstract class InGamePhaseBase
{
    protected readonly InGameController controller;
    protected readonly InGamePhaseMachine machine;
    
    
    public abstract EInGamePhase Type { get; }

    protected InGamePhaseBase(InGameController controller, InGamePhaseMachine machine)
    {
        this.controller = controller;
        this.machine = machine;
    }
    
    public virtual void Enter() {}
    public virtual void Update(float deltaTime) {}
    public virtual void Exit() {}
}
