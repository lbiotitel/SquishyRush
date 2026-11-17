public interface IBoardActions
{ 
    EInGamePhase CurrentPhase { get; }
    
    /// <summary>드래그 이동/판매가 가능한지. 합성 흐름(보너스 선택) 중엔 false.</summary>
    bool CanEditBoard { get; }
    
    bool TryMoveUnit(SlotIndex from, SlotIndex to);
    EShopActionResult TrySellUnit(SlotIndex slot);
}
