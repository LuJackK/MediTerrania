using System.Collections.Generic;
using UnityEngine;

public static class HabitatSuitabilityScorer
{
    public const float DefaultSedimentationReductionPerRope = 0.12f;
    public const float MaxRopeSedimentationReduction = 0.75f;

    public static float ComputeSuitabilityScore(SpeciesSuitabilityConfig species, ReefMetrics reefMetrics)
    {
        return Mathf.Clamp01(ComputeSuitability(species, reefMetrics).finalScore);
    }

    public static float ComputeSuitabilityScore(
        SpeciesSuitabilityConfig species,
        ReefMetrics reefMetrics,
        int ropeCount,
        float sedimentationReductionPerRope = DefaultSedimentationReductionPerRope)
    {
        return Mathf.Clamp01(ComputeSuitability(
            species,
            reefMetrics,
            ropeCount,
            sedimentationReductionPerRope).finalScore);
    }

    public static SuitabilityResult ComputeSuitability(SpeciesSuitabilityConfig species, ReefMetrics reefMetrics)
    {
        if (species == null)
        {
            Debug.LogWarning("Cannot compute habitat suitability: species config is missing.");
            return CreateEmptyResult(null);
        }

        if (reefMetrics == null)
        {
            Debug.LogWarning($"Cannot compute habitat suitability for {species.scientificName}: reef metrics are missing.");
            return CreateEmptyResult(species);
        }

        float environmentScore = WeightedSum(species.trainedEnvironmentCoefficients, reefMetrics);
        float microhabitatScore = WeightedSum(species.researchedMicrohabitatCoefficients, reefMetrics);

        float environmentWeight = Mathf.Max(0f, species.environmentWeight);
        float microhabitatWeight = Mathf.Max(0f, species.microhabitatWeight);
        float totalBlendWeight = environmentWeight + microhabitatWeight;

        if (totalBlendWeight <= Mathf.Epsilon)
        {
            environmentWeight = 0.5f;
            microhabitatWeight = 0.5f;
        }
        else
        {
            environmentWeight /= totalBlendWeight;
            microhabitatWeight /= totalBlendWeight;
        }

        float weightedScore = Mathf.Clamp01(
            (environmentWeight * environmentScore) +
            (microhabitatWeight * microhabitatScore));
        float criticalMultiplier = ApplyCriticalMultiplier(species.criticalFactors, reefMetrics);
        float finalScore = Mathf.Clamp01(weightedScore * criticalMultiplier);

        return new SuitabilityResult
        {
            speciesId = species.speciesId,
            scientificName = species.scientificName,
            commonName = species.commonName,
            localName = species.localName,
            environmentScore = environmentScore,
            microhabitatScore = microhabitatScore,
            weightedScore = weightedScore,
            criticalMultiplier = criticalMultiplier,
            finalScore = finalScore,
            suitabilityClass = ClassifySuitability(finalScore)
        };
    }

    public static SuitabilityResult ComputeSuitability(
        SpeciesSuitabilityConfig species,
        ReefMetrics reefMetrics,
        int ropeCount,
        float sedimentationReductionPerRope = DefaultSedimentationReductionPerRope)
    {
        ReefMetrics sedimentAdjustedMetrics = ApplyRopeSedimentationReduction(
            reefMetrics,
            ropeCount,
            sedimentationReductionPerRope);
        return ComputeSuitability(species, sedimentAdjustedMetrics);
    }

    public static float ComputeRopeSedimentationReduction(
        int ropeCount,
        float sedimentationReductionPerRope = DefaultSedimentationReductionPerRope)
    {
        if (ropeCount <= 0 || sedimentationReductionPerRope <= 0f)
        {
            return 0f;
        }

        float rawReduction = ropeCount * sedimentationReductionPerRope;
        return Mathf.Clamp(rawReduction, 0f, MaxRopeSedimentationReduction);
    }

