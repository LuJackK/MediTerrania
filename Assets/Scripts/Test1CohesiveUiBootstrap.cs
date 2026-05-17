using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
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
    private Image depthShade;
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
        depthShade = MediTerraniaRuntimeUi.EnsureDepthShade(canvas);
        RectTransform rightColumn = MediTerraniaRuntimeUi.EnsureRightColumn(canvas);

        temperaturePanel = InstallTemperaturePanel(rightColumn, out createdTemperatureController);
        anchorPanel = InstallAnchorPanel(rightColumn, depthShade);
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

        if (depthShade != null)
        {
            Destroy(depthShade.gameObject);
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
        panel.sizeDelta = new Vector2(320f, 194f);
        MediTerraniaRuntimeUi.AddLayoutElement(panel.gameObject, 194f, preferredWidth: 320f);
        StyleTemperaturePanel(panel);

        controller.temperatureText = FindNamed<TMP_Text>(panel.gameObject, "Temperature Text");
        controller.thermometerFill = FindNamed<Image>(panel.gameObject, "Thermometer Fill");
        controller.RefreshUI();

        WireButton(panel.gameObject, "UP button", controller.IncreaseTemperature);
        WireButton(panel.gameObject, "DOWN button", controller.DecreaseTemperature);

        DestroyTemporaryRoot(temporaryRoot);
        return panel;
    }

    private static RectTransform InstallAnchorPanel(RectTransform rightColumn, Image depthShade)
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
            new Vector2(320f, 272f));
        MediTerraniaRuntimeUi.CreateTitle(panel, "Anchor Depth");

        GameObject wellObject = new("Anchor Depth Well", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        wellObject.transform.SetParent(panel, false);
        MediTerraniaRuntimeUi.AddLayoutElement(wellObject, 214f);

        RectTransform wellRect = wellObject.GetComponent<RectTransform>();
        Image wellImage = wellObject.GetComponent<Image>();
        wellImage.sprite = MediTerraniaRuntimeUi.RoundedSprite;
        wellImage.type = Image.Type.Sliced;
        wellImage.color = new Color(0.09f, 0.34f, 0.45f, 0.02f);
        wellImage.raycastTarget = true;

        RectTransform track = FindNamed<RectTransform>(temporaryRoot, "Anchor Track");
        RectTransform anchor = anchorDrag.GetComponent<RectTransform>();
        TMP_Text depthText = FindNamed<TMP_Text>(temporaryRoot, "Depth Text");

        if (track == null || anchor == null || depthText == null)
        {
            DestroyTemporaryRoot(temporaryRoot);
            return panel;
        }

        RectTransform anchorLane = CreateAnchorLane(wellRect);
        anchorLane.SetAsFirstSibling();

        track.SetParent(wellRect, false);
        anchor.SetParent(wellRect, false);
        depthText.rectTransform.SetParent(wellRect, false);

        track.anchoredPosition = new Vector2(-54f, -18f);
        track.sizeDelta = new Vector2(16f, 128f);
        Image trackImage = track.GetComponent<Image>();
        if (trackImage != null)
        {
            trackImage.sprite = MediTerraniaRuntimeUi.RoundedSprite;
            trackImage.type = Image.Type.Sliced;
            trackImage.color = new Color(0.035f, 0.18f, 0.22f, 0.82f);
        }

        Image trackFill = CreateAnchorTrackFill(track);
        anchor.anchoredPosition = new Vector2(-54f, 46f);
        anchor.sizeDelta = new Vector2(86f, 86f);
        depthText.color = Color.white;
        depthText.rectTransform.pivot = new Vector2(0f, 0.5f);
        depthText.rectTransform.anchoredPosition = new Vector2(26f, 46f);
        depthText.rectTransform.sizeDelta = new Vector2(110f, 34f);

        anchorDrag.track = track;
        anchorDrag.trackFill = trackFill;
        anchorDrag.depthText = depthText;
        anchorDrag.darknessOverlay = depthShade;
        anchorDrag.controlledCamera = Camera.main;
        anchorDrag.CaptureCurrentCameraAsShallow();
        anchorDrag.RefreshDepth();
        ConfigureAnchorDragArea(wellObject, anchorDrag);

        DestroyTemporaryRoot(temporaryRoot);
        return panel;
    }

    private static RectTransform CreateAnchorLane(RectTransform parent)
    {
        GameObject laneObject = new("Anchor Vertical Lane", typeof(RectTransform), typeof(Image));
        laneObject.transform.SetParent(parent, false);

        RectTransform laneRect = laneObject.GetComponent<RectTransform>();
        laneRect.anchorMin = new Vector2(0.5f, 0.5f);
        laneRect.anchorMax = new Vector2(0.5f, 0.5f);
        laneRect.pivot = new Vector2(0.5f, 0.5f);
        laneRect.anchoredPosition = new Vector2(-54f, -18f);
        laneRect.sizeDelta = new Vector2(104f, 184f);

        Image laneImage = laneObject.GetComponent<Image>();
        laneImage.sprite = MediTerraniaRuntimeUi.RoundedSprite;
        laneImage.type = Image.Type.Sliced;
        laneImage.color = new Color(0.06f, 0.23f, 0.3f, 0.34f);
        laneImage.raycastTarget = false;
        return laneRect;
    }

    private static Image CreateAnchorTrackFill(RectTransform track)
    {
        Transform existing = track.Find("Anchor Track Fill");
        GameObject fillObject = existing != null ? existing.gameObject : new GameObject("Anchor Track Fill", typeof(RectTransform), typeof(Image));
        fillObject.transform.SetParent(track, false);

        RectTransform fillRect = fillObject.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        Image fillImage = fillObject.GetComponent<Image>();
        fillImage.sprite = MediTerraniaRuntimeUi.RoundedSprite;
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Vertical;
        fillImage.fillOrigin = (int)Image.OriginVertical.Top;
        fillImage.fillAmount = 0f;
        fillImage.color = new Color(0.5f, 0.28f, 0.12f, 0.95f);
        fillImage.raycastTarget = false;
        return fillImage;
    }

    private static void ConfigureAnchorDragArea(GameObject dragArea, AnchorDrag anchorDrag)
    {
        if (dragArea == null || anchorDrag == null)
        {
            return;
        }

        EventTrigger trigger = dragArea.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = dragArea.AddComponent<EventTrigger>();
        }

        trigger.triggers.Clear();
        AddAnchorDragEvent(trigger, EventTriggerType.PointerDown, anchorDrag);
        AddAnchorDragEvent(trigger, EventTriggerType.BeginDrag, anchorDrag);
        AddAnchorDragEvent(trigger, EventTriggerType.Drag, anchorDrag);
    }

    private static void AddAnchorDragEvent(EventTrigger trigger, EventTriggerType triggerType, AnchorDrag anchorDrag)
    {
        EventTrigger.Entry entry = new()
        {
            eventID = triggerType
        };
        entry.callback.AddListener(eventData => anchorDrag.MoveToPointer((PointerEventData)eventData));
        trigger.triggers.Add(entry);
    }

    private static void StyleTemperaturePanel(RectTransform panel)
    {
        Image panelImage = panel.GetComponent<Image>();
        if (panelImage != null)
        {
            panelImage.sprite = MediTerraniaRuntimeUi.RoundedSprite;
            panelImage.type = Image.Type.Sliced;
            panelImage.color = MediTerraniaRuntimeUi.PanelColor;
        }

        Outline outline = panel.GetComponent<Outline>();
        if (outline == null)
        {
            outline = panel.gameObject.AddComponent<Outline>();
        }

        outline.effectColor = MediTerraniaRuntimeUi.PanelStrokeColor;
        outline.effectDistance = new Vector2(1f, -1f);

        TMP_Text temperatureText = FindNamed<TMP_Text>(panel.gameObject, "Temperature Text");
        if (temperatureText != null)
        {
            temperatureText.color = MediTerraniaRuntimeUi.TextColor;
            temperatureText.fontSize = 30f;
            temperatureText.fontStyle = FontStyles.Bold;
            temperatureText.rectTransform.anchoredPosition = new Vector2(64f, -72f);
        }

        EnsureTemperatureLabel(panel);
        PositionTemperatureElement(panel.gameObject, "Thermometer Image", new Vector2(-76f, -15f), new Vector2(62f, 150f));
        PositionTemperatureElement(panel.gameObject, "Thermometer Fill", new Vector2(-76f, -15f), new Vector2(62f, 150f));

        RectTransform upButton = FindNamed<RectTransform>(panel.gameObject, "UP button");
        RectTransform downButton = FindNamed<RectTransform>(panel.gameObject, "DOWN button");
        if (upButton != null)
        {
            upButton.anchoredPosition = new Vector2(64f, 28f);
            upButton.sizeDelta = new Vector2(54f, 46f);
        }

        if (downButton != null)
        {
            downButton.anchoredPosition = new Vector2(64f, -22f);
            downButton.sizeDelta = new Vector2(54f, 46f);
        }
    }

    private static void EnsureTemperatureLabel(RectTransform panel)
    {
        TMP_Text label = FindNamed<TMP_Text>(panel.gameObject, "Temperature Label");
        if (label == null)
        {
            GameObject labelObject = new("Temperature Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(panel, false);
            label = labelObject.GetComponent<TMP_Text>();
        }

        label.text = "Temperature";
        label.fontSize = 15f;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.color = MediTerraniaRuntimeUi.MutedTextColor;
        label.raycastTarget = false;

        RectTransform rectTransform = label.rectTransform;
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = new Vector2(0f, 76f);
        rectTransform.sizeDelta = new Vector2(250f, 22f);
    }

    private static void PositionTemperatureElement(GameObject panel, string objectName, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        RectTransform rectTransform = FindNamed<RectTransform>(panel, objectName);
        if (rectTransform == null)
        {
            return;
        }

        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = sizeDelta;
    }

    private static void WireButton(GameObject root, string buttonName, UnityEngine.Events.UnityAction action)
    {
        Button button = FindNamed<Button>(root, buttonName);
        if (button != null)
        {
            button.interactable = true;
            if (button.targetGraphic == null)
            {
                button.targetGraphic = button.GetComponent<Image>();
            }

            Image buttonImage = button.GetComponent<Image>();
            if (buttonImage != null)
            {
                buttonImage.raycastTarget = true;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(action);
            EnsureTemperatureClickTarget(button, action);
        }
    }

    private static void EnsureTemperatureClickTarget(Button sourceButton, UnityEngine.Events.UnityAction action)
    {
        RectTransform sourceRect = sourceButton.GetComponent<RectTransform>();
        if (sourceRect == null)
        {
            return;
        }

        Transform existing = sourceRect.Find("Runtime Click Target");
        GameObject targetObject = existing != null ? existing.gameObject : new GameObject("Runtime Click Target", typeof(RectTransform), typeof(Image), typeof(Button));
        targetObject.transform.SetParent(sourceRect, false);
        targetObject.transform.SetAsLastSibling();

        RectTransform targetRect = targetObject.GetComponent<RectTransform>();
        targetRect.anchorMin = Vector2.zero;
        targetRect.anchorMax = Vector2.one;
        targetRect.offsetMin = Vector2.zero;
        targetRect.offsetMax = Vector2.zero;

        Image targetImage = targetObject.GetComponent<Image>();
        targetImage.sprite = MediTerraniaRuntimeUi.SolidSprite;
        targetImage.color = new Color(1f, 1f, 1f, 0.01f);
        targetImage.raycastTarget = true;

        Button targetButton = targetObject.GetComponent<Button>();
        targetButton.targetGraphic = targetImage;
        targetButton.onClick.RemoveAllListeners();
        targetButton.onClick.AddListener(action);
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
