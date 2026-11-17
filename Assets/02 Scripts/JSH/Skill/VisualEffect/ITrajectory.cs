using UnityEngine;

/// <summary>
/// 발사체가 출발점에서 도착점까지 어떤 경로로 가는가.
///
/// 상태를 갖지 않습니다. 필요한 값을 전부 인자로 받기 때문에
/// 구현체를 정적 인스턴스 하나로 공유할 수 있고, 발사체 수만큼 만들 필요가 없습니다.
///
/// 나선이나 사인파가 필요해지면 구현체 하나만 추가하면 됩니다.
/// </summary>
public interface ITrajectory
{
    /// <param name="t">0 = 출발, 1 = 도착</param>
    Vector3 Evaluate(Vector3 from, Vector3 to, float t, float arcHeight);
}

public sealed class LinearTrajectory : ITrajectory
{
    public Vector3 Evaluate(Vector3 from, Vector3 to, float t, float arcHeight)
        => Vector3.Lerp(from, to, t);
}

public sealed class ArcTrajectory : ITrajectory
{
    public Vector3 Evaluate(Vector3 from, Vector3 to, float t, float arcHeight)
    {
        Vector3 control = (from + to) * 0.5f + Vector3.up * arcHeight;

        return Vector3.Lerp(
            Vector3.Lerp(from, control, t),
            Vector3.Lerp(control, to, t),
            t);
    }
}