    public static ReefMetrics ApplyRopeSedimentationReduction(
        ReefMetrics reefMetrics,
        int ropeCount,
        float sedimentationReductionPerRope = DefaultSedimentationReductionPerRope)
    {
        float sedimentReduction = ComputeRopeSedimentationReduction(ropeCount, sedimentationReductionPerRope);
        if (reefMetrics == null || sedimentReduction <= Mathf.Epsilon)
        {
            return reefMetrics;
        }

        float remainingSedimentation = 1f - sedimentReduction;
        ReefMetrics adjustedMetrics = CopyReefMetrics(reefMetrics);

        adjustedMetrics.sedimentCloggingRisk = Mathf.Clamp01(reefMetrics.sedimentCloggingRisk * remainingSedimentation);
        adjustedMetrics.sandySubstrate = Mathf.Clamp01(reefMetrics.sandySubstrate * remainingSedimentation);
        adjustedMetrics.cleanCavities = Mathf.Clamp01(1f - ((1f - reefMetrics.cleanCavities) * remainingSedimentation));
        adjustedMetrics.substrateSuitability = Mathf.Clamp01(
            reefMetrics.substrateSuitability + ((1f - reefMetrics.substrateSuitability) * sedimentReduction));

        return adjustedMetrics;
    }

    public static float WeightedSum(List<FeatureWeight> coefficients, ReefMetrics reefMetrics)
    {
        if (coefficients == null || coefficients.Count == 0)
        {
            return 0f;
        }

        float weightedTotal = 0f;
        float minPossible = 0f;
        float maxPossible = 0f;
        float totalAbsoluteWeight = 0f;

        for (int i = 0; i < coefficients.Count; i++)
        {
            FeatureWeight coefficient = coefficients[i];
            if (coefficient == null || string.IsNullOrWhiteSpace(coefficient.feature))
            {
                continue;
            }

            if (Mathf.Approximately(coefficient.weight, 0f))
            {
                continue;
            }

            if (!TryGetMetricValue(coefficient.feature, reefMetrics, out float metricValue))
            {
                Debug.LogWarning($"Habitat suitability metric '{coefficient.feature}' is missing. Skipping this feature.");
                continue;
            }

            float clampedMetric = Mathf.Clamp01(metricValue);
            float weight = coefficient.weight;

            weightedTotal += weight * clampedMetric;
            totalAbsoluteWeight += Mathf.Abs(weight);

            if (weight >= 0f)
            {
                maxPossible += weight;
            }
            else
            {
                minPossible += weight;
            }
        }

        if (totalAbsoluteWeight <= Mathf.Epsilon)
        {
            return 0f;
        }

        float normalizedWeightedTotal = weightedTotal / totalAbsoluteWeight;
        float normalizedMin = minPossible / totalAbsoluteWeight;
        float normalizedMax = maxPossible / totalAbsoluteWeight;

        if (Mathf.Approximately(normalizedMin, normalizedMax))
        {
            return Mathf.Clamp01(normalizedWeightedTotal);
        }

        // Negative coefficients mean high values of that metric reduce suitability.
        // Remapping by the reachable min/max keeps mixed-sign models in a final 0..1 score.
        return Mathf.Clamp01(Mathf.InverseLerp(normalizedMin, normalizedMax, normalizedWeightedTotal));
    }

    public static float GetMetricValue(string featureName, ReefMetrics reefMetrics)
    {
        if (TryGetMetricValue(featureName, reefMetrics, out float metricValue))
        {
            return metricValue;
        }

        Debug.LogWarning($"Habitat suitability metric '{featureName}' is missing. Returning 0.");
        return 0f;
    }

    public static float ApplyCriticalMultiplier(List<string> criticalFactors, ReefMetrics reefMetrics)
    {
        if (criticalFactors == null || criticalFactors.Count == 0)
        {
            return 1f;
        }

        float multiplier = 1f;
        bool foundAnyCriticalFactor = false;

        for (int i = 0; i < criticalFactors.Count; i++)
        {
            string factor = criticalFactors[i];
            if (string.IsNullOrWhiteSpace(factor))
            {
                continue;
            }

            if (!TryGetMetricValue(factor, reefMetrics, out float factorScore))
            {
                Debug.LogWarning($"Critical habitat factor '{factor}' is missing. Skipping this limiter.");
                continue;
            }

            foundAnyCriticalFactor = true;
            multiplier = Mathf.Min(multiplier, Mathf.Clamp01(factorScore));
        }

        return foundAnyCriticalFactor ? Mathf.Clamp01(multiplier) : 1f;
    }

