using System.Collections.Generic;

 
/// <summary>
/// UI 표시용: 한 축(속성 하나 또는 종족 하나)의 현재 상태.
/// ActiveSynergy는 "켜진 것"만 담지만, UI는 안 켜진 축과 전체 단계(2/4/6)까지 알아야 해서 따로 둡니다.
/// </summary>
public readonly struct SynergyStatus
{
    /// <summary>이 축의 전체 단계. RequiredCount 오름차순 (2, 4, 6).</summary>
    public readonly IReadOnlyList<SynergyData> Tiers;

    /// <summary>현재 인원수.</summary>
    public readonly int Count;

    /// <summary>켜진 단계 (1부터). 0이면 아직 안 켜짐.</summary>
    public readonly int ActiveTier;

    public bool IsActive => ActiveTier > 0;

    public SynergyStatus(IReadOnlyList<SynergyData> tiers, int count, int activeTier)
    {
        Tiers = tiers;
        Count = count;
        ActiveTier = activeTier;
    }
}
