# Habitat Suitability Scoring

This folder contains a JSON-driven hybrid suitability scorer for Mediterranean fish reef designs.

- `trainedEnvironmentCoefficients` are ML/big-data coefficients already produced outside Unity.
- `researchedMicrohabitatCoefficients` are literature or human-curated preferences.
- `criticalFactors` are limiting factors that prevent impossible habitats from scoring too high.

Unity does not train models or run Python. It loads `Assets/StreamingAssets/species_suitability_config.json`, reads normalized `ReefMetrics`, and computes:

```text
final_score = (environmentWeight * environment_score + microhabitatWeight * microhabitat_score) * critical_multiplier
```

All metric values must be normalized to `0..1` before scoring. Use `ReefMetricNormalizer.RangeSuitability` for values such as depth where suitability is highest inside an ideal range and falls off outside it.

To add a species, add another object to the JSON `species` list. To add a feature, either use an existing `ReefMetrics` field or add it to `ReefMetrics.additionalMetrics` with the same feature name used in JSON.

## Quick Test

Add `HabitatSuitabilityTestCase` to an empty GameObject and press Play. It takes a fish input by `speciesId` plus raw/normalized terrain inputs from the Inspector, then logs suitability scores. Leave `speciesId` empty to score every configured fish against the same terrain.

You can also run a console test through Unity batchmode:

```powershell
$unity = "C:\Program Files\Unity\Hub\Editor\6000.3.2f1\Editor\Unity.exe"
$project = "C:\Users\CapnJack\MediTerrania"
$results = "$project\habitat_suitability_results.txt"
$log = "$project\habitat_suitability_unity.log"
Remove-Item $results,$log -ErrorAction SilentlyContinue
Start-Process -FilePath $unity -Wait -NoNewWindow -ArgumentList @(
  "-batchmode", "-quit",
  "-projectPath", $project,
  "-executeMethod", "HabitatSuitabilityCommandLine.Run",
  "-speciesId", "parablennius_rouxi",
  "-scenario", "all",
  "-outputFile", $results,
  "-logFile", $log
)
Get-Content $results
```

Use `-speciesId all` to score every configured fish. Use `-scenario good`, `-scenario poor`, or `-scenario all`.
