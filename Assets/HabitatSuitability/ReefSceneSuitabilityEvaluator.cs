using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public sealed class ReefSceneSuitabilityEvaluator : MonoBehaviour
{
    public const string DefaultMappingConfigFileName = "reef_scene_attribute_mapping.json";

    [SerializeField]
    private string speciesConfigFileName = SpeciesSuitabilityLoader.DefaultConfigFileName;

    [SerializeField]
    private string mappingConfigFileName = DefaultMappingConfigFileName;

    [SerializeField]
    private bool logOnStart;

    [Header("Fallback scene values")]
    [SerializeField] private float fallbackDepthMeters = 16f;
    [SerializeField] private float fallbackTemperatureCelsius = 20f;

    private void Start()
    {
        if (logOnStart)
        {
            LogCurrentSceneScores();
        }
    }

    [ContextMenu("Score Current Scene For All Species")]
    public void LogCurrentSceneScores()
    {
        List<SuitabilityResult> results = ScoreCurrentSceneForAllSpecies();
        if (results.Count == 0)
        {
            Debug.LogWarning("No current-scene habitat suitability scores were produced.");
            return;
        }

        Debug.Log("--- Current reef scene suitability scores ---");
        for (int i = 0; i < results.Count; i++)
        {
            SuitabilityResult result = results[i];
            Debug.Log(
                $"{result.scientificName} ({result.commonName}): {result.finalScore:0.000} " +
                $"| {result.suitabilityClass}");
        }
    }

    public List<SuitabilityResult> ScoreCurrentSceneForAllSpecies()
    {
        ReefSceneAttributes sceneAttributes = CaptureCurrentSceneAttributes(
            fallbackDepthMeters,
            fallbackTemperatureCelsius);
        return ScoreSceneAttributesForAllSpecies(sceneAttributes);
    }

    public List<SuitabilityResult> ScoreSceneAttributesForAllSpecies(ReefSceneAttributes sceneAttributes)
    {
        SpeciesSuitabilityDatabase database = SpeciesSuitabilityLoader.LoadFromStreamingAssets(speciesConfigFileName);
        ReefSceneAttributeMappingConfig mappingConfig =
            ReefSceneAttributeMappingLoader.LoadFromStreamingAssets(mappingConfigFileName);

        return ScoreSceneAttributesForAllSpecies(sceneAttributes, database, mappingConfig);
    }

    public static List<SuitabilityResult> ScoreCurrentScene()
    {
        ReefSceneAttributes sceneAttributes = CaptureCurrentSceneAttributes();
        SpeciesSuitabilityDatabase database = SpeciesSuitabilityLoader.LoadFromStreamingAssets();
        ReefSceneAttributeMappingConfig mappingConfig =
            ReefSceneAttributeMappingLoader.LoadFromStreamingAssets(DefaultMappingConfigFileName);

        return ScoreSceneAttributesForAllSpecies(sceneAttributes, database, mappingConfig);
    }

    public static List<SuitabilityResult> ScoreSceneAttributesForAllSpecies(
        ReefSceneAttributes sceneAttributes,
        SpeciesSuitabilityDatabase database,
        ReefSceneAttributeMappingConfig mappingConfig)
    {
        List<SuitabilityResult> results = new();
        if (database == null || database.species == null || database.species.Count == 0)
        {
            Debug.LogWarning("No fish species loaded for current-scene suitability scoring.");
            return results;
        }

        ReefSceneAttributes safeAttributes = sceneAttributes ?? new ReefSceneAttributes();
        ReefSceneAttributeMappingConfig safeMapping = mappingConfig ?? ReefSceneAttributeMappingConfig.CreateDefault();
        ReefMetrics reefMetrics = ReefSceneAttributeMapper.BuildMetrics(safeAttributes, safeMapping);

        for (int i = 0; i < database.species.Count; i++)
        {
            SpeciesSuitabilityConfig species = database.species[i];
            if (species == null)
            {
                continue;
            }

            results.Add(HabitatSuitabilityScorer.ComputeSuitability(
                species,
                reefMetrics,
                safeAttributes.ropeCount,
                safeMapping.ropeSedimentationReductionPerRope));
        }

        return results;
    }

    public static ReefSceneAttributes CaptureCurrentSceneAttributes(
        float fallbackDepthMeters = 16f,
        float fallbackTemperatureCelsius = 20f)
    {
        HabitatRopeController habitatController = FindFirstObjectByType<HabitatRopeController>();
        AnchorDrag depthController = FindFirstObjectByType<AnchorDrag>();
        SeaTemperatureController temperatureController = FindFirstObjectByType<SeaTemperatureController>();

        return new ReefSceneAttributes
        {
            terrainBaseId = habitatController != null ? habitatController.SelectedTerrainBaseId : "terrain_1",
            materialId = habitatController != null ? habitatController.SelectedMaterialId : "stone",
            ropeCount = habitatController != null ? habitatController.VisibleRopeCount : 0,
            depthMeters = depthController != null ? depthController.CurrentDepthMeters : fallbackDepthMeters,
            temperatureCelsius = temperatureController != null
                ? temperatureController.seaTemperature
                : fallbackTemperatureCelsius
        };
    }
}

