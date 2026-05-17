# Habitat Suitability Scientific Documentation

This document describes the fish-species evaluation function implemented in `Assets/HabitatSuitability` and separates three evidence categories:

- **Data-backed**: values that can be traced to `Assets/training_samples.csv`, `Assets/coefficients.csv`, or `Assets/species_model_summary.csv`.
- **Engineering conversion**: transformations needed to use the trained environmental model inside Unity as a normalized `0..1` gameplay/design score.
- **Curated/anecdotal**: species preferences or reef-design rules stored in JSON without per-claim citations or measured local validation data.

## 1. Evaluation Function

Unity loads `Assets/StreamingAssets/species_suitability_config.json` and evaluates each species with normalized `ReefMetrics`.

For a coefficient list, `HabitatSuitabilityScorer.WeightedSum` does the following:

```text
weighted_total = sum(weight_i * clamp01(metric_i))
total_abs_weight = sum(abs(weight_i))
normalized_total = weighted_total / total_abs_weight
normalized_min = sum(negative weights) / total_abs_weight
normalized_max = sum(positive weights) / total_abs_weight
score = inverse_lerp(normalized_min, normalized_max, normalized_total)
```

This makes mixed positive and negative coefficients usable as a final `0..1` score. Positive weights reward high metric values; negative weights penalize high metric values.

The full species score is:

```text
environment_score = weighted_sum(trainedEnvironmentCoefficients)
microhabitat_score = weighted_sum(researchedMicrohabitatCoefficients)

normalized_environment_weight =
  environmentWeight / (environmentWeight + microhabitatWeight)

normalized_microhabitat_weight =
  microhabitatWeight / (environmentWeight + microhabitatWeight)

weighted_score =
  normalized_environment_weight * environment_score
  + normalized_microhabitat_weight * microhabitat_score

critical_multiplier = min(clamp01(metric) for each critical factor)

final_score = clamp01(weighted_score * critical_multiplier)
```

Suitability classes are:

- `< 0.25`: Poor suitability
- `0.25..0.49`: Low suitability
- `0.50..0.74`: Moderate suitability
- `>= 0.75`: High suitability

### Rope Sedimentation Adjustment

When rope count is provided, the scorer applies a sediment mitigation rule before scoring:

```text
sedimentation_reduction = clamp(ropeCount * 0.12, 0, 0.75)
```

This reduces `sedimentCloggingRisk` and `sandySubstrate`, while improving `cleanCavities` and `substrateSuitability`. This is an engineering/curated rule, not a fitted species-specific result from the training data.

## 2. Data Provenance

The local training artifacts indicate a presence-background species distribution model:

- Presence records: `gbif_presence` rows in `Assets/training_samples.csv`.
- Background records: `biooracle_background` rows in `Assets/training_samples.csv`.
- Environmental predictors: depth, temperature, salinity, chlorophyll, light attenuation, slope, terrain ruggedness, and distance to coast.
- Model outputs: per-standard-deviation log-odds coefficients, odds ratios, relative weights, and training AUC in `Assets/coefficients.csv`.

The species-level sample sizes are:

| Species | GBIF presence records | Bio-ORACLE/background records | Training AUC |
| --- | ---: | ---: | ---: |
| *Parablennius rouxi* | 214 | 2,000 | 0.9663 |
| *Parablennius incognitus* | 165 | 2,000 | 0.9659 |
| *Chromis chromis* | 358 | 2,000 | 0.9662 |
| *Diplodus vulgaris* | 340 | 2,000 | 0.9620 |

Important interpretation: the Unity score is **not** the original logistic probability from the training artifact. The intercepts in `Assets/species_model_summary.csv` are not used at runtime. Instead, the exported relative weights are converted into normalized suitability components for an interactive reef-design score.

## 3. What The Big-Data Model Concluded

Across all four species, the fitted environmental models reached high training AUC values, approximately `0.962..0.966`. Within the local artifacts, the strongest repeated signals are:

