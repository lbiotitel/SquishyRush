using UnityEngine;

public static class StonePhysicsReactor
{
    public static void ApplyMergePop(StoneEntity stone, int newStar)
    {
        var rb = stone.Rb;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        Vector3 horizontalDir = GetDirectionTowardNearbyStones(stone);
        float popForce = 10f + (newStar * 10f);
        float upwardRatio = 0.7f;

        Vector3 finalDir = (Vector3.up * upwardRatio + horizontalDir * (1f - upwardRatio)).normalized;

        rb.AddForce(finalDir * popForce, ForceMode.Impulse);

        Vector3 randomTorque = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized;

        float torqueForce = 5f + (newStar * 2f);
        rb.AddTorque(randomTorque *  torqueForce, ForceMode.Impulse);
    }

    static Vector3 GetDirectionTowardNearbyStones(StoneEntity self, float searchRadius = 3f)
    {
        Vector3 accum = Vector3.zero;
        int count = 0; 

        foreach(var other in StoneRegistry.ActiveStones)
        {
            if(other == self) continue;
            Vector3 toOther = other.transform.position - self.transform.position;
            toOther.y = 0;
            float dist = toOther.magnitude;
            if(dist > searchRadius || dist < 0.01f) continue;

            accum += toOther.normalized;
            count++;
        }

        if(count == 0)
        {
            float randAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            return new Vector3(Mathf.Cos(randAngle), 0, Mathf.Sin(randAngle));
        }

        Vector3 avgDir = (accum / count).normalized;
        float randAngleOffset = Random.Range(-60f, 60f);
        avgDir = Quaternion.Euler(0, randAngleOffset, 0) * avgDir;
        return avgDir;
    }
}
