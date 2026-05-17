using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class HabitatSuitabilityTesterUi : MonoBehaviour
{
    private const string PanelName = "Habitat Suitability Tester";

    private readonly List<SliderBinding> sliders = new();
    private SpeciesSuitabilityDatabase database;
    private RectTransform panel;
    private RectTransform sliderContent;
    private RectTransform[] sliderColumns;
    private ScrollRect sliderScrollRect;
    private TMP_Text speciesLabel;
    private TMP_Text scoreLabel;
    private TMP_Text scoreClassLabel;
    private int selectedSpeciesIndex;
    private int createdSliderCount;
    private bool fullScreenLayout;

    private float depthMeters = 16f;
    private float temperatureCelsius = 20f;
    private float salinityPsu = 38f;
    private float distanceToCoastKm = 0.5f;
    private float slopeDegrees = 12f;
    private float rockySubstrate = 0.9f;
    private float sandySubstrate = 0.1f;
    private float substrateSuitability = 0.9f;
    private float reefAssociation = 0.85f;
    private float smallHoleDensity = 0.85f;
    private float cleanCavities = 0.86f;
    private float sedimentCloggingRisk = 0.14f;
    private float ropeCount;
    private float surfaceRoughness = 0.8f;
    private float shelterAvailability = 0.82f;

    public static HabitatSuitabilityTesterUi Create(RectTransform parent)
    {
        if (parent == null)
        {
            return null;
        }

        HabitatSuitabilityTesterUi existing = parent.GetComponentInChildren<HabitatSuitabilityTesterUi>(true);
        if (existing != null)
        {
            return existing;
        }

        RectTransform panel = MediTerraniaRuntimeUi.CreatePanel(parent, PanelName, new Vector2(360f, 640f));
        HabitatSuitabilityTesterUi testerUi = panel.gameObject.AddComponent<HabitatSuitabilityTesterUi>();
        testerUi.Build(panel);
        return testerUi;
    }

    public static HabitatSuitabilityTesterUi CreateFullScreen(Canvas canvas)
    {
        if (canvas == null)
        {
            return null;
        }

        HabitatSuitabilityTesterUi existing = canvas.GetComponentInChildren<HabitatSuitabilityTesterUi>(true);
        if (existing != null)
        {
            return existing;
        }

        GameObject panelObject = new(PanelName, typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
        panelObject.transform.SetParent(canvas.transform, false);

        RectTransform panelTransform = panelObject.GetComponent<RectTransform>();
        panelTransform.anchorMin = Vector2.zero;
        panelTransform.anchorMax = Vector2.one;
        panelTransform.pivot = new Vector2(0.5f, 0.5f);
        panelTransform.offsetMin = new Vector2(28f, 28f);
        panelTransform.offsetMax = new Vector2(-28f, -28f);

        Image image = panelObject.GetComponent<Image>();
        image.sprite = MediTerraniaRuntimeUi.SolidSprite;
        image.type = Image.Type.Sliced;
        image.color = new Color(0.025f, 0.11f, 0.16f, 0.94f);

        Outline outline = panelObject.AddComponent<Outline>();
        outline.effectColor = MediTerraniaRuntimeUi.PanelStrokeColor;
        outline.effectDistance = new Vector2(1f, -1f);

        VerticalLayoutGroup layout = panelObject.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(28, 28, 24, 24);
        layout.spacing = 10f;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        HabitatSuitabilityTesterUi testerUi = panelObject.AddComponent<HabitatSuitabilityTesterUi>();
        testerUi.Build(panelTransform, true);
        return testerUi;
    }

    private void Build(RectTransform panelTransform, bool fullScreen = false)
    {
        database = SpeciesSuitabilityLoader.LoadFromStreamingAssets();

        panel = panelTransform;
        MediTerraniaRuntimeUi.CreateTitle(panel, "Fish Suitability");

        speciesLabel = CreateText(panel, "Species", string.Empty, 13f, FontStyles.Bold, MediTerraniaRuntimeUi.TextColor);
        MediTerraniaRuntimeUi.AddLayoutElement(speciesLabel.gameObject, 22f);

        RectTransform speciesRow = MediTerraniaRuntimeUi.CreateRow(panel, "Species Controls", 32f);
        MediTerraniaRuntimeUi.CreateButton(speciesRow, "Previous Species", "<", SelectPreviousSpecies);
        MediTerraniaRuntimeUi.CreateButton(speciesRow, "Next Species", ">", SelectNextSpecies);

        scoreLabel = CreateText(panel, "Score", "0.000", 34f, FontStyles.Bold, MediTerraniaRuntimeUi.AccentColor);
        scoreLabel.alignment = TextAlignmentOptions.Center;
        MediTerraniaRuntimeUi.AddLayoutElement(scoreLabel.gameObject, 44f);

        scoreClassLabel = CreateText(panel, "Score Class", string.Empty, 12f, FontStyles.Normal, MediTerraniaRuntimeUi.MutedTextColor);
        scoreClassLabel.alignment = TextAlignmentOptions.Center;
        MediTerraniaRuntimeUi.AddLayoutElement(scoreClassLabel.gameObject, 20f);

        RectTransform presetRow = MediTerraniaRuntimeUi.CreateRow(panel, "Preset Controls", 32f);
        MediTerraniaRuntimeUi.CreateButton(presetRow, "Good Reef Preset", "Good", UseGoodPreset);
        MediTerraniaRuntimeUi.CreateButton(presetRow, "Poor Reef Preset", "Poor", UsePoorPreset);
        MediTerraniaRuntimeUi.CreateButton(presetRow, "Reset Reef Preset", "Reset", UseDefaultPreset);

        fullScreenLayout = fullScreen;
        sliderContent = fullScreen ? CreateFullScreenSliderArea(panel) : CreateSliderScrollArea(panel, false);

        CreateSlider("Depth", "0.0 m", 0f, 60f, () => depthMeters, value => depthMeters = value);
        CreateSlider("Temp", "0.0 C", 12f, 29f, () => temperatureCelsius, value => temperatureCelsius = value);
        CreateSlider("Salinity", "0.0 PSU", 34f, 41f, () => salinityPsu, value => salinityPsu = value);
        CreateSlider("Coast", "0.0 km", 0f, 20f, () => distanceToCoastKm, value => distanceToCoastKm = value);
        CreateSlider("Slope", "0.0 deg", 0f, 45f, () => slopeDegrees, value => slopeDegrees = value);
        CreateSlider("Rocky substrate", "0.00", 0f, 1f, () => rockySubstrate, value => rockySubstrate = value);
        CreateSlider("Sandy substrate", "0.00", 0f, 1f, () => sandySubstrate, value => sandySubstrate = value);
        CreateSlider("Substrate score", "0.00", 0f, 1f, () => substrateSuitability, value => substrateSuitability = value);
        CreateSlider("Reef link", "0.00", 0f, 1f, () => reefAssociation, value => reefAssociation = value);
        CreateSlider("Small holes", "0.00", 0f, 1f, () => smallHoleDensity, value => smallHoleDensity = value);
        CreateSlider("Clean cavities", "0.00", 0f, 1f, () => cleanCavities, value => cleanCavities = value);
        CreateSlider("Sediment risk", "0.00", 0f, 1f, () => sedimentCloggingRisk, value => sedimentCloggingRisk = value);
        CreateSlider("Ropes", "0", 0f, 10f, () => ropeCount, value => ropeCount = Mathf.Round(value));
        CreateSlider("Surface roughness", "0.00", 0f, 1f, () => surfaceRoughness, value => surfaceRoughness = value);
        CreateSlider("Shelter", "0.00", 0f, 1f, () => shelterAvailability, value => shelterAvailability = value);

        RefreshAll();
    }

    private void SelectPreviousSpecies()
    {
        int count = SpeciesCount;
        if (count <= 0)
        {
            return;
        }

        selectedSpeciesIndex = (selectedSpeciesIndex - 1 + count) % count;
        RefreshScore();
    }

    private void SelectNextSpecies()
    {
        int count = SpeciesCount;
        if (count <= 0)
        {
            return;
        }

        selectedSpeciesIndex = (selectedSpeciesIndex + 1) % count;
        RefreshScore();
    }

    private void UseDefaultPreset()
    {
        depthMeters = 16f;
        temperatureCelsius = 20f;
        salinityPsu = 38f;
        distanceToCoastKm = 0.5f;
        slopeDegrees = 12f;
        rockySubstrate = 0.9f;
        sandySubstrate = 0.1f;
        substrateSuitability = 0.9f;
        reefAssociation = 0.85f;
        smallHoleDensity = 0.85f;
        cleanCavities = 0.86f;
        sedimentCloggingRisk = 0.14f;
        ropeCount = 0f;
        surfaceRoughness = 0.8f;
        shelterAvailability = 0.82f;
        RefreshAll();
    }

    private void UseGoodPreset()
    {
        depthMeters = 14f;
        temperatureCelsius = 20f;
        salinityPsu = 38f;
        distanceToCoastKm = 0.5f;
        slopeDegrees = 14f;
        rockySubstrate = 0.92f;
        sandySubstrate = 0.08f;
        substrateSuitability = 0.92f;
        reefAssociation = 0.9f;
        smallHoleDensity = 0.88f;
        cleanCavities = 0.88f;
        sedimentCloggingRisk = 0.12f;
        ropeCount = 0f;
        surfaceRoughness = 0.82f;
        shelterAvailability = 0.85f;
        RefreshAll();
    }

    private void UsePoorPreset()
    {
        depthMeters = 55f;
        temperatureCelsius = 16f;
        salinityPsu = 35f;
        distanceToCoastKm = 18f;
        slopeDegrees = 1f;
        rockySubstrate = 0.05f;
        sandySubstrate = 0.92f;
        substrateSuitability = 0.18f;
        reefAssociation = 0.15f;
        smallHoleDensity = 0.04f;
        cleanCavities = 0.1f;
        sedimentCloggingRisk = 0.9f;
        ropeCount = 0f;
        surfaceRoughness = 0.08f;
        shelterAvailability = 0.08f;
        RefreshAll();
    }

    private void CreateSlider(
        string label,
        string valueFormat,
        float minValue,
        float maxValue,
        Func<float> getValue,
        Action<float> setValue)
    {
        RectTransform row = MediTerraniaRuntimeUi.CreateRow(GetSliderParent(), $"{label} Row", fullScreenLayout ? 44f : 32f);
        TMP_Text nameLabel = CreateText(row, $"{label} Label", label, fullScreenLayout ? 15f : 13f, FontStyles.Normal, MediTerraniaRuntimeUi.MutedTextColor);
        nameLabel.alignment = TextAlignmentOptions.Left;
        AddWidth(nameLabel.gameObject, fullScreenLayout ? 150f : 120f);

        Slider slider = MediTerraniaRuntimeUi.CreateSlider(row, $"{label} Slider", minValue, maxValue, getValue());

        TMP_Text valueLabel = CreateText(row, $"{label} Value", string.Empty, fullScreenLayout ? 15f : 13f, FontStyles.Bold, MediTerraniaRuntimeUi.TextColor);
        valueLabel.alignment = TextAlignmentOptions.Right;
        AddWidth(valueLabel.gameObject, fullScreenLayout ? 100f : 86f);

        SliderBinding binding = new()
        {
            slider = slider,
            valueLabel = valueLabel,
            valueFormat = valueFormat,
            getValue = getValue,
            setValue = setValue
        };

        slider.onValueChanged.AddListener(value =>
        {
            binding.setValue(value);
            RefreshSliderLabel(binding);
            RefreshScore();
        });

        sliders.Add(binding);
        createdSliderCount++;
        RefreshSliderLabel(binding);
    }

    private void RefreshAll()
    {
        for (int i = 0; i < sliders.Count; i++)
        {
            SliderBinding binding = sliders[i];
            binding.slider.SetValueWithoutNotify(binding.getValue());
            RefreshSliderLabel(binding);
        }

        RefreshScore();

        if (sliderScrollRect != null)
        {
            sliderScrollRect.verticalNormalizedPosition = 1f;
        }
    }

    private void RefreshScore()
    {
        SpeciesSuitabilityConfig species = SelectedSpecies;
        if (species == null)
        {
            speciesLabel.text = "No species config loaded";
            scoreLabel.text = "0.000";
            scoreClassLabel.text = "Score is normalized from 0 to 1";
            return;
        }

        ReefMetrics metrics = BuildMetrics();
        float score = HabitatSuitabilityScorer.ComputeSuitabilityScore(species, metrics, Mathf.RoundToInt(ropeCount));
        speciesLabel.text = $"{species.scientificName} ({species.commonName})";
        scoreLabel.text = score.ToString("0.000");
        scoreClassLabel.text = HabitatSuitabilityScorer.ClassifySuitability(score);
    }

    private ReefMetrics BuildMetrics()
    {
        float chlorophyllMgM3 = 0.2f;

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
            seagrassPresence = 0.2f,
            reefAssociation = reefAssociation,
            terrainComplexity = Mathf.Max(rockySubstrate, reefAssociation, shelterAvailability),
            smallHoleDensity = smallHoleDensity,
            mediumHoleDensity = smallHoleDensity * 0.7f,
            largeCaveAvailability = shelterAvailability * 0.35f,
            shadowAvailability = Mathf.Max(cleanCavities * 0.6f, shelterAvailability * 0.7f),
            lightExposure = 0.55f,
            mixedLightShadow = 0.7f,
            cleanCavities = cleanCavities,
            sedimentCloggingRisk = sedimentCloggingRisk,
            surfaceRoughness = surfaceRoughness,
            openSwimVolume = 0.58f,
            verticalRelief = Mathf.Max(slopeDegrees / 45f, shelterAvailability * 0.65f),
            reefEdgeComplexity = Mathf.Max(reefAssociation, shelterAvailability) * 0.85f,
            shelterAvailability = shelterAvailability
        };
    }

    private SpeciesSuitabilityConfig SelectedSpecies
    {
        get
        {
            if (SpeciesCount <= 0)
            {
                return null;
            }

            selectedSpeciesIndex = Mathf.Clamp(selectedSpeciesIndex, 0, SpeciesCount - 1);
            return database.species[selectedSpeciesIndex];
        }
    }

    private int SpeciesCount => database?.species?.Count ?? 0;

    private static void RefreshSliderLabel(SliderBinding binding)
    {
        binding.valueLabel.text = binding.getValue().ToString(binding.valueFormat);
    }

    private static TMP_Text CreateText(Transform parent, string name, string text, float fontSize, FontStyles style, Color color)
    {
        GameObject textObject = new(name, typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
        textObject.transform.SetParent(parent, false);

        TMP_Text label = textObject.GetComponent<TMP_Text>();
        label.text = text;
        label.fontSize = fontSize;
        label.fontStyle = style;
        label.color = color;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.overflowMode = TextOverflowModes.Ellipsis;
        label.raycastTarget = false;
        return label;
    }

    private RectTransform GetSliderParent()
    {
        if (sliderColumns == null || sliderColumns.Length == 0)
        {
            return sliderContent != null ? sliderContent : panel;
        }

        int columnIndex = createdSliderCount % sliderColumns.Length;
        return sliderColumns[columnIndex];
    }

    private RectTransform CreateFullScreenSliderArea(Transform parent)
    {
        sliderColumns = null;
        return CreateSliderScrollArea(parent, true);
    }

    private static RectTransform CreateSliderColumn(Transform parent, string name)
    {
        GameObject columnObject = new(name, typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
        columnObject.transform.SetParent(parent, false);

        LayoutElement layoutElement = columnObject.GetComponent<LayoutElement>();
        layoutElement.flexibleWidth = 1f;
        layoutElement.flexibleHeight = 1f;

        VerticalLayoutGroup layout = columnObject.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(0, 0, 0, 0);
        layout.spacing = 12f;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        return columnObject.GetComponent<RectTransform>();
    }

    private RectTransform CreateSliderScrollArea(Transform parent, bool fullScreen)
    {
        GameObject scrollObject = new("Input Slider Scroll Area", typeof(RectTransform), typeof(Image), typeof(ScrollRect), typeof(LayoutElement));
        scrollObject.transform.SetParent(parent, false);
        MediTerraniaRuntimeUi.AddLayoutElement(scrollObject, fullScreen ? 560f : 360f, flexibleWidth: 1f);

        LayoutElement layoutElement = scrollObject.GetComponent<LayoutElement>();
        layoutElement.flexibleHeight = fullScreen ? 1f : 0f;

        Image scrollBackground = scrollObject.GetComponent<Image>();
        scrollBackground.sprite = MediTerraniaRuntimeUi.SolidSprite;
        scrollBackground.type = Image.Type.Sliced;
        scrollBackground.color = new Color(0.02f, 0.13f, 0.17f, 0.58f);

        RectTransform scrollRectTransform = scrollObject.GetComponent<RectTransform>();

        RectTransform viewport = CreateBareRect(scrollRectTransform, "Viewport");
        viewport.anchorMin = Vector2.zero;
        viewport.anchorMax = Vector2.one;
        viewport.offsetMin = new Vector2(0f, 0f);
        viewport.offsetMax = new Vector2(-18f, 0f);

        viewport.gameObject.AddComponent<RectMask2D>();

        GameObject contentObject = new("Slider Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        contentObject.transform.SetParent(viewport, false);

        RectTransform content = contentObject.GetComponent<RectTransform>();
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        content.offsetMin = new Vector2(8f, content.offsetMin.y);
        content.offsetMax = new Vector2(-8f, 0f);

        VerticalLayoutGroup contentLayout = contentObject.GetComponent<VerticalLayoutGroup>();
        contentLayout.padding = new RectOffset(0, 0, 8, 8);
        contentLayout.spacing = 6f;
        contentLayout.childAlignment = TextAnchor.UpperLeft;
        contentLayout.childControlWidth = true;
        contentLayout.childControlHeight = false;
        contentLayout.childForceExpandWidth = true;
        contentLayout.childForceExpandHeight = false;

        ContentSizeFitter fitter = contentObject.GetComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        Scrollbar scrollbar = CreateVerticalScrollbar(scrollRectTransform);

        sliderScrollRect = scrollObject.GetComponent<ScrollRect>();
        sliderScrollRect.viewport = viewport;
        sliderScrollRect.content = content;
        sliderScrollRect.horizontal = false;
        sliderScrollRect.vertical = true;
        sliderScrollRect.movementType = ScrollRect.MovementType.Clamped;
        sliderScrollRect.scrollSensitivity = 28f;
        sliderScrollRect.verticalScrollbar = scrollbar;
        sliderScrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
        sliderScrollRect.verticalScrollbarSpacing = 4f;

        return content;
    }

    private static Scrollbar CreateVerticalScrollbar(Transform parent)
    {
        GameObject scrollbarObject = new("Vertical Scrollbar", typeof(RectTransform), typeof(Image), typeof(Scrollbar));
        scrollbarObject.transform.SetParent(parent, false);

        RectTransform scrollbarRect = scrollbarObject.GetComponent<RectTransform>();
        scrollbarRect.anchorMin = new Vector2(1f, 0f);
        scrollbarRect.anchorMax = Vector2.one;
        scrollbarRect.pivot = new Vector2(1f, 0.5f);
        scrollbarRect.offsetMin = new Vector2(-10f, 8f);
        scrollbarRect.offsetMax = new Vector2(0f, -8f);

        Image trackImage = scrollbarObject.GetComponent<Image>();
        trackImage.sprite = MediTerraniaRuntimeUi.SolidSprite;
        trackImage.type = Image.Type.Sliced;
        trackImage.color = new Color(0.03f, 0.18f, 0.23f, 0.9f);

        RectTransform slidingArea = CreateBareRect(scrollbarRect, "Sliding Area");
        slidingArea.anchorMin = Vector2.zero;
        slidingArea.anchorMax = Vector2.one;
        slidingArea.offsetMin = Vector2.zero;
        slidingArea.offsetMax = Vector2.zero;

        RectTransform handle = CreateImageRect(slidingArea, "Handle", MediTerraniaRuntimeUi.AccentColor);
        handle.anchorMin = Vector2.zero;
        handle.anchorMax = Vector2.one;
        handle.offsetMin = Vector2.zero;
        handle.offsetMax = Vector2.zero;

        Scrollbar scrollbar = scrollbarObject.GetComponent<Scrollbar>();
        scrollbar.handleRect = handle;
        scrollbar.targetGraphic = handle.GetComponent<Image>();
        scrollbar.direction = Scrollbar.Direction.BottomToTop;
        return scrollbar;
    }

    private static RectTransform CreateBareRect(Transform parent, string name)
    {
        GameObject rectObject = new(name, typeof(RectTransform));
        rectObject.transform.SetParent(parent, false);
        return rectObject.GetComponent<RectTransform>();
    }

    private static RectTransform CreateImageRect(Transform parent, string name, Color color)
    {
        GameObject imageObject = new(name, typeof(RectTransform), typeof(Image));
        imageObject.transform.SetParent(parent, false);

        Image image = imageObject.GetComponent<Image>();
        image.sprite = MediTerraniaRuntimeUi.SolidSprite;
        image.type = Image.Type.Sliced;
        image.color = color;

        return imageObject.GetComponent<RectTransform>();
    }

    private static void AddWidth(GameObject gameObject, float width)
    {
        LayoutElement layoutElement = gameObject.GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = gameObject.AddComponent<LayoutElement>();
        }

        layoutElement.minWidth = width;
        layoutElement.preferredWidth = width;
        layoutElement.flexibleWidth = 0f;
    }

    private sealed class SliderBinding
    {
        public Slider slider;
        public TMP_Text valueLabel;
        public string valueFormat;
        public Func<float> getValue;
        public Action<float> setValue;
    }
}
