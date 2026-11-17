using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Spine.Unity;
using UnityEngine;

public class InGameController : MonoBehaviour, IPerkEquipActions, IShopActions, IBoardActions, IPrepActions
{
    [Header("Versus UI")]
    [SerializeField] private VersusUI versusUI;
     
    [Header("Button")]
    [SerializeField] private AnimButton settingButton;
    [SerializeField] private AnimButton speedUpButton;
    [SerializeField] private SpeedButtonView speedView;
    [SerializeField] private AnimButton manualButton;
    
    [Header("구역 크기 설정")] 
    [SerializeField] private int battleRows = 2;
    [SerializeField] private int battleCols = 3;
    [SerializeField] private int benchCapacity = 4;
    
    [Header("시작 설정")]
    [SerializeField] private int startCurrency = 2;
    [SerializeField] private int startLife = 3;
    [SerializeField] private float battleReadyDelay = 1f;

    [Header("스테이지")] 
    [SerializeField] private Transform stageMapRoot;
    
    [Header("뷰")]
    [SerializeField] private ShopPanel shopPanel;
    [SerializeField] private PrepControlUI prepControlUI;
    [SerializeField] private BoardView boardView;
    [SerializeField] private InGameHudUI hudUI;
    [SerializeField] private VfxSystem vfxSystem;
    [SerializeField] private InGameUnitDetailedUI unitDetailedUI;
    [SerializeField] private SynergyListUI synergyListUI;
    [SerializeField] private GameStatusNotifierUI gameStatusNotifier;
    [SerializeField] private float battleResultDelay = 1.2f;
    [SerializeField] private BattleMeterDrawer meterDrawer;
    
    [Header("재능 구매 연출")] 
    [SerializeField] private PerkShopSystem perkShopSystem;
    [SerializeField] private VisualEffectData perkPurchaseVfx;
    
    [Header("테스트")] 
    [SerializeField] private List<UnitDataEntry> testUnits = new();
    [SerializeField] private List<ArtifactDataEntry> testArtifacts = new();
    [SerializeField] private EStageType testStageType = EStageType.Forest;
    [SerializeField] private int testShopLevel = 0;
    [SerializeField] private int testStartCurrency = 1000000000;


    [SerializeField] private InputController inputController;

    private ProgressService progressService;
    private bool showingPerkShop;
    private bool speedUp = false;
    private RunContext runContext;
    private BoardModel board;
    private ShopSystem shop;
    private InGamePhaseMachine phaseMachine;
    private MergeFlow mergeFlow;
    private SynergyBonusFlow synergyBonusFlow;
    private bool isEliteBonusBusy;
    private PerkInventory perkInventory;
    private PerkDeliveryFlow perkDeliveryFlow;
    // 판매 시 해제된 재능을 담는 버퍼. 판매할 때마다 new 하지 않도록 재사용합니다.
    private readonly List<PerkDataBase> perkReturnBuffer = new();

    
    private readonly RewardSystem rewardSystem = new();
    private readonly SynergyResolver synergyResolver = new();
    private readonly SynergySystem allySynergy = new();
    private readonly ArtifactSystem artifactSystem = new();
    private readonly StageManager stageManager = new();

    private UniTask prewarmTask;
    
    public RunContext Context => runContext;
    public BoardModel Board => board;
    public ShopSystem Shop => shop;
    public float BattleReadyDelay => battleReadyDelay;
    public EInGamePhase CurrentPhase => phaseMachine?.CurrentType ?? EInGamePhase.None;
    public ShopPanel ShopPanel => shopPanel;
    public PrepControlUI PrepControlUI => prepControlUI;
    public GameStatusNotifierUI GameStatusNotifier => gameStatusNotifier;
    public float BattleResultDelay => battleResultDelay;
    public BoardView BoardView => boardView;
    public VfxSystem VfxSystem => vfxSystem;
    public SynergyResolver SynergyResolver => synergyResolver;
    public SynergySystem AllySynergy => allySynergy;
    public StageManager StageManager => stageManager;
    public PerkShopSystem PerkShop => perkShopSystem;
    public BattleMeterDrawer MeterDrawer => meterDrawer;
    public PerkDeliveryFlow PerkDelivery => perkDeliveryFlow;
    
    /// <summary>
    /// [미터기] 마지막 전투의 결과표. 상점 페이즈 동안 서랍에 보여줍니다.
    /// 첫 라운드 전에는 null이라 서랍이 나오지 않습니다.
    /// 패배로 같은 라운드를 다시 해도 직전 전투 결과가 그대로 보입니다.
    /// </summary>
    public MeterSnapshot LastMeter { get; private set; }

