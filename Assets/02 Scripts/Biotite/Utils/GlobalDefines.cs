using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.U2D;

// 전역으로 사용할 상수들의 모임입니다.
public static class GlobalDefines
{

    public static class PathKey
    {
        public const string SETTING_SAVE_DATA = "Setting.json";

        public static string GetOptionPath()
        {
            return Path.Combine(Application.persistentDataPath, SETTING_SAVE_DATA);
        }
    }

    // 유닛에 대한 정의입니다.
    public static class UnitConstant
    {
        //처음 주는 유닛들
        public static readonly IReadOnlyList<string> FirstUnit = new List<string>()
        {
            "Unit_2_0001", "Unit_1_0002", "Unit_1_0003", "Unit_1_0004", "Unit_1_0005",
            "Unit_2_0006", "Unit_1_0007", "Unit_1_0008", "Unit_2_0009", "Unit_1_0010",
            "Unit_1_0011", "Unit_2_0012", "Unit_2_0013", "Unit_4_0014", "Unit_2_0015", 
            "Unit_2_0016", "Unit_1_0017", "Unit_1_0018", "Unit_2_0019", "Unit_1_0020",
            "Unit_2_0021", "Unit_3_0022", "Unit_1_0023", "Unit_3_0024", "Unit_3_0025", 
            "Unit_1_0026", "Unit_2_0027", "Unit_2_0028", "Unit_2_0029", "Unit_3_0030", 
            "Unit_3_0031", "Unit_3_0032", "Unit_2_0033", "Unit_1_0034", "Unit_2_0035",
            "Unit_1_0036", "Unit_2_0037", "Unit_1_0038", "Unit_1_0039", "Unit_4_0040",
            "Unit_3_0041", "Unit_3_0042", "Unit_3_0043", "Unit_3_0044", "Unit_1_0045",
            "Unit_2_0046", "Unit_3_0047", "Unit_4_0048", "Unit_4_0049", "Unit_1_0050", 
            "Unit_2_0051", "Unit_1_0052", "Unit_4_0053", "Unit_4_0054", "Unit_3_0055",
            "Unit_2_0056", "Unit_4_0057", "Unit_3_0058", "Unit_4_0059", "Unit_3_0060"
        }.AsReadOnly();

        private static string Syn(string key)
            => Localization.GetLocaliationText(Localization.LocalizationTable.Synergy, key);
        
        public static string GetUnitRaceName(ERace race)
        {
            return race switch
            {
                ERace.Wild     => Syn(LocaliztionKeyCode.SY_0016_NAME), // 야생
                ERace.Fly      => Syn(LocaliztionKeyCode.SY_0019_NAME), // 비행
                ERace.Aquatic  => Syn(LocaliztionKeyCode.SY_0021_NAME), // 수생
                ERace.Plant    => Syn(LocaliztionKeyCode.SY_0024_NAME), // 식물
                ERace.Ghost    => Syn(LocaliztionKeyCode.SY_0026_NAME), // 유령
                ERace.Spirit   => Syn(LocaliztionKeyCode.SY_0029_NAME), // 정령
                ERace.Creature => Syn(LocaliztionKeyCode.SY_0030_NAME), // 괴수
                ERace.Snack    => Syn(LocaliztionKeyCode.SY_0034_NAME), // 스낵
                ERace.Golem    => Syn(LocaliztionKeyCode.SY_0037_NAME), // 골렘
                ERace.Death    => Syn(LocaliztionKeyCode.SY_0038_NAME), // 죽음
                ERace.Dragon   => Syn(LocaliztionKeyCode.SY_0039_NAME), // 용
                _ => string.Empty
            };
        }

        public static string GetUnitElementalName(EElementType elementType)
        {
            return elementType switch
            {
                EElementType.FIRE  => Syn(LocaliztionKeyCode.SY_0001_NAME), // 불
                EElementType.WATER => Syn(LocaliztionKeyCode.SY_0004_NAME), // 물
                EElementType.EARTH => Syn(LocaliztionKeyCode.SY_0007_NAME), // 땅
                EElementType.LIGHT => Syn(LocaliztionKeyCode.SY_0010_NAME), // 빛
                EElementType.DARK  => Syn(LocaliztionKeyCode.SY_0013_NAME), // 어둠
                _ => string.Empty
            };
        }
    }


