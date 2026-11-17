using UnityEngine;


//매니저들을 통합 관리합니다.
public class InGameManagers : MonoBehaviour
{
    public static InGameManagers Instance { get; private set; } = null;


    private static PlayerStatManager playerStatManager;
    private static BattleManager battleManager;
    private static PoolingSystem poolingSystem;
    private static InGameUIManager inGameUIManager;
    private static RunContext runContext;
    private static BoardModel board;

    public static PlayerStatManager PlayerStat => playerStatManager;
    public static BattleManager Battle => battleManager;
    public static PoolingSystem Pool => poolingSystem;
    public static InGameUIManager UI => inGameUIManager;
    public static RunContext Run => runContext;
    public static BoardModel Board => board;
        
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Initialize();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    
    
    //매니저들 초기화
    private void Initialize()
    {
        playerStatManager = GetComponentInChildren<PlayerStatManager>();
        playerStatManager.Initialize();
        inGameUIManager = GetComponentInChildren<InGameUIManager>();
        inGameUIManager.Initialize();
    }


    public static void InitBattle()
    {
        battleManager?.Release();
        battleManager = new BattleManager();

        // Unity 오브젝트는 ?. 대신 == null 로 검사 (파괴된 오브젝트도 null로 판정되도록)
        if (poolingSystem == null)
        {
            // 풀 루트는 로컬 기본값(원점/회전 없음/스케일 1)으로 붙임.
            // InGameManagers 자체의 Transform도 기본값이어야 자식 오브젝트 위치가 틀어지지 않음
            
            var go = new GameObject("[Pool]");
            go.transform.SetParent(Instance.transform, false);
            poolingSystem = go.AddComponent<PoolingSystem>();
        }
    }

    public static void SetRun(RunContext run) => runContext = run;
    public static void SetBoard(BoardModel value) => board = value;
    
    public static void ReleaseBattle()
    {
        battleManager?.Release();
        battleManager = null;

        if (poolingSystem != null) poolingSystem.ReturnAll();  // 파괴 대신 회수 → 다음 전투 재사용
    }
    
    private void OnDestroy()
    {
        if (Instance != this) return;
        
        // 풀 오브젝트는 [Pool]이 이 오브젝트의 자식이라 씬과 함께 자동 파괴됨 → Clear 불필요
        // static 필드가 파괴된 오브젝트를 계속 가리키지 않도록 정리만 함
        battleManager?.Release();
        inGameUIManager.Release();
        inGameUIManager = null;
        battleManager = null;
        poolingSystem = null;
        playerStatManager = null;
        Instance = null;
    }
}
