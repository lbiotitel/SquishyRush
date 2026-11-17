using System;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;


//오디오에 관한 클래스 정의입니다.
public class AudioManager : IManager
{
    [SerializeField] private int sfxSourceCount = 10;

    private Transform audioContainer;

    private AudioSource bgmSource;
    private List<AudioSource> sfxSources;
    
    private ResourceTableManager resource;

    private float bgmVolume;
    private float sfxVolume;

    private bool bgmMute = false;
    private bool sfxMute = false;
    private bool isInit = false;

    public bool Initialize()
    {
        if (isInit) return true;
        isInit = true;

        audioContainer = GameObject.Find("AudioContainer").transform;

        if (audioContainer == null)
        {
            Logger.LogError("오디오 컨테이너가 존재하지 않습니다.");
            return false;
        }

        bgmSource = audioContainer.gameObject.AddComponent<AudioSource>();
        sfxSources = new List<AudioSource>();

        for (int i = 0; i < sfxSourceCount; i++)
        {
            sfxSources.Add(audioContainer.gameObject.AddComponent<AudioSource>());
        }
        
        FirstInitAudio();

        resource = GlobalManagers.Resource;

        GlobalManagers.Event.AddEvent<OnSettingDataChangedEventData>(OnSettingDataChangedHandler);

        return true;
    }

    
    
    /// <summary>
    /// 최초로 로컬 데이터를 가지고와서 오디오를 세팅 합니다.
    /// </summary>
    private void FirstInitAudio()
    {
        UserSettingData userSettingData = GlobalManagers.UserLocalData.UserSetting;

        bgmMute = !userSettingData.BgmOn;
        sfxMute = !userSettingData.SfxOn;
        bgmVolume = userSettingData.BgmVolume;
        sfxVolume = userSettingData.SfxVolume;
        
        SetAudio();
    }


    private void OnSettingDataChangedHandler(OnSettingDataChangedEventData e)
        => SetAudioBySettingData();



    /// <summary>
    /// 오디오 데이터 세팅을 변경하면 오디오 데이터를 변경시킵니다.
    /// </summary>
    private void SetAudioBySettingData()
    {
        var setting = GlobalManagers.Service.GetService<SettingService>();
        bgmVolume = setting.BgmVolume;
        sfxVolume = setting.SfxVolume;
        bgmMute = !setting.BgmOn;
        sfxMute = !setting.SfxOn;
        SetAudio();
    }

    
    
    /// <summary>
    /// 오디오 사운드 세팅을 합니다.
    /// </summary>
    private void SetAudio()
    {
        bgmSource.volume = bgmVolume;
        
        foreach (var sfx in sfxSources)
            sfx.volume = sfxVolume;
        
        if (bgmMute) bgmSource.volume = 0f;

        if (sfxMute)
        {
            foreach (var sfx in sfxSources)
                sfx.volume = 0f;
        }
    }


    /// <summary>
    /// sfx 소리를 재생합니다.
    /// </summary>
    /// <param name="audioName"></param>
    /// <param name="randomPitch"></param>
    public void PlaySfx(string audioName, bool randomPitch = false)
    {
        if (!GetAudioClip(audioName, out AudioClip audioClip)) return;

        foreach (var source in sfxSources)
        {
            if (source.isPlaying) continue;
            source.pitch = randomPitch ? UnityEngine.Random.Range(0.85f, 1.1f) : 1f;
            source.clip = audioClip;
            source.Play();
            return;
        }
        
        sfxSources[0].PlayOneShot(audioClip);
    }
    
    
    
    /// <summary>
    /// bgm을 출력합니다.
    /// </summary>
    /// <param name="audioName"></param>
    /// <param name="isLoop"></param>
    public void PlayBgm(string audioName, bool isLoop = true)
    {
        AudioClip audioClip;
        if (!GetAudioClip(audioName, out audioClip)) return;
        
        bgmSource.clip = audioClip;
        bgmSource.loop = isLoop;
        bgmSource.Play();
    }
    
    
    
    public void StopBGM() => bgmSource.Stop();
    public void ResumeBgm() => bgmSource.Play();




    /// <summary>
    /// 데이터 매니저에서 오디오 클립을 가져옵니다.
    /// </summary>
    /// <param name="audioName"></param>
    /// <param name="audioClip"></param>
    /// <returns></returns>
    private bool GetAudioClip(string audioName, out AudioClip audioClip)
    {
        audioClip = null;
        
        if(!resource.TryGetResource<AudioClip>(audioName, out AudioClip resourceClip))
        {
            Logger.LogError($"{audioName} 에 해당하는 오디오 클립이 없습니다.");
            return false;
        }

        audioClip = resourceClip;
        return true;
    }
    

    /// <summary>
    /// 이벤트를 전부해제하고 bgm을 멈춥니다.
    /// </summary>
    public void Release()
    {
        if (!isInit) return;
        isInit = false;

        GlobalManagers.Event.RemoveEvent<OnSettingDataChangedEventData>(OnSettingDataChangedHandler);

        if (bgmSource != null)
        {
            bgmSource.Stop();
            GameObject.Destroy(bgmSource);
        }
        bgmSource = null;

        foreach (var source in sfxSources)
        {
            if (source == null) continue;
            source.Stop();
            GameObject.Destroy(source);
        }
        sfxSources.Clear();
    }
    
    
}
