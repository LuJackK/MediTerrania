using UnityEngine;

public class HabitatSuitabilityTestCase : MonoBehaviour
{
    [Header("Fish input")]
    [Tooltip("Use a speciesId from species_suitability_config.json, or leave empty to test every fish.")]
    [SerializeField]
    private string speciesId = "parablennius_rouxi";

    [Header("Raw terrain input")]
    [SerializeField] private float depthMeters = 16f;
    [SerializeField] private float temperatureCelsius = 20f;
    [SerializeField] private float salinityPsu = 38f;
    [SerializeField] private float chlorophyllMgM3 = 0.2f;
    [SerializeField] private float distanceToCoastKm = 0.5f;
    [SerializeField] private float slopeDegrees = 12f;
    [SerializeField] private float terrainComplexity01 = 0.78f;

    [Header("Normalized terrain/microhabitat input")]
    [Range(0f, 1f)] [SerializeField] private float rockySubstrate = 0.9f;
    [Range(0f, 1f)] [SerializeField] private float sandySubstrate = 0.1f;
    [Range(0f, 1f)] [SerializeField] private float seagrassPresence = 0.2f;
    [Range(0f, 1f)] [SerializeField] private float reefAssociation = 0.85f;
    [Range(0f, 1f)] [SerializeField] private float smallHoleDensity = 0.85f;
    [Range(0f, 1f)] [SerializeField] private float mediumHoleDensity = 0.55f;
    [Range(0f, 1f)] [SerializeField] private float largeCaveAvailability = 0.25f;
    [Range(0f, 1f)] [SerializeField] private float shadowAvailability = 0.75f;
    [Range(0f, 1f)] [SerializeField] private float lightExposure = 0.55f;
    [Range(0f, 1f)] [SerializeField] private float mixedLightShadow = 0.7f;
    [Range(0f, 1f)] [SerializeField] private float cleanCavities = 0.86f;
    [Range(0f, 1f)] [SerializeField] private float sedimentCloggingRisk = 0.14f;
    [Min(0)] [SerializeField] private int ropeCount;
    [Range(0f, 0.25f)] [SerializeField] private float sedimentationReductionPerRope =
        HabitatSuitabilityScorer.DefaultSedimentationReductionPerRope;
    [Range(0f, 1f)] [SerializeField] private float surfaceRoughness = 0.8f;
    [Range(0f, 1f)] [SerializeField] private float openSwimVolume = 0.5f;
    [Range(0f, 1f)] [SerializeField] private float verticalRelief = 0.65f;
    [Range(0f, 1f)] [SerializeField] private float reefEdgeComplexity = 0.72f;
    [Range(0f, 1f)] [SerializeField] private float shelterAvailability = 0.82f;

    [SerializeField]
    private bool runOnStart = true;

    private void Start()
    {
        if (runOnStart)
        {
            RunCurrentInputTest();
            RunContrastingPresetTest();
        }
    }

    [ContextMenu("Run Current Input Test")]
    public void RunCurrentInputTest()
    {
        ReefMetrics reefMetrics = BuildReefMetricsFromInspector();
        RunScenario("Inspector terrain input", reefMetrics);
    }

    [ContextMenu("Run Contrasting Preset Test")]
    public void RunContrastingPresetTest()
    {
        RunScenario("Good shallow rocky reef", BuildGoodRockyReef());
        RunScenario("Poor deep sandy clogged reef", BuildPoorDeepSandyReef());
    }

