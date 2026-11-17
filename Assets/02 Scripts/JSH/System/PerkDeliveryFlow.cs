using System.Collections.Generic;
using UnityEngine;

public class PerkDeliveryFlow 
{
    // 구매는 했지만 아직 가방에 도착하지 않은 재능들
    // 도착하면 여기서 빼고 인벤토리에 넣습니다. 페이즈가 끝날 때 남아 있으면 강제로 지급합니다.
    private readonly List<PerkDataBase> inFlightPerks = new();

    private readonly PerkInventory perkInventory;
    private readonly ShopPanel shopPanel;
    private readonly BoardView boardView;
    private readonly VfxSystem vfxSystem;
    private readonly VisualEffectData perkPurchaseVfx;
    private readonly PerkShopSystem perkShopSystem;
    
    public PerkDeliveryFlow(PerkInventory perkInventory, ShopPanel shopPanel, BoardView boardView, VfxSystem vfxSystem,
        VisualEffectData perkPurchaseVfx, PerkShopSystem perkShopSystem)
    {
        this.perkInventory = perkInventory;
        this.shopPanel = shopPanel;
        this.boardView = boardView;
        this.vfxSystem = vfxSystem;
        this.perkPurchaseVfx = perkPurchaseVfx;
        this.perkShopSystem = perkShopSystem;

        perkShopSystem.OnPerkPurchased += HandlePerkPurchased;
    }
    
    /// <summary>
    /// 재능 구매 시 슬롯에서 가방으로 연출을 날리고, 도착하면 인벤토리에 넣습니다.
    /// 연출을 재생하지 못하면 바로 지급합니다.
    /// </summary>
    private void HandlePerkPurchased(PerkDataBase perk, Vector3 from)
    {
        inFlightPerks.Add(perk);

        bool played = perkPurchaseVfx != null
                      && boardView.TryScreenToBoardWorld(shopPanel.BagScreenPosition, out Vector3 to)
                      && vfxSystem.PlayProjectile(perkPurchaseVfx, from, to, () => DeliverPerk(perk));
        
        if(!played) DeliverPerk(perk);
    }

    /// <summary>
    /// 날아가던 재능을 인벤토리에 넣고 가방을 튕깁니다.
    /// 이미 강제 지급된 재능이면 목록에 없으므로, 도착 콜백이 늦게 와도 무시됩니다.
    /// 같은 재능을 두 번 샀으면 목록에도 두 개가 있어서, 콜백 하나당 하나씩 정확히 지급됩니다.
    /// </summary>
    private void DeliverPerk(PerkDataBase perk)
    {
        if (!inFlightPerks.Remove(perk)) return;
        
        perkInventory.Add(perk);
        shopPanel.PlayBagReceive();
    }

    /// <summary>
    /// 아직 도착하지 않은 재능을 전부 즉시 지급합니다. 상점 페이즈가 끝날 때 호출합니다.
    /// 연출이 끊기거나 늦어져도 산 재능이 사라지지 않게 하는 안전장치입니다.
    /// </summary>
    public void Flush()
    {
        if (inFlightPerks.Count == 0) return;
        
        perkInventory.AddRange(inFlightPerks);
        inFlightPerks.Clear();
    }

    public void Release()
    {
        perkShopSystem.OnPerkPurchased -= HandlePerkPurchased;
    }
}
