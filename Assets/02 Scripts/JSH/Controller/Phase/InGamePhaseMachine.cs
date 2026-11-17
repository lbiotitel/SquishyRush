
public class InGamePhaseMachine
{
    public InGamePhaseBase CurrentPhase { get; private set; }
    public EInGamePhase CurrentType => CurrentPhase?.Type ?? EInGamePhase.None;

    public void Init(InGamePhaseBase phase)
    {
        CurrentPhase = phase;
        Logger.Log($"[Phase] 시작 -> {CurrentType}");
        CurrentPhase.Enter();
    }

    public void ChangePhase(InGamePhaseBase next)
    {
        if (next == null) return;
        if (CurrentPhase == next) return;
        
        Logger.Log($"[Phase] {CurrentType} -> {next.Type}");

        CurrentPhase?.Exit();
        CurrentPhase = next;
        CurrentPhase.Enter();
    }

    public void Update(float deltaTime) => CurrentPhase?.Update(deltaTime);
}