    private void RunScenario(string scenarioName, ReefMetrics reefMetrics)
    {
        SpeciesSuitabilityDatabase database = SpeciesSuitabilityLoader.LoadFromStreamingAssets();
        if (database.species == null || database.species.Count == 0)
        {
            Debug.LogWarning("No fish species loaded for habitat suitability test.");
            return;
        }

        Debug.Log($"--- Habitat suitability test: {scenarioName} ---");

        if (!string.IsNullOrWhiteSpace(speciesId))
        {
            SpeciesSuitabilityConfig species = database.FindSpecies(speciesId);
            if (species == null)
            {
                Debug.LogWarning($"Fish speciesId '{speciesId}' was not found in the suitability JSON.");
                return;
            }

            LogResult(species, reefMetrics, ropeCount, sedimentationReductionPerRope);
            return;
        }

        for (int i = 0; i < database.species.Count; i++)
        {
            LogResult(database.species[i], reefMetrics, ropeCount, sedimentationReductionPerRope);
        }
    }

    private static void LogResult(
        SpeciesSuitabilityConfig species,
        ReefMetrics reefMetrics,
        int ropeCount,
        float sedimentationReductionPerRope)
    {
        SuitabilityResult result = HabitatSuitabilityScorer.ComputeSuitability(
            species,
            reefMetrics,
            ropeCount,
            sedimentationReductionPerRope);
        float sedimentReduction = HabitatSuitabilityScorer.ComputeRopeSedimentationReduction(
            ropeCount,
            sedimentationReductionPerRope);
        Debug.Log(
            $"{result.scientificName} ({result.commonName}): {result.finalScore:0.000} " +
            $"| ropes {ropeCount}, sediment reduction {sedimentReduction:0.00}");
    }

    private ReefMetrics BuildReefMetricsFromInspector()
    {
        float substrateSuitability = Mathf.Max(rockySubstrate, seagrassPresence * 0.7f, sandySubstrate * 0.35f);

        return new ReefMetrics
        {
            depth = ReefMetricNormalizer.NormalizeRange(depthMeters, 0f, 60f),
            depthSuitability = ReefMetricNormalizer.RangeSuitability(depthMeters, 8f, 24f, 2f, 42f),
            temperature = ReefMetricNormalizer.NormalizeRange(temperatureCelsius, 12f, 28f),
            temperatureSuitability = ReefMetricNormalizer.RangeSuitability(temperatureCelsius, 18f, 24f, 12f, 29f),
            salinity = ReefMetricNormalizer.NormalizeRange(salinityPsu, 34f, 40f),
            salinitySuitability = ReefMetricNormalizer.RangeSuitability(salinityPsu, 36.5f, 39f, 34f, 41f),
            chlorophyll = ReefMetricNormalizer.NormalizeRange(chlorophyllMgM3, 0f, 1f),
            chlorophyllSuitability = ReefMetricNormalizer.RangeSuitability(chlorophyllMgM3, 0.05f, 0.45f, 0f, 1.2f),
            distanceToCoast = ReefMetricNormalizer.NormalizeRange(distanceToCoastKm, 0f, 20f),
            coastProximity = ReefMetricNormalizer.NormalizeInverse(distanceToCoastKm, 0f, 20f),
            slope = ReefMetricNormalizer.NormalizeRange(slopeDegrees, 0f, 45f),
            slopeSuitability = ReefMetricNormalizer.RangeSuitability(slopeDegrees, 3f, 24f, 0f, 45f),
            rockySubstrate = rockySubstrate,
            sandySubstrate = sandySubstrate,
            substrateSuitability = substrateSuitability,
            seagrassPresence = seagrassPresence,
            reefAssociation = reefAssociation,
            terrainComplexity = terrainComplexity01,
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
            shelterAvailability = shelterAvailability
        };
    }

