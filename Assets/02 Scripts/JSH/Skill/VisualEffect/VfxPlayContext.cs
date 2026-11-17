using System;
using UnityEngine;

/// <summary>
/// 연출 한 개를 재생하는 데 필요한 전부.
///
/// 재생기는 BattleUnit도 보드도 모릅니다. 이미 좌표로 환산된 값만 받습니다.
/// 덕분에 전투 없이도(테스트 씬에서 좌표 두 개만 넣어도) 재생기를 돌려볼 수 있습니다.
/// </summary>
public readonly struct VfxPlayContext
{
    public readonly VisualEffectData Data;

    /// <summary>시작 지점.</summary>
    public readonly Vector3 From;
    /// <summary>도착 지점. 제자리 연출이면 From과 같습니다.</summary>
    public readonly Vector3 To;

    public readonly float Scale;
    
    /// <summary>
    /// 연출이 대상에 닿은 순간. 순수 연출(사운드, 화면 흔들림 등)만 겁니다.
    ///
    /// 데미지는 PendingHitQueue가 같은 TravelTime 뒤에 로직 쪽에서 넣습니다.
    /// 이 콜백은 VfxSystem이 재생을 건너뛰면(위치 못 찾음, 프리팹 없음) 불리지 않으므로,
    /// 여기에 데미지를 걸면 그때 데미지가 통째로 사라집니다.
    /// </summary>
    public readonly Action OnArrive;

    public VfxPlayContext(VisualEffectData data, Vector3 from, Vector3 to, Action onArrive = null, float scale = 1f)
    {
        Data = data;
        From = from;
        To = to;
        OnArrive = onArrive;
        Scale = scale;
    }
}
