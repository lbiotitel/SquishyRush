
public class ShopSlot
{
    public UnitData Unit { get; set; }
    public bool IsSold { get; set; }
    
    public bool IsEmpty => Unit == null || IsSold;
}