    /// <summary>[미터기] BattlePhase.EndBattle이 전투 결과표를 넘겨줄 때 부릅니다.</summary>
    public void SetLastMeter(MeterSnapshot snapshot) => LastMeter = snapshot;
    
    public ShopPhase ShopPhaseState { get;  private set; }
    public BattlePhase BattlePhaseState { get;  private set; }

    public BoardModel NextEnemyBoard { get; private set; }

    public bool IsPerkShopRound => stageManager.IsPerkShopRound(runContext.RoundIdx);
    public bool IsEliteRound => stageManager.GetRow(runContext.RoundIdx)?.RoundType == ERoundType.EliteMonster;
  
    /// <summary>
    /// 엘리트 보상 시너지 선택이 진행 중인지 여부입니다.
    /// </summary>
    public bool IsEliteBonusBusy => isEliteBonusBusy;
    private SettingService settingService;

    /// <summary>
    /// 합성 흐름(재능 선택 포함)이 진행 중인지 여부입니다.
    /// </summary>
    public bool IsMergeBusy => mergeFlow != null && mergeFlow.IsBusy;

    
    /// <summary>
    /// 상점 조작이 가능한 상태인지 여부입니다.
    /// 상점 페이즈이고, 합성 흐름이 진행 중이 아니어야 합니다.
    /// </summary>
    private bool CanShopAction => CurrentPhase == EInGamePhase.Shop && !IsMergeBusy && !isEliteBonusBusy;
    
    private bool UseTest
    {
        get
        {

#if UNITY_EDITOR
            return !UnityEditor.EditorPrefs.GetBool("AutoStartBootStrap_Enabled", true);
#else
            return false;
#endif
        }
    }


    private void Awake()
    {
        versusUI.OnCovered += OnVersusCovered;
        versusUI.OnAnimationEnd += OnVersusEnd;
        versusUI.ShowCover();
    }

    private void Start()
    {
        if (GlobalManagers.Resource.IsLoaded)
        {
            StartIntro();
            return;
        }
        
        GlobalManagers.Event.AddEvent<OnGameResourceDataLoadedEventData>(OnGameResourceDataLoaded);
    }

    private void OnGameResourceDataLoaded(OnGameResourceDataLoadedEventData e)
    {
        if (e.Key != nameof(EScene.InGame)) return;
        
        GlobalManagers.Event.RemoveEvent<OnGameResourceDataLoadedEventData>(OnGameResourceDataLoaded);
        StartIntro();
    }

    private void StartIntro() => StartIntroAsync(this.GetCancellationTokenOnDestroy()).Forget();

    private async UniTaskVoid StartIntroAsync(CancellationToken ct)
    {
        if (!InitStage()) return;
        
        StageLineup lineup = stageManager.BulidLineup(versusUI.SlotCount);
        
        var lineupIds = new List<string>();
        foreach (UnitData u in lineup.Visible) lineupIds.Add(u.UnitId);
        if(lineup.Hidden != null) lineupIds.Add(lineup.Hidden.UnitId);

        await PrewarmUnitsAsync(lineupIds, ct);
        
        ProfileService profile = UseTest ? null : GlobalManagers.Service.GetService<ProfileService>();
        versusUI.Bind(profile, stageManager.CurrentStage, lineup);
        versusUI.Play();
        
        prewarmTask = PrewarmSkeletonAsync(ct);
    }

    private async UniTask PrewarmUnitsAsync(IEnumerable<string> unitIds, CancellationToken ct)
    {
        foreach (string id in unitIds)
        {
            if (GlobalManagers.Resource.TryGetResource(id, out UnitData data) &&
                GlobalManagers.Resource.TryGetResource(data.UnitSpine, out SkeletonDataAsset sda))
                sda.GetSkeletonData(true);

            await UniTask.Yield(PlayerLoopTiming.Update, ct);
        }
    }

    private async UniTask PrewarmSkeletonAsync(CancellationToken ct)
    {
        var unitIds = new HashSet<string>();

        foreach (UnitDataEntry e in TakeOwnedUnitsSnapshot())
            unitIds.Add(e.UnitId);

        InGameStageRowData firstRow = stageManager.GetRow(1);
        if(firstRow != null)
            foreach (InGameStageRowUnitData u in firstRow.UnitRowDatas)
                unitIds.Add(u.UnitId);
        
        await PrewarmUnitsAsync(unitIds, ct);
    }

