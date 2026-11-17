using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;



//서비스 로직의 컨테이너 역할을 하는 클래스 입니다.
public class ServiceManager : IManager
{
    private readonly Dictionary<Type, IService> serviceDict = new Dictionary<Type, IService>();

    public bool Initialize()
    {
        RegistService<ProfileService>();
        RegistService<ProgressService>();
        RegistService<SettingService>();
        RegistService<CurrencyService>();
        RegistService<EnergyService>();
        RegistService<UnitService>();
        RegistService<ArtifactService>();
        RegistService<AttendanceService>();
        RegistService<MailService>();
        RegistService<GachaService>();
        RegistService<RewardService>();
        RegistService<RankingService>();
        RegistService<DailyQuestService>();
        
        foreach(var service in serviceDict)
            service.Value.Initialize();

        return true;
    }
    

    
    public void Release()
    {
        if (serviceDict == null) return; 

        foreach (var service in serviceDict.Values)
            service?.Release();
        serviceDict.Clear();
    }



    /// <summary>
    /// 서비스를 등록합니다.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    private void RegistService<T>() where T : IService, new()
    {
        var type = typeof(T);
        if (!serviceDict.ContainsKey(type))
            serviceDict.Add(type, new T());
    }

    /// <summary>
    /// 해당하는 서비스를 가져옵니다.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public T GetService<T>() where T : class, IService
    {
        if (serviceDict.TryGetValue(typeof(T), out IService service))
            return (T)service;

        Logger.LogError($"[ServiceManager] 등록되지 않은 서비스 요청: {typeof(T).Name}");
        return null;
    }

}
