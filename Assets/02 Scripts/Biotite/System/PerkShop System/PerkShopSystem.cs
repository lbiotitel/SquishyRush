using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Random = UnityEngine.Random;

//재능 상점 시스템에 관한 정의 입니다.
public class PerkShopSystem : MonoBehaviour
{
    [Header("Perk Shop Slot")]
    [SerializeField] private List<PerkShopSlot> perkShopSlots;

    [Header("Visibility")] 
    [SerializeField] private GameObject slotRoot;
    
    [Header("Camera")]
    [SerializeField] private Camera cam;
    private bool isOpen = false;

    private bool isVisible = true;

    public bool IsOpen => isOpen;

    // Constant (각 행의 합이 100이 되도록)
    private readonly Dictionary<int, int[]> PERK_RARITY_TABLE = new Dictionary<int, int[]>()
    {
        {2, new int[] { 32, 42, 22, 4 }},
        {3, new int[] { 20, 46, 26, 8 }},
    };

    private Dictionary<ERarity, List<PerkDataBase>> perkDict;
    private readonly List<PerkDataBase> currentDrawnPerks = new();
    
    private RunContext ctx;
    
    private BaseUI perkToolTipUI;

    private InputController inputController;

    public event Action<PerkDataBase, Vector3> OnPerkPurchased;

    public void Initialize(RunContext ctx, InputController inputController)
    {
        this.ctx = ctx;
        this.inputController = inputController;

        inputController.OnPressEvent += OnPressEventHandler;
        
        ParsingPerkData();

        for (int i = 0; i < perkShopSlots.Count; i++)
            perkShopSlots[i].Initialize(i);
        
        perkToolTipUI = InGameManagers.UI.PopUpContainer.OpenUI<PerkToolTipUI>();
        InGameManagers.UI.PopUpContainer.CloseUI(perkToolTipUI);

        SetVisible(false);
    }


    public void Release()
    {
        inputController.OnPressEvent -= OnPressEventHandler;
        HideTooltip();
    }
    
    /// <summary>
    /// 재능 상점을 엽니다. 재능을 뽑고 뽑은 데이터들을 숍 slot에 부여합니다.
    /// </summary>
    [ContextMenu("Open Shop")]
    public void OpenShop()
    {
        if (isOpen) return;
        if (!DrawPerkProcess()) return; 

        isOpen = true;
    }


    /// <summary>
    /// 상점을 닫습니다.
    /// </summary>
    [ContextMenu("Close Shop")]
    public void CloseShop()
    {
        if (!isOpen) return;
        isOpen = false;
        HideTooltip();
        foreach (var slot in perkShopSlots)
        {
            slot.Reset();
            slot.CloseShopSlot();
        }
    }