    private void OnVersusCovered()
    {
        versusUI.OnCovered -= OnVersusCovered;
        FinishInitAsync().Forget();
    }

    private async UniTaskVoid FinishInitAsync()
    {
        CancellationToken ct = this.GetCancellationTokenOnDestroy();
        
        await prewarmTask;
        Init();
        await UniTask.DelayFrame(2, cancellationToken: ct);
        await versusUI.ResumeAsync(ct);
    }
    
    private void OnVersusEnd()
    {
        versusUI.OnAnimationEnd -= OnVersusEnd;
        CheckTutorial();
    }

    private void CheckTutorial()
    {
        progressService = GlobalManagers.Service.GetService<ProgressService>();
        
        if (progressService?.IsClearInGameTutorial == false)
        {
            progressService?.ClearInGameTutorial();
            InGameManagers.UI.PopUpContainer.OpenUI<InGameTutorialUI>();
        }
    }
    
    

    private void Init()
    {
        GlobalManagers.Audio.PlayBgm(GlobalDefines.ResourceKey.Audio.INGAME_BGM, true);
        
        BindEvent();
        
        InitArtifacts();
        InitRun();
        ApplyStartBonus();
        InitBattle();
        InitPerkSystem();
        InitPhase();
        InitRewardSystem();
    }
    
    
    
    private void BindEvent()
    {
        settingButton.OnClicked += OnClickedSettingButton;
        speedUpButton.OnClicked += OnClickedSpeedButton;
        manualButton.OnClicked += OnClickedManualButton;
    }

    private void OnClickedSettingButton()
    {
        GlobalManagers.Audio.PlaySfx(GlobalDefines.ResourceKey.Audio.BUTTON_CLICK_SFX);
        InGameManagers.UI.PopUpContainer.OpenUI<InGameSettingUI>();
    }

    private void OnClickedSpeedButton()
    {
        GlobalManagers.Audio.PlaySfx(GlobalDefines.ResourceKey.Audio.BUTTON_CLICK_SFX);
        ChangeGameSpeed(!speedUp);
    }

    private void OnClickedManualButton()
    {
        GlobalManagers.Audio.PlaySfx(GlobalDefines.ResourceKey.Audio.BUTTON_CLICK_SFX);
        InGameManagers.UI.PopUpContainer.OpenUI<InGameTutorialUI>();
    }

    private void ChangeGameSpeed(bool onOff)
    {
        speedUp = onOff;
        settingService?.SetSpeedUp(onOff);
        Time.timeScale = 
            onOff ? GlobalDefines.GameConstants.FAST_GAME_SPEED : GlobalDefines.GameConstants.NORMAL_GAME_SPEED;
        
        speedView.SetOn(onOff);
    }


    /// <summary>
    /// 로비에서 선택한 스테이지의 데이터와 맵을 준비합니다.
    /// </summary>
    private bool InitStage()
    {
        EStageType stageType = UseTest ? testStageType : GameDataPack.StageType;
        return stageManager.Initialize(stageType, stageMapRoot);
    }

    /// <summary>
    /// 재능 시스템을 초기화 합니다.
    /// </summary>
    private void InitPerkSystem()
    {
        perkDeliveryFlow = new PerkDeliveryFlow(perkInventory, shopPanel, boardView, vfxSystem,
            perkPurchaseVfx, perkShopSystem);
        
        perkShopSystem.Initialize(runContext, inputController);
    }


    private void InitRewardSystem()
    {
        rewardSystem.Initialize();
    }

    /// <summary>
    /// 유저가 가진 아티팩트 데이터들을 초기화 합니다.
    /// </summary>
    private void InitArtifacts()
    {
        //여기서 받은 데이터 하기
        if (UseTest)
        {
            //테스트 데이터 주입
            artifactSystem.Initialize(testArtifacts);
        }
        else
        {
            //로비에서 받은 데이터 주입
            IReadOnlyList<ArtifactDataEntry> artifacts = GameDataPack.ArtifactDatas;
            artifactSystem.Initialize(artifacts);
        }
    }


