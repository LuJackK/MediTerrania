using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

#if UNITY_EDITOR
using UnityEditor;
#endif

public sealed class HabitatRopeController : MonoBehaviour
{
    [SerializeField] private GameObject initialHabitat;
    [SerializeField] private List<GameObject> habitatPrefabs = new();
    [SerializeField] private bool createRuntimeUi = true;
    [SerializeField] private Vector2 uiOffset = new(24f, -24f);

    private const string ControllerObjectName = "Habitat Rope Controller";
    private const string CanvasObjectName = "Habitat Rope UI";

    private static readonly string[] EditorHabitatAssetPaths =
    {
        "Assets/Models/habitat1.fbx",
        "Assets/Models/habitat2.fbx",
        "Assets/Models/habitat3.fbx"
    };

    private readonly List<HabitatEntry> habitats = new();
    private readonly List<GameObject> currentRopes = new();

    private Transform spawnParent;
    private Vector3 spawnPosition;
    private Quaternion spawnRotation;
    private Vector3 spawnScale = Vector3.one;
    private GameObject currentHabitat;
    private int selectedHabitatIndex;
    private int visibleRopeCount;

    private Dropdown habitatDropdown;
    private Button addRopeButton;
    private Button resetRopesButton;
    private Text ropeCountText;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateForTestScene()
    {
        if (SceneManager.GetActiveScene().name != "Test1")
        {
            return;
        }

        if (FindObjectsByType<HabitatRopeController>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length > 0)
        {
            return;
        }

        new GameObject(ControllerObjectName).AddComponent<HabitatRopeController>();
    }

    private void Awake()
    {
        CaptureSpawnTransform();
        BuildHabitatList();

        if (habitats.Count == 0)
        {
            Debug.LogWarning("No habitat models were found for the rope controller.");
            enabled = false;
            return;
        }

        if (createRuntimeUi)
        {
            BuildUi();
        }

        SelectHabitat(0);
    }

    public void AddNextRope()
    {
        if (visibleRopeCount >= currentRopes.Count)
        {
            UpdateUiState();
            return;
        }

        currentRopes[visibleRopeCount].SetActive(true);
        visibleRopeCount++;
        UpdateUiState();
    }

    public void ResetRopes()
    {
        for (int i = 0; i < currentRopes.Count; i++)
        {
            currentRopes[i].SetActive(false);
        }

        visibleRopeCount = 0;
        UpdateUiState();
    }

    private void SelectHabitat(int habitatIndex)
    {
        if (habitatIndex < 0 || habitatIndex >= habitats.Count)
        {
            return;
        }

        selectedHabitatIndex = habitatIndex;

        if (currentHabitat != null)
        {
            Destroy(currentHabitat);
            currentHabitat = null;
        }

        HabitatEntry habitat = habitats[selectedHabitatIndex];
        currentHabitat = Instantiate(habitat.Source, spawnPosition, spawnRotation, spawnParent);
        currentHabitat.name = habitat.DisplayName;
        currentHabitat.transform.localScale = spawnScale;
        currentHabitat.SetActive(true);

        CollectRopes(currentHabitat.transform, currentRopes);
        ResetRopes();
    }

    private void CaptureSpawnTransform()
    {
        if (initialHabitat == null)
        {
            initialHabitat = FindSceneHabitat();
        }

        if (initialHabitat == null)
        {
            spawnPosition = Vector3.zero;
            spawnRotation = Quaternion.identity;
            spawnScale = Vector3.one;
            return;
        }

        Transform initialTransform = initialHabitat.transform;
        spawnParent = initialTransform.parent;
        spawnPosition = initialTransform.position;
        spawnRotation = initialTransform.rotation;
        spawnScale = initialTransform.localScale;
        initialHabitat.SetActive(false);
    }

    private void BuildHabitatList()
    {
        habitats.Clear();

        AddHabitatPrefab(initialHabitat);

        for (int i = 0; i < habitatPrefabs.Count; i++)
        {
            AddHabitatPrefab(habitatPrefabs[i]);
        }

#if UNITY_EDITOR
        for (int i = 0; i < EditorHabitatAssetPaths.Length; i++)
        {
            GameObject habitatAsset = AssetDatabase.LoadAssetAtPath<GameObject>(EditorHabitatAssetPaths[i]);
            AddHabitatPrefab(habitatAsset);
        }
#endif
    }