public static class ReefSceneAttributeMapper
{
    public static ReefMetrics BuildMetrics(
        ReefSceneAttributes sceneAttributes,
        ReefSceneAttributeMappingConfig mappingConfig)
    {
        ReefSceneAttributes attributes = sceneAttributes ?? new ReefSceneAttributes();
        ReefSceneAttributeMappingConfig config = mappingConfig ?? ReefSceneAttributeMappingConfig.CreateDefault();
        ReefTerrainBaseMapping terrain = config.FindTerrainBase(attributes.terrainBaseId);
        ReefMaterialMapping material = config.FindMaterial(attributes.materialId);

        float rockySubstrate = ReefMetricNormalizer.Clamp01(material.rockySubstrate);
        float sandySubstrate = ReefMetricNormalizer.Clamp01(material.sandySubstrate);
        float seagrassPresence = ReefMetricNormalizer.Clamp01(config.defaults.seagrassPresence);
        float substrateSuitability = material.useExplicitSubstrateSuitability
            ? ReefMetricNormalizer.Clamp01(material.substrateSuitability)
            : Mathf.Max(rockySubstrate, seagrassPresence * 0.7f, sandySubstrate * 0.35f);
        bool terrainNeedsFallback = IsLikelyUnconfiguredTerrain(terrain);

        float terrainComplexity = terrainNeedsFallback
            ? Mathf.Lerp(0.32f, 0.74f, rockySubstrate)
            : terrain.terrainComplexity;
        float reefAssociation = terrainNeedsFallback
            ? Mathf.Max(0.38f, terrainComplexity * 0.82f)
            : terrain.reefAssociation;
        float shelterAvailability = terrainNeedsFallback
            ? Mathf.Max(0.34f, terrainComplexity * 0.78f)
            : terrain.shelterAvailability;
        float smallHoleDensity = terrainNeedsFallback
            ? Mathf.Max(0.28f, shelterAvailability * 0.82f)
            : terrain.smallHoleDensity;
        float mediumHoleDensity = terrainNeedsFallback
            ? Mathf.Max(0.2f, smallHoleDensity * 0.7f)
            : terrain.mediumHoleDensity;
        float largeCaveAvailability = terrainNeedsFallback
            ? Mathf.Max(0.12f, shelterAvailability * 0.36f)
            : terrain.largeCaveAvailability;
        float surfaceRoughness = terrainNeedsFallback
            ? Mathf.Max(0.3f, terrainComplexity * 0.88f)
            : terrain.surfaceRoughness;
        float verticalRelief = terrainNeedsFallback
            ? Mathf.Max(0.22f, terrainComplexity * 0.74f)
            : terrain.verticalRelief;
        float reefEdgeComplexity = terrainNeedsFallback
            ? Mathf.Max(0.22f, terrainComplexity * 0.76f)
            : terrain.reefEdgeComplexity;
        float openSwimVolume = terrainNeedsFallback
            ? Mathf.Max(0.35f, 1f - (terrainComplexity * 0.25f))
            : terrain.openSwimVolume;
        float shadowAvailability = terrainNeedsFallback
            ? Mathf.Max(0.25f, shelterAvailability * 0.72f)
            : terrain.shadowAvailability;
        float lightExposure = terrainNeedsFallback
            ? Mathf.Clamp01(0.62f - (terrainComplexity * 0.18f))
            : terrain.lightExposure;
        float mixedLightShadow = terrainNeedsFallback
            ? Mathf.Max(0.3f, (shadowAvailability + (1f - lightExposure)) * 0.5f)
            : terrain.mixedLightShadow;
        float cleanCavities = terrainNeedsFallback
            ? Mathf.Max(0.62f, 1f - Mathf.Max(terrain.sedimentCloggingRisk, material.sedimentCloggingRisk))
            : terrain.cleanCavities;
        float sedimentCloggingRisk = Mathf.Max(
            terrainNeedsFallback ? 0f : terrain.sedimentCloggingRisk,
            material.sedimentCloggingRisk);

        return new ReefMetrics
        {
            depth = ReefMetricNormalizer.NormalizeRange(
                attributes.depthMeters,
                config.depth.normalizeMinMeters,
                config.depth.normalizeMaxMeters),
            depthSuitability = ReefMetricNormalizer.RangeSuitability(
                attributes.depthMeters,
                config.depth.idealMinMeters,
                config.depth.idealMaxMeters,
                config.depth.allowedMinMeters,
                config.depth.allowedMaxMeters),
            temperature = ReefMetricNormalizer.NormalizeRange(
                attributes.temperatureCelsius,
                config.temperature.normalizeMinCelsius,
                config.temperature.normalizeMaxCelsius),
            temperatureSuitability = ReefMetricNormalizer.RangeSuitability(
                attributes.temperatureCelsius,
                config.temperature.idealMinCelsius,
                config.temperature.idealMaxCelsius,
                config.temperature.allowedMinCelsius,
                config.temperature.allowedMaxCelsius),
            salinity = ReefMetricNormalizer.NormalizeRange(config.defaults.salinityPsu, 34f, 40f),
            salinitySuitability = ReefMetricNormalizer.RangeSuitability(config.defaults.salinityPsu, 36.5f, 39f, 34f, 41f),
            chlorophyll = ReefMetricNormalizer.NormalizeRange(config.defaults.chlorophyllMgM3, 0f, 1f),
            chlorophyllSuitability = ReefMetricNormalizer.RangeSuitability(config.defaults.chlorophyllMgM3, 0.05f, 0.45f, 0f, 1.2f),
            distanceToCoast = ReefMetricNormalizer.NormalizeRange(config.defaults.distanceToCoastKm, 0f, 20f),
            coastProximity = ReefMetricNormalizer.NormalizeInverse(config.defaults.distanceToCoastKm, 0f, 20f),
            slope = ReefMetricNormalizer.NormalizeRange(terrain.slopeDegrees, 0f, 45f),
            slopeSuitability = ReefMetricNormalizer.RangeSuitability(terrain.slopeDegrees, 3f, 24f, 0f, 45f),
            rockySubstrate = rockySubstrate,
            sandySubstrate = sandySubstrate,
            substrateSuitability = substrateSuitability,
            seagrassPresence = seagrassPresence,
            reefAssociation = reefAssociation,
            terrainComplexity = terrainComplexity,
            smallHoleDensity = smallHoleDensity,
            mediumHoleDensity = mediumHoleDensity,
            largeCaveAvailability = largeCaveAvailability,
            shadowAvailability = shadowAvailability,
            lightExposure = lightExposure,
            mixedLightShadow = mixedLightShadow,
            cleanCavities = cleanCavities,
            sedimentCloggingRisk = sedimentCloggingRisk,
            surfaceRoughness = surfaceRoughness,
            openSwimVolume = openSwimVolume,
            verticalRelief = verticalRelief,
            reefEdgeComplexity = reefEdgeComplexity,
            shelterAvailability = shelterAvailability,
            additionalMetrics = new List<MetricValue>
            {
                new() { feature = "ironContent", value = material.ironContent },
                new() { feature = "sandContent", value = material.sandContent },
                new() { feature = "rockContent", value = material.rockContent }
            }
        };
    }