    private void InitRun()
    {
        int currency = UseTest ? testStartCurrency : startCurrency;
        runContext = new RunContext();
        runContext.Init(currency, startLife, stageManager.FinalRound, TakeOwnedUnitsSnapshot());
        InGameManagers.SetRun(runContext);
        
        if(UseTest && testShopLevel > 0) runContext.SetShopLevel(testShopLevel);
        board = new BoardModel(battleRows, battleCols, benchCapacity);
        InGameManagers.SetBoard(board);
        synergyBonusFlow = new SynergyBonusFlow(board, synergyResolver, allySynergy);
        mergeFlow = new MergeFlow(board, synergyBonusFlow);
        perkInventory = new PerkInventory();
        
        // 합성, 럭키7 교체로 튕긴 재능을 인벤토리로 보냅니다.
        board.OnPerksReturned += perkInventory.AddRange;

        shop = new ShopSystem(InGameBalance.GetShopLevelSetting);
    
        
        //게임 시작시 스피드 onoff데이터 가지고와 세팅
        settingService = GlobalManagers.Service.GetService<SettingService>();
        bool onOff = settingService?.SpeedUp ?? true;
        ChangeGameSpeed(onOff);
        
        gameStatusNotifier.Hide();
        boardView.Init(board,this);
        boardView.OnUnitSelected += unitDetailedUI.Show;
        boardView.OnUnitDeselected += unitDetailedUI.Hide;
        shopPanel.OnUnitPreviewOpened += unitDetailedUI.Show;
        shopPanel.OnUnitPreviewClosed += unitDetailedUI.Hide;
        boardView.OnUnitDragChanged += shopPanel.SetDragHidden;
        meterDrawer.Init();
        boardView.OnUnitDragChanged += meterDrawer.SetDragHidden;
        hudUI.Init(runContext, stageManager.CurrentStage);
        shopPanel.Init(shop, inputController ,runContext, board, this, perkInventory, this);
        prepControlUI.Init(this);
        synergyResolver.Init(GlobalManagers.Resource.GetResources<SynergyData>());
        allySynergy.Init(synergyResolver);
        synergyListUI.Init(allySynergy);
        OnBoardChangedForSynergy();
        
        board.OnBoardChanged += OnBoardChangedForSynergy;

    }

    private void InitBattle()
    {
        InGameManagers.InitBattle(); 
        
        vfxSystem?.Init(boardView);
        
        if (stageManager.EnvView != null)
            InGameManagers.Battle.SuddenDeath.OnStackChanged += stageManager.EnvView.SetStack;
    }

    private void InitPhase()
    {
        phaseMachine = new InGamePhaseMachine();
        
        ShopPhaseState = new ShopPhase(this, phaseMachine);
        BattlePhaseState = new BattlePhase(this, phaseMachine);

        phaseMachine.Init(ShopPhaseState);
    }
    
    private void ApplyStartBonus()
    {
        int firstGold = Mathf.FloorToInt(InGameManagers.PlayerStat.GetStatValue(EStatType.FirstGold));
        if (firstGold <= 0) return;
        
        runContext.AddGold(firstGold, applyMultiplier: false);
        //Logger.Log($"[Run] 아티팩트 시작 골드 +{firstGold} → {runContext.IGCurrency}");
    }

    private void Update()
    {
        if (phaseMachine == null) return;
        
        phaseMachine.Update(Time.deltaTime);
    }

    public void GiveEliteReward()
    {
        runContext.AddGold(InGameBalance.ELITE_REWARD_GOLD);

        RunElitBonusAsync(this.GetCancellationTokenOnDestroy()).Forget();
    }

    private async UniTaskVoid RunElitBonusAsync(CancellationToken ct)
    {
        isEliteBonusBusy = true;
        try
        {
            await synergyBonusFlow.RunAsync<EliteSynergyBonusUI>(ct);
        }
        catch (System.OperationCanceledException)
        {

        }
        finally
        {
            isEliteBonusBusy = false;
        }
    }

    public void GameOver(bool isClear)
    {
        boardView.SetInputEnabled(false);
        
        int displayRound = stageManager.CurrentStage.GetDisplayRound(Context.RoundIdx);

        int clearedRound = isClear ? displayRound : displayRound - 1;
        
        rewardSystem.GiveReward(clearedRound, isClear, GameDataPack.StageType).Forget();
        rewardSystem.OpenRewardUI();
    }
    
    /// <summary>
    /// 런 시작 시 사용할 유닛 풀을 1회 스냅샷합니다.
    /// 로비(유저) 데이터가 있으면 보유 유닛을, 없으면 테스트 유닛을 사용합니다.
    /// </summary>
    private List<UnitDataEntry> TakeOwnedUnitsSnapshot()
    {
        if (UseTest) return CreateTestUnitList();

        IReadOnlyList<UnitDataEntry> fromLobby = GameDataPack.UnitDatas;
        if (fromLobby != null && fromLobby.Count > 0) return new List<UnitDataEntry>(fromLobby);
    
        return CreateTestUnitList();
    }

