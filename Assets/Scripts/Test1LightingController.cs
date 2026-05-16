using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class Test1LightingController : MonoBehaviour
{
    private const string SceneName = "Test1";
    private const string ControllerObjectName = "Test1 Lighting Controller";
    private const float MinBrightness = 0.5f;
    private const float MaxBrightness = 2.4f;

    [SerializeField, Range(MinBrightness, MaxBrightness)] private float brightness = 1.25f;

    private readonly List<LightState> lights = new();
    private ColorAdjustments colorAdjustments;
    private Vignette vignette;
    private Bloom bloom;
    private VolumeComponent volumetricFog;
    private float baseExposure;
    private float baseVignetteIntensity;
    private float baseBloomIntensity;
    private float baseFogDensity;
    private float baseFogAttenuationDistance;
    private Color baseAmbientSky;
    private Color baseAmbientEquator;
    private Color baseAmbientGround;
    private float baseAmbientIntensity;
    private bool hasFogDensity;
    private bool hasFogAttenuationDistance;
    private bool hasCachedSceneState;
    private RectTransform runtimePanel;
    private Slider brightnessSlider;
    private TMP_Text brightnessValueText;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneLoadedCallback()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateForActiveTestScene()
    {
        CreateForScene(SceneManager.GetActiveScene());
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode loadMode)
    {
        CreateForScene(scene);
    }

    private static void CreateForScene(Scene scene)
    {
        if (scene.name != SceneName)
        {
            return;
        }

        if (FindObjectsByType<Test1LightingController>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length > 0)
        {
            return;
        }

        new GameObject(ControllerObjectName).AddComponent<Test1LightingController>();
    }

    private void Awake()
    {
        CacheSceneLighting();
        ApplyBrightness();
        CreateRuntimeUi();
        UpdateRuntimeUi();
    }

    private void OnDestroy()
    {
        if (runtimePanel != null)
        {
            Destroy(runtimePanel.gameObject);
            runtimePanel = null;
        }
    }

    private void OnValidate()
    {
        brightness = Mathf.Clamp(brightness, MinBrightness, MaxBrightness);
    }

    private void CreateRuntimeUi()
    {
    }

    private void SetBrightness(float value)
    {
        if (Mathf.Approximately(value, brightness))
        {
            return;
        }

        brightness = Mathf.Clamp(value, MinBrightness, MaxBrightness);
        ApplyBrightness();
        UpdateRuntimeUi();
    }

    private void CacheSceneLighting()
    {
        lights.Clear();
        Light[] sceneLights = FindObjectsByType<Light>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < sceneLights.Length; i++)
        {
            Light light = sceneLights[i];
            if (light == null)
            {
                continue;
            }

            lights.Add(new LightState(light, light.intensity, light.bounceIntensity));

            UniversalAdditionalLightData lightData = light.GetComponent<UniversalAdditionalLightData>();
            if (lightData != null)
            {
                lightData.softShadowQuality = SoftShadowQuality.High;
            }

            if (light.shadows != LightShadows.None)
            {
                light.shadowStrength = Mathf.Min(light.shadowStrength, light.type == LightType.Directional ? 0.62f : 0.36f);
                light.shadowBias = Mathf.Min(light.shadowBias, 0.035f);
                light.shadowNormalBias = Mathf.Min(light.shadowNormalBias, 0.24f);
            }
        }

        Volume[] volumes = FindObjectsByType<Volume>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < volumes.Length; i++)
        {
            VolumeProfile profile = volumes[i].profile;
            if (profile == null)
            {
                continue;
            }

            profile.TryGet(out colorAdjustments);
            profile.TryGet(out vignette);
            profile.TryGet(out bloom);
            volumetricFog = FindVolumeComponent(profile, "VolumetricFogVolumeComponent");

            if (colorAdjustments != null || vignette != null || bloom != null || volumetricFog != null)
            {
                break;
            }
        }

        if (colorAdjustments != null)
        {
            baseExposure = colorAdjustments.postExposure.value;
            colorAdjustments.postExposure.overrideState = true;
            colorAdjustments.contrast.overrideState = true;
            colorAdjustments.saturation.overrideState = true;
        }

        if (vignette != null)
        {
            baseVignetteIntensity = vignette.intensity.value;
            vignette.intensity.overrideState = true;
        }

        if (bloom != null)
        {
            baseBloomIntensity = bloom.intensity.value;
            bloom.intensity.overrideState = true;
            bloom.highQualityFiltering.overrideState = true;
            bloom.highQualityFiltering.value = true;
        }

        hasFogDensity = TryGetVolumeFloat(volumetricFog, "density", out baseFogDensity);
        hasFogAttenuationDistance = TryGetVolumeFloat(volumetricFog, "attenuationDistance", out baseFogAttenuationDistance);
        TrySetVolumeInt(volumetricFog, "maxSteps", 128);
        TrySetVolumeInt(volumetricFog, "blurIterations", 2);

        baseAmbientSky = RenderSettings.ambientSkyColor;
        baseAmbientEquator = RenderSettings.ambientEquatorColor;
        baseAmbientGround = RenderSettings.ambientGroundColor;
        baseAmbientIntensity = RenderSettings.ambientIntensity;
        hasCachedSceneState = true;
    }

    private void ApplyBrightness()
    {
        if (!hasCachedSceneState)
        {
            return;
        }

        float normalized = Mathf.InverseLerp(MinBrightness, MaxBrightness, brightness);
        float lift = brightness - 1f;

        for (int i = 0; i < lights.Count; i++)
        {
            LightState state = lights[i];
            if (state.Light == null)
            {
                continue;
            }

            float scale = state.Light.type == LightType.Directional
                ? Mathf.Lerp(0.9f, 1.75f, normalized)
                : Mathf.Lerp(0.78f, 1.55f, normalized);

            state.Light.intensity = state.BaseIntensity * scale;
            state.Light.bounceIntensity = Mathf.Max(state.BaseBounceIntensity, state.BaseBounceIntensity * Mathf.Lerp(1f, 1.35f, normalized));
        }

        if (colorAdjustments != null)
        {
            colorAdjustments.postExposure.value = Mathf.Clamp(baseExposure + lift * 0.72f, -0.35f, 0.85f);
            colorAdjustments.contrast.value = Mathf.Lerp(12f, 20f, normalized);
            colorAdjustments.saturation.value = Mathf.Lerp(-18f, -6f, normalized);
        }

        if (vignette != null)
        {
            vignette.intensity.value = Mathf.Clamp01(baseVignetteIntensity - normalized * 0.13f);
        }

        if (bloom != null)
        {
            bloom.intensity.value = Mathf.Clamp(baseBloomIntensity + normalized * 0.22f, 0.1f, 0.55f);
        }

        if (volumetricFog != null)
        {
            if (hasFogDensity)
            {
                TrySetVolumeFloat(volumetricFog, "density", Mathf.Clamp(baseFogDensity * Mathf.Lerp(1.05f, 0.64f, normalized), 0.04f, 0.24f));
            }

            if (hasFogAttenuationDistance)
            {
                TrySetVolumeFloat(volumetricFog, "attenuationDistance", Mathf.Max(baseFogAttenuationDistance, Mathf.Lerp(baseFogAttenuationDistance, 82f, normalized)));
            }
        }

        RenderSettings.ambientIntensity = baseAmbientIntensity * Mathf.Lerp(0.92f, 1.28f, normalized);
        RenderSettings.ambientSkyColor = Color.Lerp(baseAmbientSky * 0.92f, baseAmbientSky * 1.42f, normalized);
        RenderSettings.ambientEquatorColor = Color.Lerp(baseAmbientEquator * 0.95f, baseAmbientEquator * 1.5f, normalized);
        RenderSettings.ambientGroundColor = Color.Lerp(baseAmbientGround * 0.95f, baseAmbientGround * 1.55f, normalized);
    }

    private void UpdateRuntimeUi()
    {
        if (brightnessSlider != null && !Mathf.Approximately(brightnessSlider.value, brightness))
        {
            brightnessSlider.SetValueWithoutNotify(brightness);
        }

        if (brightnessValueText != null)
        {
            brightnessValueText.text = $"{brightness:0.00}x";
        }
    }

    private static VolumeComponent FindVolumeComponent(VolumeProfile profile, string componentName)
    {
        if (profile == null)
        {
            return null;
        }

        for (int i = 0; i < profile.components.Count; i++)
        {
            VolumeComponent component = profile.components[i];
            if (component != null && component.GetType().Name == componentName)
            {
                return component;
            }
        }

        return null;
    }

    private static bool TryGetVolumeFloat(VolumeComponent component, string fieldName, out float value)
    {
        value = 0f;
        object parameter = GetVolumeParameter(component, fieldName);
        if (parameter == null)
        {
            return false;
        }

        PropertyInfo valueProperty = parameter.GetType().GetProperty("value");
        if (valueProperty == null || valueProperty.PropertyType != typeof(float))
        {
            return false;
        }

        value = (float)valueProperty.GetValue(parameter);
        return true;
    }

    private static bool TrySetVolumeFloat(VolumeComponent component, string fieldName, float value)
    {
        object parameter = GetVolumeParameter(component, fieldName);
        if (parameter == null)
        {
            return false;
        }

        PropertyInfo valueProperty = parameter.GetType().GetProperty("value");
        if (valueProperty == null || valueProperty.PropertyType != typeof(float))
        {
            return false;
        }

        valueProperty.SetValue(parameter, value);
        return true;
    }

    private static bool TrySetVolumeInt(VolumeComponent component, string fieldName, int value)
    {
        object parameter = GetVolumeParameter(component, fieldName);
        if (parameter == null)
        {
            return false;
        }

        PropertyInfo valueProperty = parameter.GetType().GetProperty("value");
        if (valueProperty == null || valueProperty.PropertyType != typeof(int))
        {
            return false;
        }

        valueProperty.SetValue(parameter, value);
        return true;
    }

    private static object GetVolumeParameter(VolumeComponent component, string fieldName)
    {
        if (component == null)
        {
            return null;
        }

        const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        FieldInfo field = component.GetType().GetField(fieldName, Flags);
        return field?.GetValue(component);
    }

    private readonly struct LightState
    {
        public LightState(Light light, float baseIntensity, float baseBounceIntensity)
        {
            Light = light;
            BaseIntensity = baseIntensity;
            BaseBounceIntensity = baseBounceIntensity;
        }

        public Light Light { get; }
        public float BaseIntensity { get; }
        public float BaseBounceIntensity { get; }
    }
}