    // 등급에 대한 정의입니다.
    public static class Rarity
    {
        /// <summary>
        /// 현재 등급에 알맞은 색깔을 리턴합니다.
        /// </summary>
        public static Color GetRarityColor(ERarity rarity)
        {
            switch (rarity)
            {
                case ERarity.Normal:
                    return new Color(0.47f, 0.56f, 0.61f, 1.0f); 

                case ERarity.Rare:
                    return new Color(0.12f, 0.53f, 1.00f, 1.0f); 

                case ERarity.Unique:
                    return new Color(0.68f, 0.20f, 1.00f, 1.0f); 

                case ERarity.Legend:
                    return new Color(1.00f, 0.34f, 0.13f, 1.0f);

                default:
                    return Color.white;
            }
        }
        
        public static Color GetRarityPastelColor(ERarity rarity)
        {
            return rarity switch
            {
                ERarity.Normal => new Color32(0xD3, 0xD9, 0xE0, 0xFF),
                ERarity.Rare => new Color32(0xA8, 0xD5, 0xFF, 0xFF),
                ERarity.Unique => new Color32(0xB3, 0x8E, 0xFD, 0XFF),
                ERarity.Legend => new Color32(0xFF, 0x62, 0x40, 0xFF),
                _ => Color.white
            };
        }
        

        /// <summary>
        /// 해당 등급에 알맞는 텍스트를 반환합니다.
        /// </summary>
        public static string GetRarityText(ERarity rarity)
        {
            switch (rarity)
            {
                case ERarity.Normal:
                    return Localization.GetLocaliationText(
                        Localization.LocalizationTable.UI,
                        "ui_rarity_normal_text");

                case ERarity.Rare:
                    return Localization.GetLocaliationText(
                        Localization.LocalizationTable.UI,
                        "ui_rarity_rare_text");

                case ERarity.Unique:
                    return Localization.GetLocaliationText(
                        Localization.LocalizationTable.UI,
                        "ui_rarity_unique_text");

                case ERarity.Legend:
                    return Localization.GetLocaliationText(
                        Localization.LocalizationTable.UI,
                        "ui_rarity_legend_text");
            }

            return "BUG";
        }
    }
    
    
    
    
    //색상에 관한 정의 입니다.
    public static class ColorConstants
    {
        public const string ARTIFACT_VALUE_COLOR = "#2F8F5B"; 
        
        public const string STAT_NAME_COLOR  = "#4A3426"; 
        public const string STAT_VALUE_COLOR = "#E8557F"; 
    }


    // 티어에 대한 정의입니다.
    public static class TierRP
    {
        private const int BRONZE_RP = 1000;
        private const int SILVER_RP = 1500;
        private const int GOLD_RP = 2000;
        private const int PLATINUM_RP = 2500;
        private const int DIAMOND_RP = 3000;
        private const int MASTER_RP = 4000;
        private const int GRAND_MASTER_RP = 5000;

        public enum ETier
        {
            None,
            Bronze,
            Silver,
            Gold,
            Platinum,
            Diamond,
            Master,
            GrandMaster
        }
        

        /// <summary>
        /// 현재 RP에 해당하는 티어의 색깔을 반환합니다.
        /// </summary>
        public static Color GetTierColor(int rp)
        {
            ETier myTier = GetTierByRP(rp);

            switch (myTier)
            {
                case ETier.Bronze:
                    return new Color(0.8f, 0.5f, 0.2f);

                case ETier.Silver:
                    return new Color(0.75f, 0.75f, 0.75f);

                case ETier.Gold:
                    return new Color(1.0f, 0.84f, 0.0f);

                case ETier.Platinum:
                    return new Color(0.0f, 0.8f, 0.8f);

                case ETier.Diamond:
                    return new Color(0.4f, 0.7f, 1.0f);
                
                case ETier.Master:
                    return new Color(0.86f, 0.20f, 0.27f); 

                case ETier.GrandMaster:
                    return new Color(0.35f, 0.82f, 1.0f);
                
                default:
                    return Color.white;
            }
        }