- **Distance to coast is strongly negative** in the raw model for every species, meaning occurrence records were much more coastal than background records. Unity represents this as a positive `coastProximity` suitability metric.
- **Depth is strongly negative** in the raw model for every species, meaning shallower records were favored relative to the sampled marine background. Unity represents this as `depthSuitability`.
- **Temperature is negative** in the raw model for every species, meaning records were associated with cooler waters than the background sample. Unity converts this into `temperatureSuitability`, with an ideal range configured separately.
- **Salinity is positive** for every species, strongest for both blennies and weaker for *Chromis* and *Diplodus*.
- **Terrain ruggedness is positive** for *Chromis chromis*, *Diplodus vulgaris*, and weakly for *Parablennius rouxi*, but very slightly negative for *Parablennius incognitus*.
- **Chlorophyll is weakly positive** for all four species, but contributes little compared with coast and depth.
- **Light attenuation is negative** in the raw trained models but is not currently represented in `species_suitability_config.json`.
- **Slope is weak and inconsistent**: negative for *P. rouxi*, *C. chromis*, and *D. vulgaris*, slightly positive for *P. incognitus*.

These are correlative distribution-model conclusions. They should not be interpreted as causal proof that changing one reef object will create the same biological response.

## 4. Species-Specific Documentation

### *Parablennius rouxi* - Longstriped blenny

Runtime blend: `55%` environmental score, `45%` microhabitat score.

Data-backed environmental conclusions:

| Predictor | Raw model direction | Relative weight | Runtime representation |
| --- | --- | ---: | --- |
| Distance to coast | Negative | 0.308 | `coastProximity` rewards coastal habitat |
| Depth | Negative | 0.304 | `depthSuitability` rewards shallow/ideal depth |
| Temperature | Negative | 0.204 | `temperatureSuitability` rewards ideal configured temperature |
| Salinity | Positive | 0.121 | `salinitySuitability` |
| Chlorophyll | Positive | 0.026 | `chlorophyllSuitability` |
| Terrain ruggedness | Positive | 0.015 | `terrainComplexity` |
| Light attenuation | Negative | 0.011 | Not used in current runtime JSON |
| Slope | Negative | 0.011 | `slopeSuitability` |

Big-data summary: this model mainly identifies *P. rouxi* as a near-coastal, shallow-water species, with secondary support from cooler/ideal temperatures and higher salinity. Rugged terrain contributes only weakly.

Curated/anecdotal runtime assumptions:

| Runtime feature | Weight | Evidence status |
| --- | ---: | --- |
| `smallHoleDensity` | 0.35 | Curated; broadly consistent with blenny use of narrow holes, but weight is not fitted locally |
| `cleanCavities` | 0.25 | Curated reef-design rule |
| `shadowAvailability` | 0.20 | Curated |
| `surfaceRoughness` | 0.20 | Curated |
| `rockySubstrate` in environment list | 0.05 | Curated addition, not in `coefficients.csv` |
| Critical factors: `depthSuitability`, `substrateSuitability`, `cleanCavities` | limiter | Curated safety gate |

External ecology check: FishBase describes *P. rouxi* as demersal, shallow, associated with light rocks/pebbles and coralligenous hard bottoms, with males using narrow holes. That supports the qualitative direction of the curated hole/rock/cavity assumptions, but not the exact numerical weights.

### *Parablennius incognitus* - Mystery blenny

Runtime blend: `55%` environmental score, `45%` microhabitat score.

Data-backed environmental conclusions:

| Predictor | Raw model direction | Relative weight | Runtime representation |
| --- | --- | ---: | --- |
| Distance to coast | Negative | 0.358 | `coastProximity` |
| Depth | Negative | 0.323 | `depthSuitability` |
| Temperature | Negative | 0.151 | `temperatureSuitability` |
| Salinity | Positive | 0.111 | `salinitySuitability` |
| Chlorophyll | Positive | 0.041 | `chlorophyllSuitability` |
| Slope | Positive | 0.009 | `slopeSuitability` in runtime, though this flips detail into a broad ideal-range suitability measure |
| Terrain ruggedness | Negative | 0.004 | `terrainComplexity` with a tiny negative runtime weight |
| Light attenuation | Negative | 0.003 | Not used in current runtime JSON |

Big-data summary: the dominant signal is again near-coastal and shallow habitat. Salinity and temperature are secondary. Terrain ruggedness and slope are so small that they should be treated as weak signals.

Curated/anecdotal runtime assumptions:

