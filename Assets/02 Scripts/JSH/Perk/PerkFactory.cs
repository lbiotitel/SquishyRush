/// <summary>
/// 재능 데이터 타입에 맞는 런타임 재능(PerkBase)을 만듭니다.
/// PerkBase가 추상이라 생성 경로를 여기 하나로 모읍니다.
/// 호출은 BattleUnit.AddPerk에서만 합니다.
/// </summary>
public static class PerkFactory 
{
    public static PerkBase Create(BattleUnit owner, PerkDataBase data)
    {
        if (owner == null || data == null) return null;

        PerkBase perk = data switch
        {
            InstantPerkData => new InstantPerk(),
            TriggerPerkData => new TriggerPerk(),
            SelfDestructPerkData => new SelfDestructPerk(),
            _ => null,
        };

        if (perk == null)
        {
            Logger.LogWarning($"[Perk] '{data.PerkId}'의 {data.GetType().Name}에 대응하는 클래스가 없습니다.");

            return null;
        }
        
        perk.Initialize(owner, data);
        return perk;
    }
}