    private void OnPressEventHandler(EPressedType type, Vector2 pos)
    {
        if (!isOpen || !isVisible) return;   

        if (type == EPressedType.LongPress)
        {
            int index = GetSlotIndexAtPointer();
            OpenTooltip(index, pos);
        }

        if (type == EPressedType.ShortPress)
        {
            int index = GetSlotIndexAtPointer();
            OnClickSlot(index);        
        }

        if (type == EPressedType.PressUp)
        {
            HideTooltip();
        }
    }
    

    
    /// <summary>
    /// 포인터 위치에 있는 (판매 가능한) 슬롯의 인덱스를 반환합니다. 없으면 -1.
    /// </summary>
    private int GetSlotIndexAtPointer()
    {
        if (!TryGetPointerWorld(out Vector3 worldPos)) return -1;

        for (int i = 0; i < perkShopSlots.Count; i++)
        {
            var slot = perkShopSlots[i];
            if (slot.gameObject.activeSelf && !slot.IsSell && slot.Contains(worldPos))
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>
    /// 레이캐스트를 통해 worldpos위치를 가져옵니다. (z=0 평면)
    /// </summary>
    private bool TryGetPointerWorld(out Vector3 worldPos)
    {
        worldPos = default;
        if (cam == null) return false;

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        
        if (Mathf.Approximately(ray.direction.z, 0f)) return false;

        float t = -ray.origin.z / ray.direction.z;
        if (t <= 0f) return false;

        worldPos = ray.GetPoint(t);
        return true;
    }

    /// <summary>
    /// 슬롯이 탭 됐을 때(구매) 호출됩니다.
    /// </summary>
    private void OnClickSlot(int index)
    {
        if (!isOpen) return;
        
        if (index < 0 || index >= currentDrawnPerks.Count) return;

        PerkShopSlot slot = perkShopSlots[index];
        if (slot.IsSell) return;

        PerkDataBase perk = currentDrawnPerks[index];

        if (!ctx.TrySpendGold(perk.Price))
        {
            Logger.Log("골드가 부족합니다");
            return;
        }
        
        GlobalManagers.Audio.PlaySfx(GlobalDefines.ResourceKey.Audio.PURCHASE_SFX);
        
        OnPerkPurchased?.Invoke(perk, slot.IconPosition);

        Logger.Log($"[PerkShop] {perk.PerkId} 구매 ({perk.Price}G, 남은 골드 {ctx.IGCurrency})");
        slot.Sell();
    }

    
    /// <summary>
    /// 툴팁을 엽니다.
    /// </summary>
    /// <param name="index"></param>
    /// <param name="pos"></param>
    private void OpenTooltip(int index, Vector3 pos)
    {
        if (index < 0 || index >= currentDrawnPerks.Count) return;

        PerkDataBase perk = currentDrawnPerks[index];
        
        InGameManagers.UI.PopUpContainer.OpenUI<PerkToolTipUI>(PerkToolTipUIData.Create(perk, pos));
    }
    
    
    /// <summary>
    /// 툴팁을 숨깁니다.
    /// </summary>
    private void HideTooltip()
    {
        if (perkToolTipUI == null) return;

        InGameUIManager ui = InGameManagers.UI;
        if (ui == null || ui.PopUpContainer == null) return;
        
        ui.PopUpContainer.CloseUI(perkToolTipUI);
    }

    public void SetVisible(bool visible)
    {
        isVisible = visible;
        if(slotRoot != null) slotRoot.SetActive(visible);

        if (!visible)
        {
            HideTooltip();
        }
    }
 
    /// <summary>
    /// perk 데이터를 등급별로 분류합니다.
    /// </summary>
    private void ParsingPerkData()
    {
        perkDict = new Dictionary<ERarity, List<PerkDataBase>>
        {
            { ERarity.Normal, new List<PerkDataBase>() },
            { ERarity.Rare,   new List<PerkDataBase>() },
            { ERarity.Unique, new List<PerkDataBase>() },
            { ERarity.Legend, new List<PerkDataBase>() },
        };

        var perkDatas = GlobalManagers.Resource.GetResources<PerkDataBase>();

        if (perkDatas == null || perkDatas.Count == 0)
        {
            Logger.LogError("[Perk] 재능 데이터가 존재하지 않습니다.");
            return;
        }

        foreach (var perkData in perkDatas)
        {
            if (perkDict.TryGetValue(perkData.PerkRarity, out var list))
                list.Add(perkData);
            else
                Logger.LogWarning($"[Perk] 알 수 없는 등급입니다: {perkData.name}");
        }
    }

    /// <summary>
    /// 슬롯 수만큼 확률에 따라 퍽을 뽑습니다.
    /// </summary>
    private void DrawPerks()
    {
        currentDrawnPerks.Clear();

        for (int i = 0; i < perkShopSlots.Count; i++)
        {
            PerkDataBase perkData = DrawPerk();
            if (perkData == null) break;
            currentDrawnPerks.Add(perkData);
        }
    }
    
    /// <summary>
    /// 재능을 뽑고 슬롯에 부여합니다. 성공 여부를 반환합니다.
    /// </summary>
    private bool DrawPerkProcess()
    {
        DrawPerks();

        if (currentDrawnPerks.Count == 0)
        {
            Logger.LogError("[Perk] 뽑기 실패했습니다.");
            return false;
        }

        for (int i = 0; i < perkShopSlots.Count; i++)
        {
            if (i < currentDrawnPerks.Count)
            {
                PerkDataBase perk = currentDrawnPerks[i];
                perkShopSlots[i].OpenShopSlot(perk.PerkIcon, perk.Price);
            }
            else
            {
                perkShopSlots[i].CloseShopSlot();
            }
        }

        return true;
    }
    

    /// <summary>
    /// 재능을 1회 뽑습니다. 뽑은 등급이 비어 있으면 낮은 등급 -> 높은 등급 순으로 대체합니다.
    /// </summary>
    public PerkDataBase DrawPerk()
    {
        ERarity rarity = DrawRarity(2);
        
        for (int r = (int)rarity; r >= 0; r--)
        {
            if (perkDict.TryGetValue((ERarity)r, out var list) && list.Count > 0)
                return list[Random.Range(0, list.Count)];
        }
        
        for (int r = (int)rarity + 1; r < PERK_RARITY_TABLE[2].Length; r++)
        {
            if (perkDict.TryGetValue((ERarity)r, out var list) && list.Count > 0)
                return list[Random.Range(0, list.Count)];
        }

        Logger.LogError("[Perk] 뽑을 수 있는 재능이 없습니다.");
        return null;
    }

    /// <summary>
    /// 퍽의 등급을 뽑습니다.
    /// </summary>
    private ERarity DrawRarity(int tableKey)
    {
        if (!PERK_RARITY_TABLE.TryGetValue(tableKey, out int[] weights))
        {
            Logger.LogWarning($"[Perk] 등급 테이블에 {tableKey}성 행이 없습니다. Normal로 처리합니다.");
            return ERarity.Normal;
        }

        int total = 0;
        foreach (int w in weights) total += w;

        int roll = Random.Range(0, total);
        for (int i = 0; i < weights.Length; i++)
        {
            if (roll < weights[i]) return (ERarity)i;
            roll -= weights[i];
        }

        return ERarity.Normal;
    }


}