using System.IO;
using UnityEngine;

public class SpeciesSuitabilityLoader : MonoBehaviour
{
    public const string DefaultConfigFileName = "species_suitability_config.json";

    [SerializeField]
    private string configFileName = DefaultConfigFileName;

    public SpeciesSuitabilityDatabase Load()
    {
        return LoadFromStreamingAssets(configFileName);
    }

    public static SpeciesSuitabilityDatabase LoadFromStreamingAssets(string fileName = DefaultConfigFileName)
    {
        string safeFileName = string.IsNullOrWhiteSpace(fileName) ? DefaultConfigFileName : fileName;
        string path = Path.Combine(Application.streamingAssetsPath, safeFileName);

        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            return LoadFromJson(json);
        }

        Debug.LogWarning($"Species suitability config not found in StreamingAssets at '{path}'. Trying Resources fallback.");
        TextAsset fallback = Resources.Load<TextAsset>(Path.GetFileNameWithoutExtension(safeFileName));

        if (fallback != null)
        {
            return LoadFromJson(fallback.text);
        }

        Debug.LogError($"Species suitability config '{safeFileName}' could not be loaded.");
        return new SpeciesSuitabilityDatabase();
    }

    public static SpeciesSuitabilityDatabase LoadFromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            Debug.LogWarning("Species suitability config JSON is empty.");
            return new SpeciesSuitabilityDatabase();
        }

        SpeciesSuitabilityDatabase database = JsonUtility.FromJson<SpeciesSuitabilityDatabase>(json);
        if (database == null)
        {
            Debug.LogError("Species suitability config JSON could not be parsed.");
            return new SpeciesSuitabilityDatabase();
        }

        if (database.species == null)
        {
            database.species = new System.Collections.Generic.List<SpeciesSuitabilityConfig>();
        }

        return database;
    }
}
