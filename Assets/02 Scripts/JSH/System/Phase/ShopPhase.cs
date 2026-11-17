/// <summary>
/// 정비 단계입니다. 일반 라운드에서는 유닛상점만, 재능상점 라운드에서는 재능상점도 함께 엽니다.
/// 재능상점 라운드가 끝나면 컨트롤러가 라운드를 넘긴 뒤 바로 전투로 보냅니다(InGameController.StartBattle).
/// </summary>
public class ShopPhase : InGamePhaseBase
{
    public override EInGamePhase Type => EInGamePhase.Shop;
    
    public ShopPhase(InGameController c, InGamePhaseMachine m) : base(c, m) { }

    public override void Enter()
    {
        controller.Context.ResetRerollCount();
        controller.Shop.Roll(controller.Context);
        controller.ShopPanel.SlideIn();
        controller.PrepControlUI.Show();

        controller.PrepareNextEnemy();

        if (controller.IsPerkShopRound)
            controller.BeginPerkShop();
        
        controller.BoardView.Refresh();
        
        controller.MeterDrawer.Show(controller.LastMeter);
    }

    public override void Exit()
    {
        controller.PerkDelivery.Flush();
        controller.EndPerkShop();
        controller.PrepControlUI.Hide();
        controller.ShopPanel.SlideOut();

        controller.MeterDrawer.Hide();
    }
}
