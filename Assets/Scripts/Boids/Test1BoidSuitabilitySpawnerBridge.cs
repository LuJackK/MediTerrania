using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class Test1BoidSuitabilitySpawnerBridge : MonoBehaviour
{
    [SerializeField] private BoidSpawnManager boidSpawnManager;
    [SerializeField] private bool applyCurrentScoresOnEnable = true;
    [SerializeField] private bool clearUnmappedSpecies = true;
    [SerializeField] private bool logUpdates;

    private void Reset()
    {
        boidSpawnManager = GetComponent<BoidSpawnManager>();
    }

    private void Awake()
    {
        if (boidSpawnManager == null)
        {
            boidSpawnManager = GetComponent<BoidSpawnManager>();
        }
    }

    private void OnEnable()
    {
        Test1CohesiveUiBootstrap.ScoresUpdated += HandleScoresUpdated;

        if (applyCurrentScoresOnEnable)
        {
            ApplyCurrentSceneScores();
        }
    }

    private void OnDisable()
    {
        Test1CohesiveUiBootstrap.ScoresUpdated -= HandleScoresUpdated;
    }

    [ContextMenu("Apply Current Scene Scores To Boids")]
    public void ApplyCurrentSceneScores()
    {
        ReefSceneAttributes sceneAttributes = ReefSceneSuitabilityEvaluator.CaptureCurrentSceneAttributes();
        SpeciesSuitabilityDatabase speciesDatabase = SpeciesSuitabilityLoader.LoadFromStreamingAssets();
        ReefSceneAttributeMappingConfig mappingConfig =
            ReefSceneAttributeMappingLoader.LoadFromStreamingAssets(ReefSceneSuitabilityEvaluator.DefaultMappingConfigFileName);

        List<SuitabilityResult> results = ReefSceneSuitabilityEvaluator.ScoreSceneAttributesForAllSpecies(
            sceneAttributes,
            speciesDatabase,
            mappingConfig);

        ApplyScoresToBoids(results);
    }

    private void HandleScoresUpdated(IReadOnlyList<SuitabilityResult> results)
    {
        ApplyScoresToBoids(results);
    }

    private void ApplyScoresToBoids(IReadOnlyList<SuitabilityResult> results)
    {
        if (boidSpawnManager == null)
        {
            Debug.LogWarning("[Test1BoidSuitabilitySpawnerBridge] Missing BoidSpawnManager reference.");
            return;
        }

        if (boidSpawnManager.species == null || boidSpawnManager.species.Length == 0)
        {
            return;
        }

        int mappedSpeciesCount = results != null ? Mathf.Min(results.Count, boidSpawnManager.species.Length) : 0;

        for (int speciesIndex = 0; speciesIndex < boidSpawnManager.species.Length; speciesIndex++)
        {
            BoidSpawnManager.BoidSpecies boidSpecies = boidSpawnManager.species[speciesIndex];
            if (boidSpecies == null)
            {
                continue;
            }

            int targetBoidCount = 0;
            bool hasScore = speciesIndex < mappedSpeciesCount && results[speciesIndex] != null;
            if (hasScore)
            {
                float normalizedScore = Mathf.Clamp01(results[speciesIndex].finalScore);
                targetBoidCount = Mathf.RoundToInt(normalizedScore * boidSpecies.maxBoids);
            }
            else if (!clearUnmappedSpecies)
            {
                continue;
            }

            boidSpawnManager.SetBoidCount(speciesIndex, targetBoidCount);
        }

        if (logUpdates)
        {
            Debug.Log($"[Test1BoidSuitabilitySpawnerBridge] Updated boid counts for {mappedSpeciesCount} mapped species.");
        }
    }
}
