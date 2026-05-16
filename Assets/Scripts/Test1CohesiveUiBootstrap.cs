using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

public sealed class Test1CohesiveUiBootstrap : MonoBehaviour
{
    private const string SceneName = "Test1";
    private const string BootstrapObjectName = "Test1 Cohesive UI Bootstrap";
    private const string TemperatureControllerName = "Sea Temperature Controller";
    private const string TemperaturePrefabPath = "Assets/UI/Thermometer Canvas.prefab";
    private const string AnchorPrefabPath = "Assets/UI - Anchor/Anchor Canvas.prefab";

    private RectTransform temperaturePanel;
    private RectTransform anchorPanel;
    private SeaTemperatureController createdTemperatureController;

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

        if (FindObjectsByType<Test1CohesiveUiBootstrap>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length > 0)
        {
            return;
        }

        new GameObject(BootstrapObjectName).AddComponent<Test1CohesiveUiBootstrap>();
    }

    private void Awake()
    {
        Canvas canvas = MediTerraniaRuntimeUi.EnsureCanvas();
        RectTransform rightColumn = MediTerraniaRuntimeUi.EnsureRightColumn(canvas);

        temperaturePanel = InstallTemperaturePanel(rightColumn, out createdTemperatureController);
        anchorPanel = InstallAnchorPanel(rightColumn);
    }

    private void OnDestroy()
    {
        if (temperaturePanel != null)
        {
            Destroy(temperaturePanel.gameObject);
        }

        if (anchorPanel != null)
        {
            Destroy(anchorPanel.gameObject);
        }

        if (createdTemperatureController != null)
        {
            Destroy(createdTemperatureController.gameObject);
        }
    }

    private static RectTransform InstallTemperaturePanel(
        RectTransform rightColumn,
        out SeaTemperatureController createdController)
    {
        createdController = null;
        SeaTemperatureController controller = FindFirstObjectByType<SeaTemperatureController>(FindObjectsInactive.Include);
        if (controller == null)
        {
            controller = new GameObject(TemperatureControllerName).AddComponent<SeaTemperatureController>();
            createdController = controller;
        }

        RectTransform panel = FindNamed<RectTransform>("Temperature Panel");
        GameObject temporaryRoot = null;

        if (panel == null)
        {
            temporaryRoot = InstantiatePrefab(TemperaturePrefabPath);
            panel = FindNamed<RectTransform>(temporaryRoot, "Temperature Panel");
        }

        if (panel == null)
        {
            DestroyTemporaryRoot(temporaryRoot);
            return null;
        }

        panel.SetParent(rightColumn, false);
        panel.SetAsFirstSibling();
        panel.sizeDelta = new Vector2(350f, 260f);
        MediTerraniaRuntimeUi.AddLayoutElement(panel.gameObject, 260f, preferredWidth: 350f);

        controller.temperatureText = FindNamed<TMP_Text>(panel.gameObject, "Temperature Text");
        controller.thermometerFill = FindNamed<Image>(panel.gameObject, "Thermometer Fill");
        controller.RefreshUI();

        WireButton(panel.gameObject, "UP button", controller.IncreaseTemperature);
        WireButton(panel.gameObject, "DOWN button", controller.DecreaseTemperature);

        DestroyTemporaryRoot(temporaryRoot);
        return panel;
    }

    private static RectTransform InstallAnchorPanel(RectTransform rightColumn)
    {
        RectTransform existingPanel = FindNamed<RectTransform>("Anchor Controls");
        if (existingPanel != null)
        {
            return existingPanel;
        }

        GameObject temporaryRoot = InstantiatePrefab(AnchorPrefabPath);
        RectTransform sourceRoot = temporaryRoot != null ? temporaryRoot.GetComponent<RectTransform>() : null;
        AnchorDrag anchorDrag = temporaryRoot != null ? temporaryRoot.GetComponentInChildren<AnchorDrag>(true) : null;

        if (sourceRoot == null || anchorDrag == null)
        {
            DestroyTemporaryRoot(temporaryRoot);
            return null;
        }

        RectTransform panel = MediTerraniaRuntimeUi.CreatePanel(
            rightColumn,
            "Anchor Controls",
            new Vector2(350f, 300f));
        MediTerraniaRuntimeUi.CreateTitle(panel, "Anchor Depth");

        GameObject wellObject = new("Anchor Depth Well", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        wellObject.transform.SetParent(panel, false);
        MediTerraniaRuntimeUi.AddLayoutElement(wellObject, 242f);

        RectTransform wellRect = wellObject.GetComponent<RectTransform>();
        Image wellImage = wellObject.GetComponent<Image>();
        wellImage.sprite = MediTerraniaRuntimeUi.SolidSprite;
        wellImage.type = Image.Type.Sliced;
        wellImage.color = new Color(0.02f, 0.13f, 0.17f, 0.68f);
        wellImage.raycastTarget = false;

        RectTransform track = FindNamed<RectTransform>(temporaryRoot, "Anchor Track");
        RectTransform anchor = anchorDrag.GetComponent<RectTransform>();
        TMP_Text depthText = FindNamed<TMP_Text>(temporaryRoot, "Depth Text");

        if (track == null || anchor == null || depthText == null)
        {
            DestroyTemporaryRoot(temporaryRoot);
            return panel;
        }

        track.SetParent(wellRect, false);
        anchor.SetParent(wellRect, false);
        depthText.rectTransform.SetParent(wellRect, false);

        track.anchoredPosition = new Vector2(-64f, 0f);
        track.sizeDelta = new Vector2(5f, 178f);
        anchor.anchoredPosition = new Vector2(-64f, 89f);
        anchor.sizeDelta = new Vector2(86f, 86f);
        depthText.rectTransform.pivot = new Vector2(0f, 0.5f);
        depthText.rectTransform.anchoredPosition = new Vector2(16f, 89f);
        depthText.rectTransform.sizeDelta = new Vector2(110f, 34f);

        anchorDrag.track = track;
        anchorDrag.depthText = depthText;
        anchorDrag.RefreshDepth();

        DestroyTemporaryRoot(temporaryRoot);
        return panel;
    }

    private static void WireButton(GameObject root, string buttonName, UnityEngine.Events.UnityAction action)
    {
        Button button = FindNamed<Button>(root, buttonName);
        if (button != null)
        {
            button.onClick.AddListener(action);
        }
    }

    private static T FindNamed<T>(string objectName) where T : Component
    {
        T[] components = FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < components.Length; i++)
        {
            if (components[i].name == objectName)
            {
                return components[i];
            }
        }

        return null;
    }

    private static T FindNamed<T>(GameObject root, string objectName) where T : Component
    {
        if (root == null)
        {
            return null;
        }

        T[] components = root.GetComponentsInChildren<T>(true);
        for (int i = 0; i < components.Length; i++)
        {
            if (components[i].name == objectName)
            {
                return components[i];
            }
        }

        return null;
    }

    private static GameObject InstantiatePrefab(string assetPath)
    {
#if UNITY_EDITOR
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        return prefab != null ? Instantiate(prefab) : null;
#else
        return null;
#endif
    }

    private static void DestroyTemporaryRoot(GameObject temporaryRoot)
    {
        if (temporaryRoot != null)
        {
            Destroy(temporaryRoot);
        }
    }
}
