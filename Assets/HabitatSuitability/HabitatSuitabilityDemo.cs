using UnityEngine;

public class HabitatSuitabilityDemo : MonoBehaviour
{
    [SerializeField]
    private string configFileName = SpeciesSuitabilityLoader.DefaultConfigFileName;

    [SerializeField]
    private bool runOnStart = true;

    [SerializeField]
    private ReefMetrics sampleReefMetrics = ReefMetrics.CreateSampleNormalizedReef();

    private void Reset()
    {
        sampleReefMetrics = ReefMetrics.CreateSampleNormalizedReef();
    }

    private void Start()
    {
        if (runOnStart)
        {
            RunDemo();
        }
    }

    [ContextMenu("Run Habitat Suitability Demo")]
    public void RunDemo()
    {
        SpeciesSuitabilityDatabase database = SpeciesSuitabilityLoader.LoadFromStreamingAssets(configFileName);
        if (database.species == null || database.species.Count == 0)
        {
            Debug.LogWarning("No species suitability configs were loaded.");
            return;
        }

        ReefMetrics reefMetrics = sampleReefMetrics ?? ReefMetrics.CreateSampleNormalizedReef();

        for (int i = 0; i < database.species.Count; i++)
        {
            SuitabilityResult result = HabitatSuitabilityScorer.ComputeSuitability(database.species[i], reefMetrics);
            Debug.Log($"{result.scientificName}: {result.finalScore:0.000}");
        }
    }
}