    private void AddHabitatPrefab(GameObject habitat)
    {
        if (habitat == null)
        {
            return;
        }

        string displayName = habitat.name;
        for (int i = 0; i < habitats.Count; i++)
        {
            if (habitats[i].DisplayName == displayName)
            {
                return;
            }
        }

        habitats.Add(new HabitatEntry(displayName, habitat));
    }

    private static GameObject FindSceneHabitat()
    {
        GameObject[] sceneObjects = FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < sceneObjects.Length; i++)
        {
            GameObject candidate = sceneObjects[i];
            if (!candidate.scene.IsValid())
            {
                continue;
            }

            if (candidate.transform.parent == null && IsHabitatName(candidate.name))
            {
                return candidate;
            }
        }

        return null;
    }

    private static bool IsHabitatName(string objectName)
    {
        return objectName.StartsWith("habitat", System.StringComparison.OrdinalIgnoreCase);
    }

    private static void CollectRopes(Transform root, List<GameObject> ropes)
    {
        ropes.Clear();
        Transform[] transforms = root.GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < transforms.Length; i++)
        {
            Transform candidate = transforms[i];
            if (candidate == root)
            {
                continue;
            }

            if (IsRopeName(candidate.name))
            {
                ropes.Add(candidate.gameObject);
            }
        }

        ropes.Sort(CompareRopeObjects);
    }

    private static bool IsRopeName(string objectName)
    {
        string normalized = RemoveDiacritics(objectName).ToLowerInvariant();
        return normalized.Contains("beziercurve") || normalized.Contains("breziercurve");
    }

    private static int CompareRopeObjects(GameObject left, GameObject right)
    {
        int leftNumber = GetNumericSuffix(left.name);
        int rightNumber = GetNumericSuffix(right.name);

        if (leftNumber != rightNumber)
        {
            return leftNumber.CompareTo(rightNumber);
        }

        return string.Compare(left.name, right.name, System.StringComparison.OrdinalIgnoreCase);
    }

    private static int GetNumericSuffix(string objectName)
    {
        int dotIndex = objectName.LastIndexOf('.');
        if (dotIndex < 0 || dotIndex >= objectName.Length - 1)
        {
            return 0;
        }

        return int.TryParse(objectName[(dotIndex + 1)..], out int value) ? value : 0;
    }

    private static string RemoveDiacritics(string value)
    {
        string normalized = value.Normalize(NormalizationForm.FormD);
        StringBuilder builder = new(normalized.Length);

        for (int i = 0; i < normalized.Length; i++)
        {
            UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(normalized[i]);
            if (category != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(normalized[i]);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    private void BuildUi()
    {
        EnsureEventSystem();

        Canvas existingCanvas = FindCanvas();
        Canvas canvas = existingCanvas != null ? existingCanvas : CreateCanvas();
        Transform panel = CreatePanel(canvas.transform);

        Text title = CreateText("Title", panel, "Habitat", 18, FontStyle.Bold);
        SetRect(title.rectTransform, new Vector2(16f, -12f), new Vector2(248f, 28f));

        habitatDropdown = CreateDropdown(panel);
        SetRect(habitatDropdown.GetComponent<RectTransform>(), new Vector2(16f, -48f), new Vector2(248f, 34f));
        habitatDropdown.ClearOptions();

        List<string> options = new();
        for (int i = 0; i < habitats.Count; i++)
        {
            options.Add(habitats[i].DisplayName);
        }

        habitatDropdown.AddOptions(options);
        habitatDropdown.onValueChanged.AddListener(SelectHabitat);

        addRopeButton = CreateButton("AddRopeButton", panel, "Add rope");
        SetRect(addRopeButton.GetComponent<RectTransform>(), new Vector2(16f, -92f), new Vector2(118f, 34f));
        addRopeButton.onClick.AddListener(AddNextRope);

        resetRopesButton = CreateButton("ResetRopesButton", panel, "Reset");
        SetRect(resetRopesButton.GetComponent<RectTransform>(), new Vector2(146f, -92f), new Vector2(118f, 34f));
        resetRopesButton.onClick.AddListener(ResetRopes);

        ropeCountText = CreateText("RopeCount", panel, string.Empty, 14, FontStyle.Normal);
        SetRect(ropeCountText.rectTransform, new Vector2(16f, -132f), new Vector2(248f, 24f));
    }

    private static void EnsureEventSystem()
    {
        EventSystem eventSystem = FindFirstObjectByType<EventSystem>(FindObjectsInactive.Include);
        if (eventSystem == null)
        {
            GameObject eventSystemObject = new("EventSystem");
            eventSystem = eventSystemObject.AddComponent<EventSystem>();
        }

#if ENABLE_INPUT_SYSTEM
        if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
        {
            eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
        }

        StandaloneInputModule standaloneInputModule = eventSystem.GetComponent<StandaloneInputModule>();
        if (standaloneInputModule != null)
        {
            standaloneInputModule.enabled = false;
        }
#else
        if (eventSystem.GetComponent<BaseInputModule>() == null)
        {
            eventSystem.gameObject.AddComponent<StandaloneInputModule>();
        }
#endif
    }

    private static Canvas FindCanvas()
    {
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < canvases.Length; i++)
        {
            if (canvases[i].renderMode == RenderMode.ScreenSpaceOverlay)
            {
                return canvases[i];
            }
        }

        return null;
    }

    private static Canvas CreateCanvas()
    {
        GameObject canvasObject = new(CanvasObjectName, typeof(RectTransform));
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObject.AddComponent<CanvasScaler>();
        canvasObject.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    private Transform CreatePanel(Transform parent)
    {
        GameObject panelObject = new("HabitatRopePanel", typeof(RectTransform));
        panelObject.transform.SetParent(parent, false);

        Image panelImage = panelObject.AddComponent<Image>();
        panelImage.color = new Color(0.04f, 0.08f, 0.1f, 0.82f);

        RectTransform rectTransform = panelObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0f, 1f);
        rectTransform.anchorMax = new Vector2(0f, 1f);
        rectTransform.pivot = new Vector2(0f, 1f);
        rectTransform.anchoredPosition = uiOffset;
        rectTransform.sizeDelta = new Vector2(280f, 168f);

        return panelObject.transform;
    }

    private static Dropdown CreateDropdown(Transform parent)
    {
        GameObject dropdownObject = new("HabitatDropdown", typeof(RectTransform));
        dropdownObject.transform.SetParent(parent, false);

        Image image = dropdownObject.AddComponent<Image>();
        image.color = new Color(0.95f, 0.96f, 0.94f, 1f);

        Dropdown dropdown = dropdownObject.AddComponent<Dropdown>();

        Text label = CreateText("Label", dropdownObject.transform, string.Empty, 14, FontStyle.Normal);
        label.color = new Color(0.08f, 0.09f, 0.1f, 1f);
        SetRect(label.rectTransform, new Vector2(10f, -3f), new Vector2(204f, 28f));
        dropdown.captionText = label;

        Text arrow = CreateText("Arrow", dropdownObject.transform, "v", 16, FontStyle.Bold);
        arrow.color = new Color(0.08f, 0.09f, 0.1f, 1f);
        arrow.alignment = TextAnchor.MiddleCenter;
        SetRect(arrow.rectTransform, new Vector2(216f, -3f), new Vector2(24f, 28f));

        RectTransform template = CreateDropdownTemplate(dropdownObject.transform, label.font);
        dropdown.template = template;
        dropdown.itemText = template.GetComponentInChildren<Toggle>(true).GetComponentInChildren<Text>(true);

        return dropdown;
    }

    private static RectTransform CreateDropdownTemplate(Transform parent, Font font)
    {
        GameObject templateObject = new("Template", typeof(RectTransform));
        templateObject.SetActive(false);
        templateObject.transform.SetParent(parent, false);

        Image templateImage = templateObject.AddComponent<Image>();
        templateImage.color = new Color(0.95f, 0.96f, 0.94f, 1f);

        ScrollRect scrollRect = templateObject.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;

        RectTransform templateRect = templateObject.GetComponent<RectTransform>();
        SetRect(templateRect, new Vector2(0f, -36f), new Vector2(248f, 108f));

        GameObject viewportObject = new("Viewport", typeof(RectTransform));
        viewportObject.transform.SetParent(templateObject.transform, false);
        Image viewportImage = viewportObject.AddComponent<Image>();
        viewportImage.color = new Color(1f, 1f, 1f, 0.08f);
        Mask mask = viewportObject.AddComponent<Mask>();
        mask.showMaskGraphic = false;
        RectTransform viewportRect = viewportObject.GetComponent<RectTransform>();
        Stretch(viewportRect);

        GameObject contentObject = new("Content", typeof(RectTransform));
        contentObject.transform.SetParent(viewportObject.transform, false);
        RectTransform contentRect = contentObject.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(0f, 34f);

        GameObject itemObject = new("Item", typeof(RectTransform));
        itemObject.transform.SetParent(contentObject.transform, false);
        Toggle toggle = itemObject.AddComponent<Toggle>();
        Image itemImage = itemObject.AddComponent<Image>();
        itemImage.color = new Color(0.95f, 0.96f, 0.94f, 1f);
        toggle.targetGraphic = itemImage;
        toggle.graphic = null;

        RectTransform itemRect = itemObject.GetComponent<RectTransform>();
        itemRect.anchorMin = new Vector2(0f, 0.5f);
        itemRect.anchorMax = new Vector2(1f, 0.5f);
        itemRect.pivot = new Vector2(0.5f, 0.5f);
        itemRect.sizeDelta = new Vector2(0f, 32f);

        GameObject itemTextObject = new("Item Label", typeof(RectTransform));
        itemTextObject.transform.SetParent(itemObject.transform, false);

        Text itemText = itemTextObject.AddComponent<Text>();
        itemText.font = font;
        itemText.fontSize = 14;
        itemText.color = new Color(0.08f, 0.09f, 0.1f, 1f);
        itemText.alignment = TextAnchor.MiddleLeft;
        itemText.raycastTarget = false;
        itemText.supportRichText = false;

        RectTransform itemTextRect = itemText.rectTransform;
        itemTextRect.anchorMin = new Vector2(0f, 0f);
        itemTextRect.anchorMax = new Vector2(1f, 1f);
        itemTextRect.offsetMin = new Vector2(10f, 0f);
        itemTextRect.offsetMax = new Vector2(-10f, 0f);

        scrollRect.viewport = viewportRect;
        scrollRect.content = contentRect;

        return templateRect;
    }

    private static Button CreateButton(string objectName, Transform parent, string label)
    {
        GameObject buttonObject = new(objectName, typeof(RectTransform));
        buttonObject.transform.SetParent(parent, false);

        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.87f, 0.62f, 0.24f, 1f);

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;

        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(0.98f, 0.76f, 0.36f, 1f);
        colors.pressedColor = new Color(0.64f, 0.42f, 0.14f, 1f);
        colors.disabledColor = new Color(0.35f, 0.36f, 0.36f, 0.8f);
        button.colors = colors;

        Text text = CreateText("Text", buttonObject.transform, label, 14, FontStyle.Bold);
        text.alignment = TextAnchor.MiddleCenter;
        text.color = new Color(0.05f, 0.06f, 0.07f, 1f);
        Stretch(text.rectTransform);

        return button;
    }

    private static Text CreateText(string objectName, Transform parent, string value, int fontSize, FontStyle fontStyle)
    {
        GameObject textObject = new(objectName, typeof(RectTransform));
        textObject.transform.SetParent(parent, false);

        Text text = textObject.AddComponent<Text>();
        text.text = value;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ??
            Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleLeft;
        text.raycastTarget = false;
        text.supportRichText = false;

        return text;
    }

    private static void SetRect(RectTransform rectTransform, Vector2 anchoredPosition, Vector2 size)
    {
        rectTransform.anchorMin = new Vector2(0f, 1f);
        rectTransform.anchorMax = new Vector2(0f, 1f);
        rectTransform.pivot = new Vector2(0f, 1f);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = size;
    }

    private static void Stretch(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }

    private void UpdateUiState()
    {
        if (addRopeButton != null)
        {
            addRopeButton.interactable = visibleRopeCount < currentRopes.Count;
        }

        if (resetRopesButton != null)
        {
            resetRopesButton.interactable = visibleRopeCount > 0;
        }

        if (ropeCountText != null)
        {
            ropeCountText.text = $"{visibleRopeCount} / {currentRopes.Count} ropes visible";
        }

        if (habitatDropdown != null && habitatDropdown.value != selectedHabitatIndex)
        {
            habitatDropdown.SetValueWithoutNotify(selectedHabitatIndex);
        }
    }

    private readonly struct HabitatEntry
    {
        public HabitatEntry(string displayName, GameObject source)
        {
            DisplayName = displayName;
            Source = source;
        }

        public string DisplayName { get; }
        public GameObject Source { get; }
    }
}
