using System.Collections.Generic;


public class BattleManager
{
    private BoardModel ally;
    private BoardModel enemy;
    private System.Random rng;
    private List<BattleUnit> allUnits;

    public SkillTriggerSystem Trigger { get; private set; }
    public SuddenDeath SuddenDeath { get; } = new();

    /// <summary>
    /// 날아가는 중인 타격들(일반공격 + 발사체 스킬).
    /// 전투마다 새로 만들지 않고 Release에서 비웁니다. 매 라운드 할당을 피하려는 것입니다.
    /// </summary>
    public PendingHitQueue Hits { get; } = new();

    /// <summary>
    /// [미터기] 이번 전투의 피해·회복·실드 기록. Hits처럼 하나를 계속 쓰고 Init에서 비웁니다.
    /// Release에서는 비우지 않습니다. 전투가 끝난 직후 BattlePhase가 여기서 결과표를 복사해 가기 때문입니다.
    /// </summary>
    public BattleStatsRecorder Stats { get; } = new();
    public System.Random Rng => rng;
    
    public bool IsFinished => ally.IsAllDead() || enemy.IsAllDead();
    
    public void Init(BoardModel ally, BoardModel enemy, int seed)
    {
        Release();
        Stats.Reset();
        
        this.ally = ally;
        this.enemy = enemy;
        rng = new System.Random(seed);
        Trigger = new SkillTriggerSystem();
        
        allUnits = new List<BattleUnit>();
        allUnits.AddRange(ally.GetBattleUnits());
        allUnits.AddRange(enemy.GetBattleUnits());

        Trigger.Init(allUnits);
        
        for(int i =0; i < allUnits.Count; i++)
            if(allUnits[i].IsAlive) allUnits[i].EnterBattlePerks();
        
        SuddenDeath.Reset(allUnits);
    }

    /// <summary>
    /// 전투를 엽니다. Init 직후, 첫 Tick 전에 한 번 부릅니다.
    /// OnBattleStart 스킬이 여기서 나갑니다.
    /// </summary>
    public void StartBattle()
    {
        Trigger.RaiseBattleStart();
    }

    public void TickOpening(float dt)
    {
        for (int i = 0; i<allUnits.Count; i++) allUnits[i].ChargeAttack(dt);
    }

    private float debugTimer;
    public void Tick(float dt)
    {
        SuddenDeath.Tick(dt);
        
        for(int i = 0; i < allUnits.Count; i++) allUnits[i].TickStatus(dt);

        // 도착한 발사체 적용.
        // CheckDeath보다 앞에 두는 이유: 발사체로 쓰러진 유닛이 같은 프레임에 사망 처리되게.
        // TickAttack보다 앞에 두는 이유: 이번 프레임에 새로 쏜 타격은 이번 dt를 깎지 않아야
        // 연출(다음 프레임부터 elapsed 누적)과 같은 프레임에 도착합니다.
        Hits.Tick(dt);
        
        for(int i = 0; i < allUnits.Count; i++) allUnits[i].CheckDeath();
        for(int i = 0; i < allUnits.Count; i++) allUnits[i].TickAttack(dt);
        
        debugTimer += dt;
        if (debugTimer < 1f) return;
        debugTimer = 0f;

        for (int i = 0; i < allUnits.Count; i++)
        {
            BattleUnit u = allUnits[i];
            if (!u.IsAlive) continue;

            //Logger.Log($"[스냅] {u.RuntimeId} Atk={u.Stat.GetStat(EStatType.Atk):F3} | {u.DebugStatus()}");
        }
    }
    
    public BoardModel GetBoard(BattleUnit caster, ETargetType targetType, out bool boardIsAlly)
    {
        bool wantAlly = targetType == ETargetType.Team;
        boardIsAlly = caster.IsAlly == wantAlly;
        return boardIsAlly ? ally : enemy;
    }

    public void Release()
    {
        // 날아가던 타격 폐기. allUnits 검사보다 앞에 둬야 어떤 경로로 와도 비워집니다.
        // 안 비우면 마지막 적을 죽인 뒤 남은 발사체가 다음 라운드 유닛에게 들어갑니다.
        // (연출 쪽 발사체는 자기 TravelTime이 끝나면 알아서 사라지므로 따로 정리하지 않습니다.)
        Hits.Clear();
        SuddenDeath.Release();
        
        if (allUnits == null) return;

        for (int i = 0; i < allUnits.Count; i++)
        {
            if (allUnits[i].IsAlly) allUnits[i].ExitBattlePerks();
            else allUnits[i].Release();
        }

        allUnits.Clear();
    }
}