        /// <summary>
        /// RP를 티어로 변환합니다.
        /// </summary>
        public static ETier GetTierByRP(int rp)
        {
            if (BRONZE_RP <= rp && rp < SILVER_RP)
                return ETier.Bronze;

            if (SILVER_RP <= rp && rp < GOLD_RP)
                return ETier.Silver;

            if (GOLD_RP <= rp && rp < PLATINUM_RP)
                return ETier.Gold;

            if (PLATINUM_RP <= rp && rp < DIAMOND_RP)
                return ETier.Platinum;

            if (DIAMOND_RP <= rp && rp < MASTER_RP)
                return ETier.Diamond;

            if (MASTER_RP <= rp && rp < GRAND_MASTER_RP)
                return ETier.Master;
            
            if(GRAND_MASTER_RP <= rp)
                return ETier.GrandMaster;

            return ETier.None;
        }
    }

    public static class ResourceKey
    {
        public static class Sprite
        {
            public const string ICON_GOLD_SPRITE = "Currency_001.sprite";
            public const string ICON_GEM_SPRITE = "Currency_002.sprite";
            public const string ICON_ENERGY_SPRITE = "Currency_003.sprite";
        }

        public static class UI
        {
            public const string LEADER_BOARD_NODE_UI_PREFAB =
                "Leader Board Node UI Prefab";

            public const string UNIT_LIST_NODE_CELL_VIEW_PREFAB =
                "UnitListCellView Prefab";

            public const string ARTIFACT_CELL_VIEW_PREFAB =
                "ArtifactListCellView Prefab";

            public const string DAILY_REWARD_CELL_VIEW_PREFAB =
                "DailyRewardCellView Prefab";

            public const string LANGUAGE_BUTTON_PREFAB =
                "Language Button Prefab";

            public const string PROFILE_IMAGE_CELL_VIEW_PREFAB =
                "ProfileImageCellView Prefab";

            public const string MAIL_CELL_VIEW_PREFAB =
                "MailCellView Prefab";

            public const string MAIL_REWARD_ITEM_CELL_VIEW_PREFAB =
                "Mail Reward Item Cell Veiw Prefab";

            public const string RESOURCE_MOVER_PREFAB = "Resource Mover Prefab";
            public const string UNIT_LEVEL_CELL_VIEW_PREFAB = "UnitLevelCellView Prefab";
            public const string GACHA_FRAME_PREFAB = "GachaFrame Prefab";
            public const string DAILY_QUEST_CELL_VIEW_PREFAB = "DailyQuestCellView Prefab";
        }

        public static class Audio
        {
            public const string LOBBY_BGM = "Lobby BGM";
            public const string TITLE_BGM = "Title BGM";
            public const string INGAME_BGM = "InGame BGM";

            public const string BUTTON_CLICK_SFX = "Button Click SFX";
            public const string RESOURCE_GET_SFX = "Resource Get SFX";
            public const string ARTIFACT_UPGRADE_SFX = "ArtifactUpgrade SFX";
            public const string GAME_START_SFX = "GameStart SFX";
            public const string GACHA_UNIT_SFX = "GachaUnit SFX";
            public const string LEGEND_UNIT_SFX = "LegendUnit SFX";
            public const string SWITCH_BTN_SFX = "SwitchBtn SFX";
            public const string PURCHASE_SFX = "Spawn SFX";
            public const string MERGED_SFX = "Merged SFX";
            public const string SELL_SFX = "Sell SFX";
            public const string REROLL_SFX = "Reroll SFX";
            public const string BATTLE_START_SFX = "BattleStart SFX";
            public const string DIE_SFX = "Die SFX";
            public const string HIT_SFX = "Hit SFX";
            public const string USER_LEVEL_UP_SFX = "UserLevelUp SFX";
            public const string RESULT_SFX = "Result SFX";
            public const string VERSUS_SFX = "Versus SFX";
            public const string PERK_EQUIP_SFX = "PerkEquip SFX";
            public const string STAGE_UNLOCK_SFX = "StageUnlock SFX";
            public const string DEATH_FLY_SFX = "DeathFly SFX";
            public const string CHEST_LAND_SFX = "ChestLand SFX";
        }

