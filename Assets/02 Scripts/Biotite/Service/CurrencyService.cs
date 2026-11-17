using BackEnd;
using BackEnd.Quobject.EngineIoClientDotNet.Modules;
using Cysharp.Threading.Tasks;
using UnityEngine;



//유저 재화에 대한 서비스 로직입니다.
public class CurrencyService : IService
{
    private UserServerCurrencyData userServerCurrencyData;
    
    public void Initialize()
    {
        userServerCurrencyData = GlobalManagers.UserServerData.GetData<UserServerCurrencyData>();
    }

    public void Release()
    {
        
    }


    public int Gold => userServerCurrencyData.UserGold;
    public int Gem => userServerCurrencyData.UserGem;
    
    
    #region Gold Logic

    public void AddGold(int amount, bool showAnimEffect = true)
    {
        if (amount <= 0) return;

        int prevGold = userServerCurrencyData.UserGold;
        int nextGold = Mathf.Clamp(prevGold + amount, 0, GlobalDefines.MAX_VALUE.MAX_GOLD);

        userServerCurrencyData.UserGold = nextGold;

        GlobalManagers.Event.NotifyEvent<OnUserResourceValueChangedEventData>(new OnUserResourceValueChangedEventData
        {
            resourceType = EResourceType.Gold,
            prevValue = prevGold,
            currentValue = nextGold,
            showAnimEffect = showAnimEffect,
            spriteKey = GlobalDefines.ResourceKey.Sprite.ICON_GOLD_SPRITE
        });
    }


    /// <summary>
    /// 골드 사용
    /// </summary>
    /// <param name="amount"></param>
    /// <returns></returns>
    public bool SpendGold(int amount)
    {
        if (amount <= 0 || !HasEnoughGold(amount)) return false;

        int prevGold = userServerCurrencyData.UserGold; //12000 1000
        int nextGold = prevGold - amount;

        userServerCurrencyData.UserGold = nextGold;

        GlobalManagers.Event.NotifyEvent<OnUserResourceValueChangedEventData>(new OnUserResourceValueChangedEventData
        {
            resourceType = EResourceType.Gold,
            prevValue = prevGold,
            currentValue = nextGold,
            showAnimEffect = false,
        });

        return true;
    }


    /// <summary>
    /// 골드가 충분한지 체크
    /// </summary>
    /// <param name="amount"></param>
    /// <returns></returns>
    public bool HasEnoughGold(int amount) => userServerCurrencyData.UserGold >= amount;

    #endregion

    #region Gem Logic

    public void AddGem(int amount, bool showAnimEffect = true)
    {
        if (amount <= 0) return;

        int prevGem = userServerCurrencyData.UserGem;
        int nextGem = Mathf.Clamp(prevGem + amount, 0, GlobalDefines.MAX_VALUE.MAX_GEM);

        userServerCurrencyData.UserGem = nextGem;

        GlobalManagers.Event.NotifyEvent<OnUserResourceValueChangedEventData>(new OnUserResourceValueChangedEventData
        {
            resourceType = EResourceType.Gem,
            prevValue = prevGem,
            currentValue = nextGem,
            showAnimEffect = showAnimEffect,
            spriteKey = GlobalDefines.ResourceKey.Sprite.ICON_GEM_SPRITE
        });
    }
    
    
    public bool SpendGem(int amount)
    {
        if (amount <= 0 || !HasEnoughGem(amount)) return false;

        int prevGem = userServerCurrencyData.UserGem;
        int nextGem = prevGem - amount;

        userServerCurrencyData.UserGem = nextGem;

        GlobalManagers.Event.NotifyEvent<OnUserResourceValueChangedEventData>(new OnUserResourceValueChangedEventData
        {
            resourceType = EResourceType.Gem,
            prevValue = prevGem,
            currentValue = nextGem,
            showAnimEffect = false
        });

        return true;
    }
    

    public bool HasEnoughGem(int amount) => userServerCurrencyData.UserGem >= amount;

    #endregion


    
    /// <summary>
    /// 데이터의 indate를 호춯합니다.
    /// </summary>
    /// <returns></returns>
    public string GetInDate()
        => userServerCurrencyData.InDate;
    
    
    /// <summary>
    /// 파라미터를 리턴합니다.
    /// </summary>
    /// <returns></returns>
    public Param GetParam()
        => userServerCurrencyData.ToParam();


    /// <summary>
    /// 스냅샷을 만들어 놓습니다.
    /// </summary>
    /// <returns></returns>
    public (int Gem, int Gold) CreateSnapshot()
        => (Gem, Gold);
    
    
    /// <summary>
    /// 데이터를 롤백합니다.
    /// </summary>
    /// <param name="snapshot"></param>
    public void RestoreSnapshot((int Gem, int Gold) snapshot)
    {
        userServerCurrencyData.UserGem = snapshot.Gem;
        userServerCurrencyData.UserGold = snapshot.Gold;
        
        GlobalManagers.Event.NotifyEvent<OnUserResourceValueChangedEventData>(new OnUserResourceValueChangedEventData
        {
            showAnimEffect = false
        });
    }


}