
/// <summary>
/// 지금 켜져 있는 시너지 하나.
///
/// RequiredCount 오름차순으로 세웠을 때의 순위(1부터)를 SynergyResolver가 계산합니다.
/// UI가 "2단계"를 그리는 데만 쓰이고, 효과 적용은 이 값을 보지 않습니다.
/// </summary>
public readonly struct ActiveSynergy
{
    public readonly SynergyData Data;

    /// <summary>이 축의 현재 인원수. UI가 "5 / 6"을 그릴 때 씁니다.</summary>
    public readonly int Count;

    /// <summary>몇 단계인지. 1부터.</summary>
    public readonly int TierIndex;

    /// <summary>이 축의 마지막 단계인지. UI에서 만렙 표시용입니다.</summary>
    public readonly bool IsMaxTier;

    public ActiveSynergy(SynergyData data, int count, int tierIndex, bool isMaxTier)
    {
        Data = data;
        Count = count;
        TierIndex = tierIndex;
        IsMaxTier = isMaxTier;
    }
}
