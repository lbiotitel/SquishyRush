using UnityEngine;

public static class InGameBalance
{
    #region 상점

    private static readonly int[] RARITY_COST = { 1, 2, 3, 5 };

    public const int REROLL_BASE_COST = 1;
    public const float REROLL_COST_GROWTH = 1.2f;
    
    public static int GetUnitCost(ERarity rarity) =>  RARITY_COST[(int)rarity];

    public static int GetRerollCost(int baseCost, int rerollCount) => Mathf.CeilToInt(baseCost* Mathf.Pow(REROLL_COST_GROWTH, rerollCount));

    /// <summary>
    /// 유닛의 실제 판매가입니다. 등급·성급 기준 가격에 유닛의 판매가 배율(재능 등)을 곱합니다.
    /// 판매 처리와 판매 가격 표시가 모두 이 함수를 써야 서로 어긋나지 않습니다.
    /// </summary>
    public static int GetSellPrice(BattleUnit unit)
    {
        int basePrice = GetSellPrice(unit.CurrentUnitData.UnitRarity, unit.Star);
        float multiplier = unit.Stat.GetStat(EStatType.SellPriceMultiplier);
        return Mathf.Max(0, Mathf.RoundToInt(basePrice * multiplier));
    }
    
    #endregion

    #region 재능

    public const int MAX_PERK_COUNT = 3;

    #endregion
    
    #region 성

    public const string STAR_MODIFIER_SOURCE = "Star";
    public const string PLAYER_BONUS_MODIFIER_SOURCE = "PlayerBonus";
    
    private static readonly float[] HP_MULT = { 1f, 1.48f, 2.19f, 3.24f };
    private static readonly float[] ATK_MULT = { 1f, 1.31f, 1.72f, 2.25f };

    private static int ClampStar(int star) => Mathf.Clamp(star, 0, GlobalDefines.MAX_VALUE.IG_MAX_STAR);

    public static float GetHpMultBonus(int star) => HP_MULT[ClampStar(star)] - 1f;
    public static float GetAtkMultBonus(int star) => ATK_MULT[ClampStar(star)] - 1f;
          
    #endregion

    #region 판매

    public const int SELL_PENALTY_MIN_STAR = 2;
    public const int SELL_PENALTY = 1;

    public static int GetSellPrice(ERarity rarity, int star)
    {
        int unitCount = 1 << star;
        int total = GetUnitCost(rarity) * unitCount;

        if (rarity != ERarity.Normal && star >= SELL_PENALTY_MIN_STAR) total -= SELL_PENALTY;

        return Mathf.Max(0, total);
    }
    
    #endregion

    #region 라운드 수입

    public const int ELITE_REWARD_GOLD = 5;

    private static readonly int[] INCOME_BY_TIER = { 3, 4, 5, 6, 7 };
    private const int INCOME_TIER_LENGTH = 7;
    
    // 라운드 종료 시 기본 수입
    // 1~4R: 4 / 5~9R: 7 / 10~14R: 10 / 15R~: 14  → 시작 골드 2 포함 20라운드 총 173골드
    public static int GetRoundIncome(int battleRound)
    {
        int tier = Mathf.Clamp((battleRound - 1) / INCOME_TIER_LENGTH, 0, INCOME_TIER_LENGTH - 1);
        return INCOME_BY_TIER[tier];
    }

    #endregion

    #region 상점 경험치

    public const int MAX_SHOP_LEVEL = 9;
    public const int SHOP_EXP_PER_ROUND = 2;

    // index 0 = 1→2 레벨업에 필요한 경험치 ... index 4 = 5→6
    // 퍽 없이 2, 4, 7, 11, 15라운드 시작 시 레벨업
    private static readonly int[] SHOP_EXP_REQ = { 4, 4, 6, 6, 8, 10, 10, 12 };

    /// <summary>
    /// 현재 레벨에서 다음 레벨까지 필요한 경험치. 최대 레벨이면 int.MaxValue.
    /// </summary>
    public static int GetShopExpReq(int shopLevel)
    {
        int idx = shopLevel - 1;
        if (idx < 0 || idx >= SHOP_EXP_REQ.Length) return int.MaxValue;
        return SHOP_EXP_REQ[idx];
    }
    
    #endregion

    #region 상점 레벨별 확률

    // 노말, 레어, 유니크, 레전드 (합계 100)
    private static readonly int[][] SHOP_RARITY_WEIGHTS =
    {
        new[] { 100,  0,  0,  0 }, // Lv1  1~2전투
        new[] {  75, 25,  0,  0 }, // Lv2  3~4
        new[] {  57, 38, 5,  0 }, // Lv3  5~7
        new[] {  40, 45, 15,  0 }, // Lv4  8~10
        new[] {  30, 40, 30,  0 }, // Lv5  11~14
        new[] {  23, 38, 38,  1 }, // Lv6  15~19  ← 레전드 첫 등장
        new[] {  18, 33, 45,  4 }, // Lv7  20~24
        new[] {  13, 27, 51,  9 }, // Lv8  25~30
        new[] {   6, 24, 55, 15 }, // Lv9  31~35
    };

    public static ShopLevelSetting GetShopLevelSetting(int shopLevel)
    {
        int idx = Mathf.Clamp(shopLevel, 1, MAX_SHOP_LEVEL) - 1;
        return new ShopLevelSetting(SHOP_RARITY_WEIGHTS[idx], REROLL_BASE_COST);
    }

    #endregion

    #region 크리 데미지, 일반 공격 공격력 비율, 확률 퍼센트 스케일

    public const float CRIT_MULTIPLIER = 1.5f;
    public const float ATK_RATIO = 0.5f;
    public const int PERCENT_SCALE = 1000;
    
    #endregion
}