| Runtime feature | Weight | Evidence status |
| --- | ---: | --- |
| `smallHoleDensity` | 0.28 | Curated |
| `mediumHoleDensity` | 0.20 | Curated |
| `mixedLightShadow` | 0.18 | Curated |
| `cleanCavities` | 0.22 | Curated |
| `surfaceRoughness` | 0.12 | Curated |
| `rockySubstrate` in environment list | 0.05 | Curated addition, not in `coefficients.csv` |
| Critical factors: `depthSuitability`, `substrateSuitability`, `cleanCavities` | limiter | Curated safety gate |

External ecology check: FishBase describes adults as inhabiting rocky coastal shores and feeding on bottom invertebrates and algae. This supports the general rocky/nearshore/benthic framing, but not the exact small-hole, mixed-shadow, or clean-cavity weights.

### *Chromis chromis* - Damselfish / Mediterranean chromis

Runtime blend: `60%` environmental score, `40%` microhabitat score.

Data-backed environmental conclusions:

| Predictor | Raw model direction | Relative weight | Runtime representation |
| --- | --- | ---: | --- |
| Distance to coast | Negative | 0.361 | `coastProximity` |
| Depth | Negative | 0.337 | `depthSuitability` |
| Temperature | Negative | 0.118 | `temperatureSuitability` |
| Terrain ruggedness | Positive | 0.068 | `terrainComplexity` |
| Light attenuation | Negative | 0.047 | Not used in current runtime JSON |
| Salinity | Positive | 0.042 | `salinitySuitability` |
| Slope | Negative | 0.021 | `slopeSuitability` |
| Chlorophyll | Positive | 0.006 | `chlorophyllSuitability` |

Big-data summary: *C. chromis* is strongly associated with coastal, shallow environments. Compared with the blennies, terrain ruggedness is a more visible secondary signal, consistent with a reef-associated fish.

Curated/anecdotal runtime assumptions:

| Runtime feature | Weight | Evidence status |
| --- | ---: | --- |
| `openSwimVolume` | 0.32 | Curated |
| `verticalRelief` | 0.20 | Curated |
| `reefEdgeComplexity` | 0.18 | Curated |
| `shelterAvailability` | 0.16 | Curated |
| `mixedLightShadow` | 0.14 | Curated |
| `reefAssociation` in environment list | 0.10 | Curated addition, not in `coefficients.csv` |
| Critical factors: `depthSuitability`, `reefAssociation`, `substrateSuitability` | limiter | Curated safety gate |

External ecology check: FishBase lists *C. chromis* as marine, reef-associated, non-migratory, with a `2..40 m` depth range. This supports the reef-association and shallow-depth direction, but not the exact open-swim or vertical-relief weights.

### *Diplodus vulgaris* - Common two-banded seabream

Runtime blend: `60%` environmental score, `40%` microhabitat score.

Data-backed environmental conclusions:

| Predictor | Raw model direction | Relative weight | Runtime representation |
| --- | --- | ---: | --- |
| Distance to coast | Negative | 0.402 | `coastProximity` |
| Depth | Negative | 0.340 | `depthSuitability` |
| Temperature | Negative | 0.110 | `temperatureSuitability` |
| Terrain ruggedness | Positive | 0.064 | `terrainComplexity` |
| Slope | Negative | 0.031 | `slopeSuitability` |
| Salinity | Positive | 0.026 | `salinitySuitability` |
| Light attenuation | Negative | 0.025 | Not used in current runtime JSON |
| Chlorophyll | Positive | 0.002 | `chlorophyllSuitability` |

Big-data summary: the strongest result is coastal/shallow association, even stronger for coast proximity than for the other species. Terrain ruggedness is a modest positive secondary signal. Chlorophyll is essentially negligible.

Curated/anecdotal runtime assumptions:

| Runtime feature | Weight | Evidence status |
| --- | ---: | --- |
| `openSwimVolume` | 0.24 | Curated |
| `reefEdgeComplexity` | 0.22 | Curated |
| `mediumHoleDensity` | 0.16 | Curated |
| `shelterAvailability` | 0.18 | Curated |
| `verticalRelief` | 0.20 | Curated |
| `rockySubstrate` in environment list | 0.08 | Curated addition, not in `coefficients.csv` |
| Critical factors: `depthSuitability`, `substrateSuitability`, `shelterAvailability` | limiter | Curated safety gate |