    private List<UnitDataEntry> CreateTestUnitList()
    {
        List<UnitDataEntry> list = new List<UnitDataEntry>();
        
        foreach (UnitDataEntry entry in testUnits)
        {
            if (string.IsNullOrEmpty(entry.UnitId)) continue;
            list.Add(entry);
        }

        return list;
    }

    private void OnBoardChangedForSynergy()
    {
        allySynergy.Recount(board);
        /*Logger.Log($"[시너지] 카운트 {allySynergy.Counter.Dump()}");
        Logger.Log($"[시너지] 판정   {synergyResolver.Dump(allySynergy.Active)}");*/
    }

    // 다음 라운드 적 보드를 만들고 화면에 배치
    public void PrepareNextEnemy()
    {
        boardView.ClearEnemyBoard();

        int round = IsPerkShopRound ? runContext.RoundIdx + 1 : runContext.RoundIdx;
        
        NextEnemyBoard = stageManager.BuildEnemyBoard(runContext, round, battleRows, battleCols);
        boardView.ShowEnemyBoard(NextEnemyBoard);
    }
    
    // 전투가 끝나면 다 쓴 보드 참조를 끊음
    public void ClearNextEnemy() => NextEnemyBoard = null;

    #region IShopActions
    
    public EShopActionResult TryBuyUnit(int shopSlotIndex)
    {
        // 보너스 팝업을 숨긴 상태에서 구매 → 합성이 겹치는 걸 막습니다.
        if (!CanShopAction) return EShopActionResult.WrongPhase;
        return shop.TryBuy(shopSlotIndex, runContext, board);
    }

    public EShopActionResult TryReroll()
    {
        if (!CanShopAction) return EShopActionResult.WrongPhase;
        return shop.TryReroll(runContext);
    }

    #endregion

    #region IPrepActions

    public void StartBattle()
    {
        if (!CanShopAction) return;

        if (!board.HasBattleUnit() && board.DeployBenchToBattle() == 0)
        {
            //Logger.Log("[Prep] 출전 가능한 유닛이 없어 전투를 시작할 수 없습니다.");
            // TODO: 토스트 안내 (ToastMessageUI + 로컬라이즈 키)
            return;
        }

        EnterBattle();
    }

    // 재능상점 라운드면 한 칸 넘기고 전투로 진입합니다.
    private void EnterBattle()
    {
        if(IsPerkShopRound) runContext.NextRound();
        phaseMachine.ChangePhase(BattlePhaseState);
    }
    
#if UNITY_EDITOR
    #region 디버그

    /// <summary>
    /// 유닛 유무와 관계없이 전투로 넘깁니다. 치트 전용
    /// </summary>
    public void ForceEnterBattle()
    {
        if (!CanShopAction) return;
        EnterBattle();
    }

    #endregion
    
#endif
    /// <summary>
    /// 적/상품 전환 버튼
    /// </summary>
    public bool TogglePerkPreview()
    {
        if (CurrentPhase != EInGamePhase.Shop || !perkShopSystem.IsOpen) return showingPerkShop;

        SetPerkShopView(!showingPerkShop);
        return showingPerkShop;
    }

    #endregion

    #region 재능상점 라운드

    /// <summary>
    /// 재능상점 라운드 정비 시작
    /// 상품부터 보여주고 적은 숨깁니다. 적 뷰가 먼저 만들어져 있어야 숨길 수 있습니다.
    /// </summary>
    public void BeginPerkShop()
    {
        SetPerkShopView(true);
        perkShopSystem.OpenShop();

        if (!perkShopSystem.IsOpen)
        {
            SetPerkShopView(false);
            return;
        }
        
        prepControlUI.SetPerkShopMode(true);
    }

    /// <summary>
    /// 정비 종료(ShopPhase.Exit). 일반 라운드에서 불려도 안전합니다
    /// 전투에 들어가기 전에 상품을 닫고 적이 다시 보이게 합니다.
    /// </summary>
    public void EndPerkShop()
    {
        perkShopSystem.CloseShop();
        prepControlUI.SetPerkShopMode(false);
        SetPerkShopView(false);
    }

    /// <summary>
    /// 상품과 적은 같은 자리를 쓰므로 항상 하나만 보이게 합니다.
    /// 상품은 다시 뽑지 않고 보이기만 전환합니다(OpenShop/CloseShop을 쓰면 리롤/리셋됨).
    /// </summary>
    private void SetPerkShopView(bool showPerk)
    {
        showingPerkShop = showPerk;
        perkShopSystem.SetVisible(showPerk);
        boardView.SetEnemyBoardVisible(!showPerk);
    }
    
