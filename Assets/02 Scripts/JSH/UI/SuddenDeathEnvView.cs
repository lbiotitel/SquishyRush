using UnityEngine;

/// <summary>
/// 서든데스 환경 연출(황사, 바람, 폭풍우...). 스테이지 맵 프리팹마다 하나씩 붙입니다.
///
///  인스펙터(프리팹)에 세팅한 모습 = 최대 강도(15중첩)일 때의 모습입니다.
///   - 각 파티클의 Emission Rate over Time → 최대 방출량
///   - Overlay SpriteRenderer의 Color      → 최대 색/알파
///   프리팹을 열어 보이는 그대로가 서든데스 끝 무렵의 화면이라, 아티스트가 숫자 없이 맞출 수 있습니다.
///   플레이하면 Awake에서 0으로 내려가고 중첩에 따라 다시 올라옵니다.
///
/// </summary>
public class SuddenDeathEnvView : MonoBehaviour
{
    [SerializeField] private ParticleSystem[] particles;
    [Tooltip("최대 강도일 때 파티클 재생 속도 배율. 바람이 점점 거세지는 느낌을 줍니다.")]
    [SerializeField] private float maxSimSpeed = 2f;
    [Tooltip("화면을 덮는 색. 비워도 됩니다. SpriteRenderer의 Color가 최대 색/알파입니다.")]
    [SerializeField] private SpriteRenderer overlay;
    [Tooltip("강도가 목표값까지 따라가는 속도(초당). 1이면 0→최대까지 1초.")]
    [SerializeField] private float fadeSpeed = 1.5f;
    [Tooltip("이 중첩에서 연출이 최대가 됩니다. 프리팹에 세팅한 모습 = 이 중첩일 때의 모습.")]
    [SerializeField] private int fullIntensityStack = 10;
    
    private float[] maxRates;
    private Color maxColor;
    private float target, current;

    private void Awake()
    {
        maxRates = new float[particles.Length];
        for (int i = 0; i < particles.Length; i++)
        {
            maxRates[i] = particles[i].emission.rateOverTimeMultiplier;
            particles[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        if (overlay != null) maxColor = overlay.color;
        
        Apply(0f);
    }

    /// <summary>SuddenDeath.OnStackChanged가 호출합니다. 0이면 페이드아웃.</summary>
    public void SetStack(int stack)
    {
        target = Mathf.Clamp01((float)stack / Mathf.Max(1, fullIntensityStack));
        if (stack > 0) 
            foreach(var p in particles) if(!p.isPlaying) p.Play();
    }

    private void Update()
    {
        if (current == target) return;
        
        current = Mathf.MoveTowards(current, target, fadeSpeed * Time.deltaTime);
        Apply(current);
        
        // 완전히 꺼지면 방출만 멈춤 (이미 날아가던 입자는 자연스럽게 사라지게)
        if (current <= 0f)
                foreach(var p in particles) p.Stop(true, ParticleSystemStopBehavior.StopEmitting);
    }

    private void Apply(float t)
    {
        for (int i = 0; i< maxRates.Length; i++)
        {
            var em = particles[i].emission;
            em.rateOverTimeMultiplier = maxRates[i] * t;
            
            var main = particles[i].main;
            main.simulationSpeed = Mathf.Lerp(1f, maxSimSpeed, t);
        }

        if (overlay != null)
        {
            Color c = maxColor;
            c.a *= t;
            overlay.color = c;
        }
    }
}