External ecology check: FishBase describes *D. vulgaris* as benthopelagic, commonly shallow, using rocky and sometimes sandy bottoms, with young sometimes in seagrass beds. This supports broad substrate and shelter assumptions, but not the exact reef-edge, hole-density, or vertical-relief weights.

## 5. Evidence Classification By Model Component

| Component | Evidence class | Notes |
| --- | --- | --- |
| Presence counts and background counts | Data-backed | Stored in `training_samples.csv`, `coefficients.csv`, and `species_model_summary.csv` |
| Coefficient directions and relative weights for depth, temperature, salinity, chlorophyll, slope, terrain ruggedness, distance to coast | Data-backed | Exported trained model outputs |
| Training AUC values | Data-backed, but limited | Training AUC only; no independent validation file is present |
| `depthSuitability`, `temperatureSuitability`, `salinitySuitability`, etc. | Engineering conversion | Converts raw ecological gradients into normalized scene metrics |
| Environmental/microhabitat blend weights (`0.55/0.45`, `0.60/0.40`) | Curated/engineering | Not trained in the visible artifacts |
| Microhabitat coefficient lists | Curated/anecdotal | No per-feature literature citation or local measurement dataset is stored |
| `rockySubstrate` and `reefAssociation` inside `trainedEnvironmentCoefficients` | Curated addition | These are placed in the trained list but do not appear in `coefficients.csv` |
| Critical factors | Curated/engineering | Useful ecological safety gates, but not fitted from the local big-data model |
| Rope sedimentation reduction | Curated/engineering | Generic mitigation rule, not species-specific training output |
| Fallback terrain inference for unconfigured terrain bases | Engineering heuristic | Keeps scores informative when JSON terrain mappings are zero-filled |

## 6. Scientific Limitations

- The repository contains exported training artifacts but not the training script, exact model class, cross-validation design, spatial thinning/filtering steps, or hyperparameters.
- The AUC values are training AUCs, not independent test-set AUCs.
- GBIF presence data are occurrence records, not standardized abundance surveys. They can include observer, geography, accessibility, and taxonomic biases.
- Bio-ORACLE/background rows are pseudo-absence/background samples, not confirmed absences.
- The environmental model describes broad-scale distribution correlations. The Unity score is a reef-design suitability index, so it should be treated as a decision-support score rather than a calibrated probability of occupancy, abundance, survival, or recruitment.
- Microhabitat weights are plausible species-specific priors but are not currently auditable as scientific claims because the JSON does not include citations, confidence values, or source notes.
- Current `reef_scene_attribute_mapping.json` terrain bases are mostly zero placeholders. Runtime fallback heuristics will therefore influence terrain-complexity, hole-density, shelter, light, and roughness scores until those mappings are measured or authored directly.

## 7. Recommended Improvements

To make the function scientifically auditable, add the following fields to each JSON feature weight:

```json
{
  "feature": "smallHoleDensity",
  "weight": 0.35,
  "evidenceLevel": "literature",
  "source": "FishBase / primary study / expert elicitation",
  "confidence": 0.6,
  "notes": "Qualitative direction supported; numeric weight expert-assigned."
}
```

Also recommended:

- Store the training script and model specification alongside `coefficients.csv`.
- Add independent validation metrics or spatial block cross-validation.
- Keep trained environmental coefficients and hand-added environmental priors in separate JSON lists.
- Add `light_attenuation_kdpar` to runtime metrics or document why it was intentionally omitted.
- Replace placeholder terrain mappings with measured values from the actual reef geometry or authored per-prefab values.
- Record source citations for every microhabitat preference.

## 8. External Reference Links

- GBIF citation and occurrence-data guidance: https://www.gbif.org/citation-guidelines
- Bio-ORACLE marine environmental layers: https://bio-oracle.org/
- Bio-ORACLE data paper summary: https://cir.nii.ac.jp/crid/1361699994036900224
- FishBase: *Parablennius rouxi*: https://www.fishbase.se/summary/Parablennius-rouxi.html
- FishBase: *Parablennius incognitus*: https://www.fishbase.se/summary/Parablennius-incognitus.html
- FishBase: *Chromis chromis*: https://www.fishbase.se/summary/Chromis-chromis.html
- FishBase: *Diplodus vulgaris*: https://www.fishbase.se/summary/Diplodus-vulgaris.html
