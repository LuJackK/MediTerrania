using UnityEngine;

public static class ReefMetricNormalizer
{
    public static float Clamp01(float value)
    {
        if (float.IsNaN(value) || float.IsInfinity(value))
        {
            return 0f;
        }

        return Mathf.Clamp01(value);
    }

    public static float NormalizeRange(float value, float min, float max)
    {
        if (Mathf.Approximately(min, max))
        {
            return value >= max ? 1f : 0f;
        }

        return Clamp01((value - min) / (max - min));
    }

    public static float NormalizeInverse(float value, float min, float max)
    {
        return Clamp01(1f - NormalizeRange(value, min, max));
    }

    public static float NormalizeDepthSuitability(float rawDepthMeters, float idealMin, float idealMax, float tolerance)
    {
        float minAllowed = idealMin - Mathf.Abs(tolerance);
        float maxAllowed = idealMax + Mathf.Abs(tolerance);
        return RangeSuitability(rawDepthMeters, idealMin, idealMax, minAllowed, maxAllowed);
    }

    public static float RangeSuitability(float value, float idealMin, float idealMax, float minAllowed, float maxAllowed)
    {
        if (idealMax < idealMin)
        {
            float temp = idealMin;
            idealMin = idealMax;
            idealMax = temp;
        }

        if (minAllowed > idealMin)
        {
            minAllowed = idealMin;
        }

        if (maxAllowed < idealMax)
        {
            maxAllowed = idealMax;
        }

        if (value < minAllowed || value > maxAllowed)
        {
            return 0f;
        }

        if (value >= idealMin && value <= idealMax)
        {
            return 1f;
        }

        if (value < idealMin)
        {
            if (Mathf.Approximately(minAllowed, idealMin))
            {
                return 1f;
            }

            return Clamp01((value - minAllowed) / (idealMin - minAllowed));
        }

        if (Mathf.Approximately(maxAllowed, idealMax))
        {
            return 1f;
        }

        return Clamp01((maxAllowed - value) / (maxAllowed - idealMax));
    }
}
