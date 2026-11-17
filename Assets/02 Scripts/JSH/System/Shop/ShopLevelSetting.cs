
public readonly struct ShopLevelSetting
{
    public readonly int[] RarityWeights;
    public readonly int RerollBaseCost;

    public ShopLevelSetting(int[] weights, int rerollBaseCost)
    {
        RarityWeights = weights;
        RerollBaseCost = rerollBaseCost;
    }
}