    // If a terrain base is still left at placeholder zeros, use live-derived defaults so scores stay informative.
    private static bool IsLikelyUnconfiguredTerrain(ReefTerrainBaseMapping terrain)
    {
        if (terrain == null)
        {
            return true;
        }

        float signal =
            Mathf.Abs(terrain.terrainComplexity) +
            Mathf.Abs(terrain.smallHoleDensity) +
            Mathf.Abs(terrain.mediumHoleDensity) +
            Mathf.Abs(terrain.largeCaveAvailability) +
            Mathf.Abs(terrain.shadowAvailability) +
            Mathf.Abs(terrain.lightExposure) +
            Mathf.Abs(terrain.mixedLightShadow) +
            Mathf.Abs(terrain.surfaceRoughness) +
            Mathf.Abs(terrain.openSwimVolume) +
            Mathf.Abs(terrain.verticalRelief) +
            Mathf.Abs(terrain.reefEdgeComplexity) +
            Mathf.Abs(terrain.shelterAvailability) +
            Mathf.Abs(terrain.reefAssociation) +
            Mathf.Abs(terrain.slopeDegrees);

        return signal <= 0.0001f;
    }
}

public static class ReefSceneAttributeMappingLoader
{
    public static ReefSceneAttributeMappingConfig LoadFromStreamingAssets(string fileName)
    {
        string safeFileName = string.IsNullOrWhiteSpace(fileName)
            ? ReefSceneSuitabilityEvaluator.DefaultMappingConfigFileName
            : fileName;
        string path = Path.Combine(Application.streamingAssetsPath, safeFileName);

        if (!File.Exists(path))
        {
            Debug.LogWarning(
                $"Reef scene attribute mapping config not found at '{path}'. Using built-in defaults.");
            return ReefSceneAttributeMappingConfig.CreateDefault();
        }

        string json = File.ReadAllText(path);
        ReefSceneAttributeMappingConfig config = JsonUtility.FromJson<ReefSceneAttributeMappingConfig>(json);
        if (config == null)
        {
            Debug.LogWarning("Reef scene attribute mapping JSON could not be parsed. Using built-in defaults.");
            return ReefSceneAttributeMappingConfig.CreateDefault();
        }

        config.EnsureDefaults();
        return config;
    }
}

