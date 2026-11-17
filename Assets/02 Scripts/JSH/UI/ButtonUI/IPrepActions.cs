
public interface IPrepActions
{
    /// <summary>전투 시작. 재능상점 라운드면 라운드를 넘긴 뒤 정비 없이 바로 전투합니다.</summary>
    void StartBattle();

    
    /// <summary>
    /// 재능상점 라운드에서 적 미리보기 ↔ 재능 상품을 전환합니다.
    /// 전환 후 상품이 보이고 있으면 true
    /// </summary>
    bool TogglePerkPreview();
    
}