    public static string ClassifySuitability(float score)
    {
        float clampedScore = Mathf.Clamp01(score);

        if (clampedScore < 0.25f)
        {
            return "Poor suitability";
        }

        if (clampedScore < 0.5f)
        {
            return "Low suitability";
        }

        if (clampedScore < 0.75f)
        {
            return "Moderate suitability";
        }

        return "High suitability";
    }

    private static bool TryGetMetricValue(string featureName, ReefMetrics reefMetrics, out float metricValue)
    {
        metricValue = 0f;

        if (reefMetrics == null)
        {
            return false;
        }

        return reefMetrics.TryGetMetricValue(featureName, out metricValue);
    }

    private static ReefMetrics CopyReefMetrics(ReefMetrics source)
    {
        ReefMetrics copy = new ReefMetrics
        {
            depth = source.depth,
            depthSuitability = source.depthSuitability,
            temperature = source.temperature,
            temperatureSuitability = source.temperatureSuitability,
            salinity = source.salinity,
            salinitySuitability = source.salinitySuitability,
            chlorophyll = source.chlorophyll,
            chlorophyllSuitability = source.chlorophyllSuitability,
            distanceToCoast = source.distanceToCoast,
            coastProximity = source.coastProximity,
            slope = source.slope,
            slopeSuitability = source.slopeSuitability,
            rockySubstrate = source.rockySubstrate,
            sandySubstrate = source.sandySubstrate,
            substrateSuitability = source.substrateSuitability,
            seagrassPresence = source.seagrassPresence,
            reefAssociation = source.reefAssociation,
            terrainComplexity = source.terrainComplexity,
            smallHoleDensity = source.smallHoleDensity,
            mediumHoleDensity = source.mediumHoleDensity,
            largeCaveAvailability = source.largeCaveAvailability,
            shadowAvailability = source.shadowAvailability,
            lightExposure = source.lightExposure,
            mixedLightShadow = source.mixedLightShadow,
            cleanCavities = source.cleanCavities,
            sedimentCloggingRisk = source.sedimentCloggingRisk,
            surfaceRoughness = source.surfaceRoughness,
            openSwimVolume = source.openSwimVolume,
            verticalRelief = source.verticalRelief,
            reefEdgeComplexity = source.reefEdgeComplexity,
            shelterAvailability = source.shelterAvailability
        };

        if (source.additionalMetrics != null)
        {
            copy.additionalMetrics = new List<MetricValue>(source.additionalMetrics.Count);
            for (int i = 0; i < source.additionalMetrics.Count; i++)
            {
                MetricValue sourceMetric = source.additionalMetrics[i];
                copy.additionalMetrics.Add(sourceMetric == null
                    ? null
                    : new MetricValue
                    {
                        feature = sourceMetric.feature,
                        value = sourceMetric.value
                    });
            }
        }

        return copy;
    }

    private static SuitabilityResult CreateEmptyResult(SpeciesSuitabilityConfig species)
    {
        string speciesId = species != null ? species.speciesId : string.Empty;
        string scientificName = species != null ? species.scientificName : string.Empty;
        string commonName = species != null ? species.commonName : string.Empty;
        string localName = species != null ? species.localName : string.Empty;

        return new SuitabilityResult
        {
            speciesId = speciesId,
            scientificName = scientificName,
            commonName = commonName,
            localName = localName,
            environmentScore = 0f,
            microhabitatScore = 0f,
            weightedScore = 0f,
            criticalMultiplier = 0f,
            finalScore = 0f,
            suitabilityClass = ClassifySuitability(0f)
        };
    }
}