    private static ReefMetrics BuildGoodRockyReef()
    {
        return new ReefMetrics
        {
            depth = ReefMetricNormalizer.NormalizeRange(14f, 0f, 60f),
            depthSuitability = ReefMetricNormalizer.RangeSuitability(14f, 8f, 24f, 2f, 42f),
            temperature = ReefMetricNormalizer.NormalizeRange(20f, 12f, 28f),
            temperatureSuitability = ReefMetricNormalizer.RangeSuitability(20f, 18f, 24f, 12f, 29f),
            salinity = ReefMetricNormalizer.NormalizeRange(38f, 34f, 40f),
            salinitySuitability = ReefMetricNormalizer.RangeSuitability(38f, 36.5f, 39f, 34f, 41f),
            chlorophyll = ReefMetricNormalizer.NormalizeRange(0.2f, 0f, 1f),
            chlorophyllSuitability = ReefMetricNormalizer.RangeSuitability(0.2f, 0.05f, 0.45f, 0f, 1.2f),
            distanceToCoast = ReefMetricNormalizer.NormalizeRange(0.5f, 0f, 20f),
            coastProximity = ReefMetricNormalizer.NormalizeInverse(0.5f, 0f, 20f),
            slope = ReefMetricNormalizer.NormalizeRange(14f, 0f, 45f),
            slopeSuitability = ReefMetricNormalizer.RangeSuitability(14f, 3f, 24f, 0f, 45f),
            rockySubstrate = 0.92f,
            sandySubstrate = 0.08f,
            substrateSuitability = 0.92f,
            seagrassPresence = 0.25f,
            reefAssociation = 0.9f,
            terrainComplexity = 0.8f,
            smallHoleDensity = 0.88f,
            mediumHoleDensity = 0.6f,
            largeCaveAvailability = 0.28f,
            shadowAvailability = 0.78f,
            lightExposure = 0.55f,
            mixedLightShadow = 0.72f,
            cleanCavities = 0.88f,
            sedimentCloggingRisk = 0.12f,
            surfaceRoughness = 0.82f,
            openSwimVolume = 0.58f,
            verticalRelief = 0.68f,
            reefEdgeComplexity = 0.75f,
            shelterAvailability = 0.85f
        };
    }

    private static ReefMetrics BuildPoorDeepSandyReef()
    {
        return new ReefMetrics
        {
            depth = ReefMetricNormalizer.NormalizeRange(55f, 0f, 60f),
            depthSuitability = ReefMetricNormalizer.RangeSuitability(55f, 8f, 24f, 2f, 42f),
            temperature = ReefMetricNormalizer.NormalizeRange(16f, 12f, 28f),
            temperatureSuitability = ReefMetricNormalizer.RangeSuitability(16f, 18f, 24f, 12f, 29f),
            salinity = ReefMetricNormalizer.NormalizeRange(35f, 34f, 40f),
            salinitySuitability = ReefMetricNormalizer.RangeSuitability(35f, 36.5f, 39f, 34f, 41f),
            chlorophyll = ReefMetricNormalizer.NormalizeRange(0.9f, 0f, 1f),
            chlorophyllSuitability = ReefMetricNormalizer.RangeSuitability(0.9f, 0.05f, 0.45f, 0f, 1.2f),
            distanceToCoast = ReefMetricNormalizer.NormalizeRange(18f, 0f, 20f),
            coastProximity = ReefMetricNormalizer.NormalizeInverse(18f, 0f, 20f),
            slope = ReefMetricNormalizer.NormalizeRange(1f, 0f, 45f),
            slopeSuitability = ReefMetricNormalizer.RangeSuitability(1f, 3f, 24f, 0f, 45f),
            rockySubstrate = 0.05f,
            sandySubstrate = 0.92f,
            substrateSuitability = 0.18f,
            seagrassPresence = 0.05f,
            reefAssociation = 0.15f,
            terrainComplexity = 0.12f,
            smallHoleDensity = 0.04f,
            mediumHoleDensity = 0.03f,
            largeCaveAvailability = 0.02f,
            shadowAvailability = 0.08f,
            lightExposure = 0.9f,
            mixedLightShadow = 0.05f,
            cleanCavities = 0.1f,
            sedimentCloggingRisk = 0.9f,
            surfaceRoughness = 0.08f,
            openSwimVolume = 0.35f,
            verticalRelief = 0.05f,
            reefEdgeComplexity = 0.06f,
            shelterAvailability = 0.08f
        };
    }
}