[Serializable]
public class ReefSceneAttributes
{
    public string terrainBaseId = "terrain_1";
    public string materialId = "stone";
    public int ropeCount;
    public float depthMeters = 16f;
    public float temperatureCelsius = 20f;
}

[Serializable]
public class ReefSceneAttributeMappingConfig
{
    public ReefDepthMapping depth = new();
    public ReefTemperatureMapping temperature = new();
    public ReefEnvironmentDefaults defaults = new();
    public float ropeSedimentationReductionPerRope = HabitatSuitabilityScorer.DefaultSedimentationReductionPerRope;
    public List<ReefMaterialMapping> materials = new();
    public List<ReefTerrainBaseMapping> terrainBases = new();

    public static ReefSceneAttributeMappingConfig CreateDefault()
    {
        ReefSceneAttributeMappingConfig config = new();
        config.EnsureDefaults();
        return config;
    }

    public void EnsureDefaults()
    {
        depth ??= new ReefDepthMapping();
        temperature ??= new ReefTemperatureMapping();
        defaults ??= new ReefEnvironmentDefaults();
        materials ??= new List<ReefMaterialMapping>();
        terrainBases ??= new List<ReefTerrainBaseMapping>();

        if (materials.Count == 0)
        {
            materials.Add(new ReefMaterialMapping
            {
                id = "iron",
                ironContent = 1f,
                sandContent = 0f,
                rockContent = 0.2f,
                rockySubstrate = 0.2f,
                sandySubstrate = 0f,
                sedimentCloggingRisk = 0.05f
            });
            materials.Add(new ReefMaterialMapping
            {
                id = "sand",
                ironContent = 0f,
                sandContent = 1f,
                rockContent = 0f,
                rockySubstrate = 0f,
                sandySubstrate = 1f,
                sedimentCloggingRisk = 0.55f
            });
            materials.Add(new ReefMaterialMapping
            {
                id = "stone",
                ironContent = 0f,
                sandContent = 0.1f,
                rockContent = 1f,
                rockySubstrate = 1f,
                sandySubstrate = 0.1f,
                sedimentCloggingRisk = 0.08f
            });
        }

        if (terrainBases.Count == 0)
        {
            terrainBases.Add(new ReefTerrainBaseMapping { id = "terrain_1" });
            terrainBases.Add(new ReefTerrainBaseMapping { id = "terrain_2" });
            terrainBases.Add(new ReefTerrainBaseMapping { id = "terrain_3" });
        }
    }