        public static class Atlas
        {
            public const string PROFILE_IMAGE_ATLAS =
                "Profile_Image_Atlas";
        }
    }

    public static class DataTableName
    {
        public const string USER_ARTIFACT_DATA_TABLE =
            "USER_ARTIFACT_DATA";

        public const string USER_UNIT_DATA_TABLE =
            "USER_UNIT_DATA";

        public const string USER_DAILY_QUEST_TABLE
            = "USER_DAILY_QUEST_DATA";
        
        public const string USER_ATTENDANCE_DATA_TABLE =
            "USER_ATTENDANCE_DATA";

        public const string USER_ENERGY_DATA_TABLE =
            "USER_ENERGY_DATA";

        public const string USER_CURRENCY_DATA_TABLE =
            "USER_CURRENCY_DATA";

        public const string USER_PROFILE_DATA_TABLE =
            "USER_PROFILE_DATA";
        
        public const string USER_PROGRESS_DATA_TABLE = 
            "USER_PROGRESS_DATA";

        public const string RANKING_DATA_TABLE =
            "01a05b18-722c-78a1-ac56-04985b85db0f";

        public const string UNIT_GACHA_TABLE = "20312";
        public const string ARTIFACT_GACHA_TABLE = "19808";
    }

    // 게임 상수
    public static class GameConstants
    {
        public const string DEFAULT_PROFILE_ID = "Profile_Image_17";
        public const int ENERGY_REGEN_TIME = 1800;

        public const int GACHA_1X_REQUIRED_GEM = 100;

        public const int GACHA_DUPLICATED_STONE_COUNT = 10;

        public const int REFILL_ENERGY_AMOUNT = 5;

        public const int MAIL_REQUEST_COUNT = 10;
        public const int MAIL_REQUEST_DELAY = 2000;
        
        public const int FRAME_RATE = 80;
        public const float NORMAL_GAME_SPEED = 1f;
        public const float FAST_GAME_SPEED = 1.5f;
        
        public const long DATA_SAVE_INTERVAL = 15;
        public const double BACKGROUND_TIME = 10;
        public const int RANK_COUNT = 50;
        public const string PRIVACY_INFO_URL = "https://app.notion.com/p/3f6c648a2285801db668fca11b234dc6?source=copy_link";
    }

    // 각각의 최댓값에 대한 정의입니다.
    public static class MAX_VALUE
    {
        public const string MAX_LEVEL_TEXT = "MAX";
        
        public const int MAX_USER_LEVEL = 99; 
        public const int UNIT_MAX_LEVEL = 20;
        public const int ARTIFACT_MAX_LEVEL = 99;
        public const int MAX_GOLD = 999999;
        public const int MAX_GEM = 999999;
        public const int MAX_ENERGY = 999;

        public const int IG_MAX_STAR = 3;
        public const int IG_MAX_SLOT_COUNT = 4;

        public const int MAX_REGEN_ENERGY = 5;

        public const int MAX_DAILY_REWARD_COUNT = 15;
    }

    public static class DAMAGE_TEXT
    {
        public static readonly Color NORMAL = Color.white;
        public static readonly Color CRIT    = new Color32(0xFB, 0x00, 0x1C, 0xFF);
        public static readonly Color SHOCK   = new Color32(0x4F, 0xC3, 0xF7, 0xFF);
        public static readonly Color SHIELD  = new Color32(0xB0, 0xBE, 0xC5, 0xFF);
        public static readonly Color HEAL    = new Color32(0x66, 0xBB, 0x6A, 0xFF);
        public static readonly Color REFLECT = new Color32(0xCE, 0x93, 0xD8, 0xFF);
        
        public const float SCALE_NORMAL = 1.0f;
        public const float SCALE_CRIT = 1.35f;
        public const float SCALE_SUB = 0.9f;
    }
}