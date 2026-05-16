using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ReefMetrics
{
    [Header("Normalized environmental metrics")]
    [Range(0f, 1f)] public float depth;
    [Range(0f, 1f)] public float depthSuitability = 1f;
    [Range(0f, 1f)] public float temperature;
    [Range(0f, 1f)] public float temperatureSuitability = 1f;
    [Range(0f, 1f)] public float salinity;
    [Range(0f, 1f)] public float salinitySuitability = 1f;
    [Range(0f, 1f)] public float chlorophyll;
    [Range(0f, 1f)] public float chlorophyllSuitability = 1f;
    [Range(0f, 1f)] public float distanceToCoast;
    [Range(0f, 1f)] public float coastProximity = 1f;
    [Range(0f, 1f)] public float slope;
    [Range(0f, 1f)] public float slopeSuitability = 1f;
    [Range(0f, 1f)] public float rockySubstrate;
    [Range(0f, 1f)] public float sandySubstrate;
    [Range(0f, 1f)] public float substrateSuitability = 1f;
    [Range(0f, 1f)] public float seagrassPresence;
    [Range(0f, 1f)] public float reefAssociation;
    [Range(0f, 1f)] public float terrainComplexity;

    [Header("Normalized microhabitat metrics")]
    [Range(0f, 1f)] public float smallHoleDensity;
    [Range(0f, 1f)] public float mediumHoleDensity;
    [Range(0f, 1f)] public float largeCaveAvailability;
    [Range(0f, 1f)] public float shadowAvailability;
    [Range(0f, 1f)] public float lightExposure;
    [Range(0f, 1f)] public float mixedLightShadow;
    [Range(0f, 1f)] public float cleanCavities = 1f;
    [Range(0f, 1f)] public float sedimentCloggingRisk;
    [Range(0f, 1f)] public float surfaceRoughness;
    [Range(0f, 1f)] public float openSwimVolume;
    [Range(0f, 1f)] public float verticalRelief;
    [Range(0f, 1f)] public float reefEdgeComplexity;
    [Range(0f, 1f)] public float shelterAvailability;

    [Tooltip("Extra normalized 0..1 metrics for future reef-analysis features without changing JSON structure.")]
    public List<MetricValue> additionalMetrics = new List<MetricValue>();

    private static readonly Dictionary<string, Func<ReefMetrics, float>> BuiltInMetrics =
        new Dictionary<string, Func<ReefMetrics, float>>(StringComparer.OrdinalIgnoreCase)
        {
            { "depth", metrics => metrics.depth },
            { "depthSuitability", metrics => metrics.depthSuitability },
            { "temperature", metrics => metrics.temperature },
            { "temperatureSuitability", metrics => metrics.temperatureSuitability },
            { "salinity", metrics => metrics.salinity },
            { "salinitySuitability", metrics => metrics.salinitySuitability },
            { "chlorophyll", metrics => metrics.chlorophyll },
            { "chlorophyllSuitability", metrics => metrics.chlorophyllSuitability },
            { "distanceToCoast", metrics => metrics.distanceToCoast },
            { "coastProximity", metrics => metrics.coastProximity },
            { "slope", metrics => metrics.slope },
            { "slopeSuitability", metrics => metrics.slopeSuitability },
            { "rockySubstrate", metrics => metrics.rockySubstrate },
            { "sandySubstrate", metrics => metrics.sandySubstrate },
            { "substrateSuitability", metrics => metrics.substrateSuitability },
            { "seagrassPresence", metrics => metrics.seagrassPresence },
            { "reefAssociation", metrics => metrics.reefAssociation },
            { "terrainComplexity", metrics => metrics.terrainComplexity },
            { "smallHoleDensity", metrics => metrics.smallHoleDensity },
            { "mediumHoleDensity", metrics => metrics.mediumHoleDensity },
            { "largeCaveAvailability", metrics => metrics.largeCaveAvailability },
            { "shadowAvailability", metrics => metrics.shadowAvailability },
            { "lightExposure", metrics => metrics.lightExposure },
            { "mixedLightShadow", metrics => metrics.mixedLightShadow },
            { "cleanCavities", metrics => metrics.cleanCavities },
            { "sedimentCloggingRisk", metrics => metrics.sedimentCloggingRisk },
            { "surfaceRoughness", metrics => metrics.surfaceRoughness },
            { "openSwimVolume", metrics => metrics.openSwimVolume },
            { "verticalRelief", metrics => metrics.verticalRelief },
            { "reefEdgeComplexity", metrics => metrics.reefEdgeComplexity },
            { "shelterAvailability", metrics => metrics.shelterAvailability }
        };

    public bool TryGetMetricValue(string featureName, out float value)
    {
        value = 0f;

        if (string.IsNullOrWhiteSpace(featureName))
        {
            return false;
        }

        string key = featureName.Trim();
        if (BuiltInMetrics.TryGetValue(key, out Func<ReefMetrics, float> getter))
        {
            value = ReefMetricNormalizer.Clamp01(getter(this));
            return true;
        }

        if (additionalMetrics != null)
        {
            for (int i = 0; i < additionalMetrics.Count; i++)
            {
                MetricValue metric = additionalMetrics[i];
                if (metric != null && string.Equals(metric.feature, key, StringComparison.OrdinalIgnoreCase))
                {
                    value = ReefMetricNormalizer.Clamp01(metric.value);
                    return true;
                }
            }
        }

        return false;
    }

    public static ReefMetrics CreateSampleNormalizedReef()
    {
        return new ReefMetrics
        {
            depth = 0.18f,
            depthSuitability = 0.92f,
            temperature = 0.63f,
            temperatureSuitability = 0.82f,
            salinity = 0.72f,
            salinitySuitability = 0.85f,
            chlorophyll = 0.36f,
            chlorophyllSuitability = 0.65f,
            distanceToCoast = 0.22f,
            coastProximity = 0.78f,
            slope = 0.42f,
            slopeSuitability = 0.7f,
            rockySubstrate = 0.9f,
            sandySubstrate = 0.18f,
            substrateSuitability = 0.88f,
            seagrassPresence = 0.35f,
            reefAssociation = 0.86f,
            terrainComplexity = 0.74f,
            smallHoleDensity = 0.82f,
            mediumHoleDensity = 0.62f,
            largeCaveAvailability = 0.34f,
            shadowAvailability = 0.73f,
            lightExposure = 0.58f,
            mixedLightShadow = 0.69f,
            cleanCavities = 0.84f,
            sedimentCloggingRisk = 0.16f,
            surfaceRoughness = 0.76f,
            openSwimVolume = 0.67f,
            verticalRelief = 0.64f,
            reefEdgeComplexity = 0.72f,
            shelterAvailability = 0.8f
        };
    }
}

[Serializable]
public class MetricValue
{
    public string feature;
    [Range(0f, 1f)] public float value;
}