    #endregion

    #region IBoardActions

    public bool CanEditBoard => CanShopAction;
    
    public bool TryMoveUnit(SlotIndex from, SlotIndex to)
    {
        if (!CanShopAction) return false;
        return board.Move(from, to);
    }

    public EShopActionResult TrySellUnit(SlotIndex slot)
    {
        if (!CanShopAction) return EShopActionResult.WrongPhase;

        BattleUnit removed = board.Remove(slot);
        if (removed == null) return EShopActionResult.NoUnitInSlot;
     
        GlobalManagers.Audio.PlaySfx(GlobalDefines.ResourceKey.Audio.SELL_SFX);

        // 판매가를 재능 해제 전에 계산합니다.
        // 판매비용 2배는 스탯 배율이라, 재능을 먼저 해제하면 1배로 돌아간 값으로 계산됩니다.
        int sellPrice = InGameBalance.GetSellPrice(removed);
        
        // 해제한 재능 중 판매 소모 재능(판매비용 2배)만 빼고 인벤토리로 돌려줍니다.
        // 걸러내는 걸 ReleasePerks 안이 아니라 여기서 하는 이유: 럭키7 교체도 ReleasePerks를 쓰는데,
        // 그건 판매가 아니므로 판매비용 2배도 돌려받아야 하기 때문입니다
        perkReturnBuffer.Clear();
        removed.ReleasePerks(perkReturnBuffer);
        perkReturnBuffer.RemoveAll(p => p.ConsumeOnSell);
        perkInventory.AddRange(perkReturnBuffer);
        
        runContext.AddGold(sellPrice, applyMultiplier: false);
        return EShopActionResult.Success;
    }

    #endregion

    #region IPerkEquipActions
    
    public EPerkEquipResult TryEquipPerkAt(int inventoryIndex, Vector2 screenPos)
        => TryEquipPerk(inventoryIndex, boardView.TryPickAllyUnit(screenPos));

    private EPerkEquipResult TryEquipPerk(int inventoryIndex, BattleUnit unit)
    {
        if (!CanShopAction) return EPerkEquipResult.WrongPhase;
        if (unit == null) return EPerkEquipResult.NoUnit;

        PerkDataBase data = perkInventory.Get(inventoryIndex);
        EPerkEquipResult result = unit.CheckEquip(data);
        if (result != EPerkEquipResult.Success) return result;

        if (!unit.AddPerk(data))
        {
            Logger.LogError($"[Perk] {data.PerkId} 장착 실패 - 재능 생성에 실패했습니다.");
            return EPerkEquipResult.NoPerk;
        }

        perkInventory.RemoveAt(inventoryIndex);
        unit.RestoreHp();
        
        GlobalManagers.Audio.PlaySfx(GlobalDefines.ResourceKey.Audio.PERK_EQUIP_SFX);
        return EPerkEquipResult.Success;
    }

    #endregion
    
    private void OnDestroy()
    {
        GlobalManagers.Event?.RemoveEvent<OnGameResourceDataLoadedEventData>(OnGameResourceDataLoaded);
        if (board != null)
        {
            board.OnBoardChanged -= OnBoardChangedForSynergy;
            if (perkInventory != null) board.OnPerksReturned -= perkInventory.AddRange;
        }

        if (boardView != null && meterDrawer != null)
            boardView.OnUnitDragChanged -= meterDrawer.SetDragHidden;

        if (shopPanel != null && unitDetailedUI != null)
        {
            shopPanel.OnUnitPreviewOpened -= unitDetailedUI.Show;
            shopPanel.OnUnitPreviewClosed -= unitDetailedUI.Hide;
        }

        Time.timeScale = GlobalDefines.GameConstants.NORMAL_GAME_SPEED;
        
        mergeFlow?.Release();
        perkShopSystem?.Release();
        
        InGameManagers.SetRun(null);
        InGameManagers.SetBoard(null);
        
        settingButton.OnClicked -= OnClickedSettingButton;
        speedUpButton.OnClicked -= OnClickedSpeedButton;
        manualButton.OnClicked -= OnClickedManualButton;
        
        perkDeliveryFlow.Release();
        
        vfxSystem?.Release();
        stageManager.Release();
        InGameManagers.ReleaseBattle();

    }
}