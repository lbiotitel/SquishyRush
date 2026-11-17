
//인게임 상태 정의 enum
public enum EInGamePhase
{
    None,
    Shop,
    Battle,
    Settlement
}

//인게임 그리드 구역 정의 enum
public enum EZone
{
    Bench, 
    Battle
}

//슬롯을 구조체로 묶어 구역을 정리
public readonly struct SlotIndex
{
    public readonly EZone Zone;
    public readonly int Index;

    public SlotIndex(EZone zone, int index)
    {
        Zone = zone;
        Index = index;
    }

    public override string ToString() => $"{Zone}[{Index}]";
}

public enum EShopActionResult
{
    Success,
    WrongPhase,
    InvalidSlot,
    SlotEmpty,
    NotEnoughGold,
    BenchFull,
    NoUnitInSlot
}

public static class ShopActionResultExtensions
{
    public static string GetMessageKey(this EShopActionResult result)
    {
        switch (result)
        {
            case EShopActionResult.NotEnoughGold : return LocaliztionKeyCode.UI_GOLD_IS_NOT_ENOUGH_TEXT;
            case EShopActionResult.BenchFull : return LocaliztionKeyCode.UI_BENCH_IS_FULL_TEXT;
            default: return null;
        }
    }
}
