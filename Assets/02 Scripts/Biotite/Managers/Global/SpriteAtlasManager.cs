using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;


//스프라이트아틀라스를 뽑아 스프라이트들로 캐싱하는 매니저 입니다.
public class SpriteAtlasManager : IManager
{

    private readonly Dictionary<string, Sprite> spriteDict = new Dictionary<string, Sprite>();
    private bool isInit = false;
        
    public bool Initialize()
    {
        if (isInit) return true;
        isInit = true;
        GlobalManagers.Event.AddEvent<OnGameResourceDataLoadedEventData>(CashingSprite);
        return true;
    }


    
    /// <summary>
    /// 아틀라스를 가지고와서 스프라이트를 캐싱해둡니다.
    /// </summary>
    /// <param name="e"></param>
    private void CashingSprite(OnGameResourceDataLoadedEventData e)
    {
        List<SpriteAtlas> spriteAtlasList = GlobalManagers.Resource.GetResources<SpriteAtlas>();

        foreach (SpriteAtlas spriteAtlas in spriteAtlasList)
        {
            Sprite[] sprite = new Sprite[spriteAtlas.spriteCount];
            spriteAtlas.GetSprites(sprite);

            foreach (var s in sprite)
            {
                string key = s.name.Replace("(Clone)", "");
                spriteDict.TryAdd(key, s);
            }
        }
    }
    

    /// <summary>
    /// 스프라이트를 딕셔너리에서 가져옵니다.
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    private Sprite GetSprite(string key)
    {
        return spriteDict.GetValueOrDefault(key) ?? null;
    }


    
    
    /// <summary>
    /// profileId에 해당하는 프로필 이미지를 가져옵니다.
    /// </summary>
    /// <param name="profileId"></param>
    /// <returns></returns>
    public Sprite GetProfileImage(string profileId)
    {
        return GetSprite(profileId);
    }


    /// <summary>
    /// 유닛의 속성 아이콘을 가져옵니다.
    /// </summary>
    /// <param name="elementType"></param>
    /// <returns></returns>
    public Sprite GetUnitElementalIcon(EElementType elementType)
    {
        switch (elementType)
        {
            case EElementType.FIRE:
                return GetSprite("Elemental_Fire.sprite");

            case EElementType.WATER:
                return GetSprite("Elemental_Water.sprite");

            case EElementType.EARTH:
                return GetSprite("Elemental_Earth.sprite");

            case EElementType.DARK:
                return GetSprite("Elemental_Dark.sprite");

            case EElementType.LIGHT:
                return GetSprite("Elemental_Light.sprite");

            default:
                return null;
        }
    }
    
    
    /// <summary>
    /// 유닛의 종족 아이콘을 가져옵니다.
    /// </summary>
    /// <param name="race"></param>
    /// <returns></returns>
    public Sprite GetUnitRaceIcon(ERace race)
    {
        switch (race)
        {
            case ERace.Wild:
                return GetSprite("Race_Wild.sprite");
            case ERace.Aquatic:
                return GetSprite("Race_Aquatic.sprite");
            case ERace.Creature:
                return GetSprite("Race_Creature.sprite");
            case ERace.Death:
                return GetSprite("Race_Death.sprite");
            case ERace.Golem:
                return GetSprite("Race_Golem.sprite");
            case ERace.Ghost:
                return GetSprite("Race_Ghost.sprite");
            case ERace.Fly:
                return GetSprite("Race_Fly.sprite");
            case ERace.Plant:
                return GetSprite("Race_Plant.sprite");
            case ERace.Snack:
                return GetSprite("Race_Snack.sprite"); 
            case ERace.Spirit:
                return GetSprite("Race_Spirit.sprite");    
            case ERace.Dragon:
                return GetSprite("Race_Dragon.sprite");
            default:
                return null;
        }
    }
    
    
    
    /// <summary>
    ///  rp 에 해당하는 티어 이미지를 가져옵니다.
    /// </summary>
    /// <param name="rp"></param>
    /// <returns></returns>
    public Sprite GetTierImage(int rp)
    {
        GlobalDefines.TierRP.ETier tier = GlobalDefines.TierRP.GetTierByRP(rp);
        
        switch (tier)
        {
            case GlobalDefines.TierRP.ETier.Bronze:
                return GetSprite("Tier_Bronze.sprite");

            case GlobalDefines.TierRP.ETier.Silver:
                return GetSprite("Tier_Silver.sprite");

            case GlobalDefines.TierRP.ETier.Gold:
                return GetSprite("Tier_Gold.sprite");

            case GlobalDefines.TierRP.ETier.Platinum:
                return GetSprite("Tier_Platinum.sprite");

            case GlobalDefines.TierRP.ETier.Diamond:
                return GetSprite("Tier_Diamond.sprite");
            
            case GlobalDefines.TierRP.ETier.Master:
                return GetSprite("Tier_Master.sprite");
            
            case GlobalDefines.TierRP.ETier.GrandMaster:
                return GetSprite("Tier_GrandMaster.sprite");
        }

        return null;
    }
    

    public void Release()
    {
        isInit = false;
        GlobalManagers.Event.RemoveEvent<OnGameResourceDataLoadedEventData>(CashingSprite);
        spriteDict.Clear();
    }
}