    public ReefMaterialMapping FindMaterial(string id)
    {
        EnsureDefaults();
        string safeId = string.IsNullOrWhiteSpace(id) ? "stone" : id.Trim();

        for (int i = 0; i < materials.Count; i++)
        {
            ReefMaterialMapping material = materials[i];
            if (material != null && string.Equals(material.id, safeId, StringComparison.OrdinalIgnoreCase))
            {
                return material;
            }
        }

        Debug.LogWarning($"Material mapping '{safeId}' was not found. Falling back to the first material mapping.");
        return materials[0];
    }

    public ReefTerrainBaseMapping FindTerrainBase(string id)
    {
        EnsureDefaults();
        string safeId = string.IsNullOrWhiteSpace(id) ? "terrain_1" : id.Trim();

        for (int i = 0; i < terrainBases.Count; i++)
        {
            ReefTerrainBaseMapping terrain = terrainBases[i];
            if (terrain != null && string.Equals(terrain.id, safeId, StringComparison.OrdinalIgnoreCase))
            {
                return terrain;
            }
        }

        Debug.LogWarning($"Terrain base mapping '{safeId}' was not found. Falling back to the first terrain mapping.");
        return terrainBases[0];
    }
}

[Serializable]
public class ReefDepthMapping
{
    public float normalizeMinMeters = 0f;
    public float normalizeMaxMeters = 60f;
    public float idealMinMeters = 8f;
    public float idealMaxMeters = 24f;
    public float allowedMinMeters = 2f;
    public float allowedMaxMeters = 42f;
}

[Serializable]
public class ReefTemperatureMapping
{
    public float normalizeMinCelsius = 12f;
    public float normalizeMaxCelsius = 28f;
    public float idealMinCelsius = 18f;
    public float idealMaxCelsius = 24f;
    public float allowedMinCelsius = 12f;
    public float allowedMaxCelsius = 29f;
}

[Serializable]
public class ReefEnvironmentDefaults
{
    public float salinityPsu = 38f;
    public float chlorophyllMgM3 = 0.2f;
    public float distanceToCoastKm = 0.5f;
    [Range(0f, 1f)] public float seagrassPresence = 0.2f;
}

[Serializable]
public class ReefMaterialMapping
{
    public string id;
    [Range(0f, 1f)] public float ironContent;
    [Range(0f, 1f)] public float sandContent;
    [Range(0f, 1f)] public float rockContent;
    [Range(0f, 1f)] public float rockySubstrate;
    [Range(0f, 1f)] public float sandySubstrate;
    public bool useExplicitSubstrateSuitability;
    [Range(0f, 1f)] public float substrateSuitability;
    [Range(0f, 1f)] public float sedimentCloggingRisk;
}

[Serializable]
public class ReefTerrainBaseMapping
{
    public string id;
    public float slopeDegrees;
    [Range(0f, 1f)] public float reefAssociation;
    [Range(0f, 1f)] public float terrainComplexity;
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
}
