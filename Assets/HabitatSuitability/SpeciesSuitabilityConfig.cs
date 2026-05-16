using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SpeciesSuitabilityDatabase
{
    public List<SpeciesSuitabilityConfig> species = new List<SpeciesSuitabilityConfig>();

    public SpeciesSuitabilityConfig FindSpecies(string speciesId)
    {
        if (string.IsNullOrWhiteSpace(speciesId) || species == null)
        {
            return null;
        }

        for (int i = 0; i < species.Count; i++)
        {
            SpeciesSuitabilityConfig config = species[i];
            if (config != null && string.Equals(config.speciesId, speciesId, StringComparison.OrdinalIgnoreCase))
            {
                return config;
            }
        }

        return null;
    }
}

[Serializable]
public class SpeciesSuitabilityConfig
{
    public string speciesId;
    public string scientificName;
    public string commonName;
    public string localName;

    [Range(0f, 1f)]
    public float environmentWeight = 0.5f;

    [Range(0f, 1f)]
    public float microhabitatWeight = 0.5f;

    // ML/big-data model outputs. Keep these separate from researched microhabitat preferences.
    public List<FeatureWeight> trainedEnvironmentCoefficients = new List<FeatureWeight>();

    // Literature/human-curated habitat preferences. These can be edited without retraining.
    public List<FeatureWeight> researchedMicrohabitatCoefficients = new List<FeatureWeight>();

    // Critical factors limit impossible or near-impossible habitats even when other features score well.
    public List<string> criticalFactors = new List<string>();
}

[Serializable]
public class FeatureWeight
{
    public string feature;
    public float weight;
}

[Serializable]
public class SuitabilityResult
{
    public string speciesId;
    public string scientificName;
    public string commonName;
    public string localName;
    public float environmentScore;
    public float microhabitatScore;
    public float weightedScore;
    public float criticalMultiplier;
    public float finalScore;
    public string suitabilityClass;
}
