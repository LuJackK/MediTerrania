using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class HabitatSuitabilityCommandLine
{
    public static void Run()
    {
        string speciesId = GetArgumentValue("-speciesId", "parablennius_rouxi");
        string scenario = GetArgumentValue("-scenario", "all").ToLowerInvariant();
        string outputPath = GetArgumentValue("-outputFile", GetDefaultOutputPath());
        List<string> outputLines = new List<string>();

        SpeciesSuitabilityDatabase database = SpeciesSuitabilityLoader.LoadFromStreamingAssets();
        if (database.species == null || database.species.Count == 0)
        {
            WriteLine("No species suitability configs were loaded.", outputLines, true);
            WriteResultsFile(outputPath, outputLines);
            return;
        }

        if (scenario == "all" || scenario == "good")
        {
            RunScenario("Good shallow rocky reef", speciesId, database, BuildGoodRockyReef(), outputLines);
        }

        if (scenario == "all" || scenario == "poor")
        {
            RunScenario("Poor deep sandy clogged reef", speciesId, database, BuildPoorDeepSandyReef(), outputLines);
        }

        WriteResultsFile(outputPath, outputLines);
        WriteLine($"Results written to: {outputPath}", outputLines);
    }

    private static void RunScenario(
        string scenarioName,
        string speciesId,
        SpeciesSuitabilityDatabase database,
        ReefMetrics reefMetrics,
        List<string> outputLines)
    {
        WriteLine($"--- Habitat suitability command-line test: {scenarioName} ---", outputLines);

        if (!string.IsNullOrWhiteSpace(speciesId) && speciesId != "all")
        {
            SpeciesSuitabilityConfig species = database.FindSpecies(speciesId);
            if (species == null)
            {
                WriteLine($"Species '{speciesId}' was not found in species_suitability_config.json.", outputLines, true);
                return;
            }

            LogResult(species, reefMetrics, outputLines);
            return;
        }

        for (int i = 0; i < database.species.Count; i++)
        {
            LogResult(database.species[i], reefMetrics, outputLines);
        }
    }

    private static void LogResult(SpeciesSuitabilityConfig species, ReefMetrics reefMetrics, List<string> outputLines)
    {
        float score = HabitatSuitabilityScorer.ComputeSuitabilityScore(species, reefMetrics);
        WriteLine($"{species.scientificName} ({species.commonName}): {score:0.000}", outputLines);
    }

    private static string GetArgumentValue(string argumentName, string fallback)
    {
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (string.Equals(args[i], argumentName, StringComparison.OrdinalIgnoreCase))
            {
                return args[i + 1];
            }
        }

        return fallback;
    }

    private static void WriteLine(string message, List<string> outputLines, bool isError = false)
    {
        outputLines.Add(message);
        Console.WriteLine(message);

        if (isError)
        {
            Debug.LogError(message);
        }
        else
        {
            Debug.Log(message);
        }
    }

    private static void WriteResultsFile(string outputPath, List<string> outputLines)
    {
        try
        {
            string directory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllLines(outputPath, outputLines);
        }
        catch (Exception exception)
        {
            Debug.LogError($"Could not write habitat suitability results file: {exception.Message}");
        }
    }

    private static string GetDefaultOutputPath()
    {
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        return Path.Combine(projectRoot, "habitat_suitability_results.txt");
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
