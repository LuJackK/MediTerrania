using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

#if UNITY_EDITOR
using UnityEditor;
#endif

public sealed class HabitatRopeController : MonoBehaviour
{
    [SerializeField] private GameObject initialHabitat;
    [SerializeField] private Material ropeMaterial;
    [SerializeField] private List<Material> habitatSurfaceMaterials = new();
    [SerializeField] private List<GameObject> habitatPrefabs = new();
    [SerializeField] private bool createRuntimeUi = true;
    [SerializeField] private float habitatBottomClearance = 0.08f;

    private const string ControllerObjectName = "Habitat Rope Controller";
    private const string EditorRopeMaterialAssetPath = "Assets/Materials/Rope001_1K-PNG/Rope.mat";

    private static readonly HabitatMaterialDefinition[] HabitatMaterialDefinitions =
    {
        new("Sandstone", "Assets/Materials/textures 3/Sandstone.mat", new Color(0.72f, 0.55f, 0.35f, 1f)),
        new("Marble", "Assets/Materials/textures/Marble.mat", new Color(0.86f, 0.88f, 0.84f, 1f)),
        new("Metal", "Assets/Materials/textures 2/Metal.mat", new Color(0.46f, 0.56f, 0.62f, 1f))
    };

    private static readonly string[] EditorHabitatAssetPaths =
    {
        "Assets/Models/habitat1.fbx",
        "Assets/Models/habitat2.fbx",
        "Assets/Models/habitat3.fbx"
    };

    private readonly List<HabitatEntry> habitats = new();
    private readonly List<HabitatMaterialOption> habitatMaterialOptions = new();
    private readonly List<GameObject> currentRopes = new();

    private Transform spawnParent;
    private Vector3 spawnPosition;
    private Quaternion spawnRotation;
    private Vector3 spawnScale = Vector3.one;
    private GameObject currentHabitat;
    private int selectedHabitatIndex;
    private int visibleRopeCount;
    private RectTransform runtimePanel;
    private Button previousHabitatButton;
    private Button nextHabitatButton;
    private TMP_Text habitatNameText;
    private Button habitatMaterialButton;
    private Image habitatMaterialPreview;
    private TMP_Text habitatMaterialNameText;
    private Button addRopeButton;
    private Button resetRopeButton;
    private TMP_Text ropeCountText;
    private int selectedHabitatMaterialIndex;

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
        if (scene.name != "Test1")
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
        DisableUnsupportedEventSystemModules();
        CaptureSpawnTransform();
        LoadDefaultRopeMaterial();
        LoadDefaultHabitatMaterials();
        BuildHabitatMaterialOptions();
        BuildHabitatList();

        if (habitats.Count == 0)
        {
            Debug.LogWarning("No habitat models were found for the rope controller.");
            enabled = false;
            return;
        }

        SelectHabitat(0);
        CreateRuntimeUi();
        UpdateUiState();
    }

    private void OnDestroy()
    {
        if (runtimePanel != null)
        {
            Destroy(runtimePanel.gameObject);
            runtimePanel = null;
        }
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
        PlaceHabitatOnSeafloor(currentHabitat);

        CollectRopes(currentHabitat.transform, currentRopes);
        ApplySelectedHabitatMaterial();
        ApplyRopeMaterial(currentRopes);
        ResetRopes();
    }

    private void PlaceHabitatOnSeafloor(GameObject habitat)
    {
        if (habitat == null)
        {
            return;
        }

        if (!TryGetRendererBounds(habitat, out Bounds bounds))
        {
            return;
        }

        float seafloorY = GetSeafloorY();
        float liftToBottom = seafloorY + habitatBottomClearance - bounds.min.y;
        habitat.transform.position += Vector3.up * liftToBottom;
    }

    private static bool TryGetRendererBounds(GameObject root, out Bounds bounds)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        bounds = default;
        bool hasBounds = false;

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null || IsRopeName(renderer.name))
            {
                continue;
            }

            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
                continue;
            }

            bounds.Encapsulate(renderer.bounds);
        }

        return hasBounds;
    }

    private static float GetSeafloorY()
    {
        GameObject seafloor = GameObject.Find("SeaFloor");
        return seafloor != null ? seafloor.transform.position.y : 0f;
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

    private void LoadDefaultRopeMaterial()
    {
        if (ropeMaterial != null)
        {
            return;
        }

#if UNITY_EDITOR
        ropeMaterial = AssetDatabase.LoadAssetAtPath<Material>(EditorRopeMaterialAssetPath);
#endif
    }

    private void LoadDefaultHabitatMaterials()
    {
        if (habitatSurfaceMaterials.Count > 0)
        {
            return;
        }

#if UNITY_EDITOR
        for (int i = 0; i < HabitatMaterialDefinitions.Length; i++)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(HabitatMaterialDefinitions[i].AssetPath);
            if (material != null)
            {
                habitatSurfaceMaterials.Add(material);
            }
        }
#endif
    }

    private void BuildHabitatMaterialOptions()
    {
        habitatMaterialOptions.Clear();

        for (int i = 0; i < HabitatMaterialDefinitions.Length; i++)
        {
            Material material = FindConfiguredHabitatMaterial(HabitatMaterialDefinitions[i].DisplayName);
            if (material == null)
            {
                continue;
            }

            habitatMaterialOptions.Add(new HabitatMaterialOption(
                HabitatMaterialDefinitions[i].DisplayName,
                material,
                HabitatMaterialDefinitions[i].PreviewColor));
        }

        if (selectedHabitatMaterialIndex >= habitatMaterialOptions.Count)
        {
            selectedHabitatMaterialIndex = 0;
        }
    }

    private Material FindConfiguredHabitatMaterial(string displayName)
    {
        for (int i = 0; i < habitatSurfaceMaterials.Count; i++)
        {
            Material material = habitatSurfaceMaterials[i];
            if (material != null && string.Equals(material.name, displayName, System.StringComparison.OrdinalIgnoreCase))
            {
                return material;
            }
        }

        return null;
    }

    private void AddHabitatPrefab(GameObject habitat)
    {
        if (habitat == null || !IsHabitatName(habitat.name) || habitat.GetComponent<HabitatRopeController>() != null)
        {
            return;
        }

        string habitatKey = GetHabitatKey(habitat.name);
        for (int i = 0; i < habitats.Count; i++)
        {
            if (GetHabitatKey(habitats[i].Source.name) == habitatKey)
            {
                return;
            }
        }

        string displayName = $"Habitat {habitats.Count + 1}";
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
        if (string.IsNullOrWhiteSpace(objectName))
        {
            return false;
        }

        string key = GetHabitatKey(objectName);
        return key == "habitat1" || key == "habitat2" || key == "habitat3";
    }

    private static string GetHabitatKey(string objectName)
    {
        return objectName
            .Replace(" ", string.Empty)
            .Replace("_", string.Empty)
            .Replace("-", string.Empty)
            .Replace("(Clone)", string.Empty)
            .ToLowerInvariant();
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

    private void ApplyRopeMaterial(List<GameObject> ropes)
    {
        if (ropeMaterial == null)
        {
            Debug.LogWarning($"Rope material was not found at {EditorRopeMaterialAssetPath}.");
            return;
        }

        for (int i = 0; i < ropes.Count; i++)
        {
            Renderer[] renderers = ropes[i].GetComponentsInChildren<Renderer>(true);
            for (int j = 0; j < renderers.Length; j++)
            {
                Material[] materials = renderers[j].sharedMaterials;
                if (materials == null || materials.Length == 0)
                {
                    renderers[j].sharedMaterial = ropeMaterial;
                    continue;
                }

                for (int k = 0; k < materials.Length; k++)
                {
                    materials[k] = ropeMaterial;
                }

                renderers[j].sharedMaterials = materials;
            }
        }
    }

    private void ApplySelectedHabitatMaterial()
    {
        if (currentHabitat == null || habitatMaterialOptions.Count == 0)
        {
            return;
        }

        if (selectedHabitatMaterialIndex < 0 || selectedHabitatMaterialIndex >= habitatMaterialOptions.Count)
        {
            selectedHabitatMaterialIndex = 0;
        }

        Material selectedMaterial = habitatMaterialOptions[selectedHabitatMaterialIndex].Material;
        Renderer[] renderers = currentHabitat.GetComponentsInChildren<Renderer>(true);

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null || IsInsideRope(renderer.transform, currentHabitat.transform))
            {
                continue;
            }

            Material[] materials = renderer.sharedMaterials;
            if (materials == null || materials.Length == 0)
            {
                renderer.sharedMaterial = selectedMaterial;
                continue;
            }

            for (int j = 0; j < materials.Length; j++)
            {
                materials[j] = selectedMaterial;
            }

            renderer.sharedMaterials = materials;
        }
    }

    private static bool IsInsideRope(Transform candidate, Transform root)
    {
        Transform current = candidate;
        while (current != null && current != root)
        {
            if (IsRopeName(current.name))
            {
                return true;
            }

            current = current.parent;
        }

        return false;
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

    private void CreateRuntimeUi()
    {
        if (!createRuntimeUi || runtimePanel != null || habitats.Count == 0)
        {
            return;
        }

        Canvas canvas = MediTerraniaRuntimeUi.EnsureCanvas();
        runtimePanel = MediTerraniaRuntimeUi.CreatePanel(
            MediTerraniaRuntimeUi.EnsureLeftColumn(canvas),
            "Habitat Controls",
            new Vector2(286f, 248f));

        VerticalLayoutGroup layout = runtimePanel.GetComponent<VerticalLayoutGroup>();
        if (layout != null)
        {
            layout.spacing = 7f;
            layout.padding = new RectOffset(12, 12, 10, 10);
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
        }

        MediTerraniaRuntimeUi.CreateTitle(runtimePanel, "Habitat");
        CreateHabitatSwitcher(runtimePanel);
        CreateHabitatMaterialButton(runtimePanel);
        addRopeButton = CreateAddRopeButton(runtimePanel);
    }

    private void CreateHabitatSwitcher(RectTransform parent)
    {
        GameObject switcherObject = new("Habitat Switcher", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        switcherObject.transform.SetParent(parent, false);
        MediTerraniaRuntimeUi.AddLayoutElement(switcherObject, 40f);

        Image switcherImage = switcherObject.GetComponent<Image>();
        switcherImage.sprite = MediTerraniaRuntimeUi.RoundedSprite;
        switcherImage.type = Image.Type.Sliced;
        switcherImage.color = new Color(0.1f, 0.36f, 0.48f, 0.36f);

        HorizontalLayoutGroup switcherLayout = switcherObject.GetComponent<HorizontalLayoutGroup>();
        switcherLayout.padding = new RectOffset(5, 5, 4, 4);
        switcherLayout.spacing = 6f;
        switcherLayout.childAlignment = TextAnchor.MiddleCenter;
        switcherLayout.childControlWidth = true;
        switcherLayout.childControlHeight = true;
        switcherLayout.childForceExpandWidth = false;
        switcherLayout.childForceExpandHeight = true;

        previousHabitatButton = MediTerraniaRuntimeUi.CreateButton(switcherObject.transform, "Previous Habitat", "<", SelectPreviousHabitat);
        ConfigureSwitcherArrow(previousHabitatButton);

        GameObject labelObject = new("Selected Habitat Label", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
        labelObject.transform.SetParent(switcherObject.transform, false);
        habitatNameText = labelObject.GetComponent<TMP_Text>();
        habitatNameText.text = habitats[selectedHabitatIndex].DisplayName;
        habitatNameText.fontSize = 14f;
        habitatNameText.fontStyle = FontStyles.Bold;
        habitatNameText.alignment = TextAlignmentOptions.Center;
        habitatNameText.color = MediTerraniaRuntimeUi.TextColor;
        habitatNameText.textWrappingMode = TextWrappingModes.NoWrap;
        habitatNameText.overflowMode = TextOverflowModes.Ellipsis;
        habitatNameText.raycastTarget = false;

        LayoutElement labelLayout = labelObject.GetComponent<LayoutElement>();
        labelLayout.preferredHeight = 34f;
        labelLayout.flexibleWidth = 1f;

        nextHabitatButton = MediTerraniaRuntimeUi.CreateButton(switcherObject.transform, "Next Habitat", ">", SelectNextHabitat);
        ConfigureSwitcherArrow(nextHabitatButton);
    }

    private void CreateHabitatMaterialButton(RectTransform parent)
    {
        if (habitatMaterialOptions.Count == 0)
        {
            return;
        }

        GameObject buttonObject = new("Habitat Material", typeof(RectTransform), typeof(Image), typeof(Button), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        buttonObject.transform.SetParent(parent, false);
        MediTerraniaRuntimeUi.AddLayoutElement(buttonObject, 46f);

        Image buttonImage = buttonObject.GetComponent<Image>();
        buttonImage.sprite = MediTerraniaRuntimeUi.RoundedSprite;
        buttonImage.type = Image.Type.Sliced;
        buttonImage.color = MediTerraniaRuntimeUi.ButtonColor;

        HorizontalLayoutGroup layout = buttonObject.GetComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(8, 10, 6, 6);
        layout.spacing = 9f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        GameObject previewObject = new("Material Preview", typeof(RectTransform), typeof(Image), typeof(LayoutElement), typeof(Outline));
        previewObject.transform.SetParent(buttonObject.transform, false);
        habitatMaterialPreview = previewObject.GetComponent<Image>();
        habitatMaterialPreview.sprite = MediTerraniaRuntimeUi.RoundedSprite;
        habitatMaterialPreview.type = Image.Type.Sliced;
        habitatMaterialPreview.raycastTarget = false;

        Outline previewOutline = previewObject.GetComponent<Outline>();
        previewOutline.effectColor = new Color(1f, 1f, 1f, 0.3f);
        previewOutline.effectDistance = new Vector2(1f, -1f);

        LayoutElement previewLayout = previewObject.GetComponent<LayoutElement>();
        previewLayout.preferredWidth = 30f;
        previewLayout.preferredHeight = 30f;
        previewLayout.minWidth = 30f;
        previewLayout.minHeight = 30f;

        GameObject labelObject = new("Material Name", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
        labelObject.transform.SetParent(buttonObject.transform, false);
        habitatMaterialNameText = labelObject.GetComponent<TMP_Text>();
        habitatMaterialNameText.fontSize = 14f;
        habitatMaterialNameText.fontStyle = FontStyles.Bold;
        habitatMaterialNameText.alignment = TextAlignmentOptions.MidlineLeft;
        habitatMaterialNameText.color = MediTerraniaRuntimeUi.TextColor;
        habitatMaterialNameText.textWrappingMode = TextWrappingModes.NoWrap;
        habitatMaterialNameText.overflowMode = TextOverflowModes.Ellipsis;
        habitatMaterialNameText.raycastTarget = false;

        LayoutElement labelLayout = labelObject.GetComponent<LayoutElement>();
        labelLayout.preferredHeight = 30f;
        labelLayout.flexibleWidth = 1f;

        habitatMaterialButton = buttonObject.GetComponent<Button>();
        habitatMaterialButton.targetGraphic = buttonImage;
        habitatMaterialButton.colors = CreateRopeButtonColors(MediTerraniaRuntimeUi.ButtonColor);
        habitatMaterialButton.onClick.AddListener(SelectNextHabitatMaterial);
    }

    private static void ConfigureSwitcherArrow(Button button)
    {
        if (button == null)
        {
            return;
        }

        LayoutElement layoutElement = button.GetComponent<LayoutElement>();
        if (layoutElement != null)
        {
            layoutElement.preferredWidth = 34f;
            layoutElement.minWidth = 34f;
            layoutElement.preferredHeight = 32f;
            layoutElement.minHeight = 32f;
            layoutElement.flexibleWidth = 0f;
        }
    }

    private Button CreateAddRopeButton(RectTransform parent)
    {
        GameObject controlObject = new("Add Rope Control", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
        controlObject.transform.SetParent(parent, false);
        MediTerraniaRuntimeUi.AddLayoutElement(controlObject, 82f);

        VerticalLayoutGroup layout = controlObject.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(0, 0, 8, 0);
        layout.spacing = 2f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        GameObject squareObject = new("Add Rope", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        squareObject.transform.SetParent(controlObject.transform, false);

        Image squareImage = squareObject.GetComponent<Image>();
        squareImage.sprite = MediTerraniaRuntimeUi.RoundedSprite;
        squareImage.type = Image.Type.Sliced;
        squareImage.color = MediTerraniaRuntimeUi.ButtonColor;
        squareImage.raycastTarget = true;

        LayoutElement squareLayout = squareObject.GetComponent<LayoutElement>();
        squareLayout.preferredWidth = 52f;
        squareLayout.preferredHeight = 52f;
        squareLayout.minWidth = 52f;
        squareLayout.minHeight = 52f;

        GameObject labelObject = new("Add Rope Label", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
        labelObject.transform.SetParent(controlObject.transform, false);

        TMP_Text label = labelObject.GetComponent<TMP_Text>();
        label.text = "Add rope";
        label.fontSize = 12f;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.color = MediTerraniaRuntimeUi.TextColor;
        label.raycastTarget = false;

        LayoutElement labelLayout = labelObject.GetComponent<LayoutElement>();
        labelLayout.preferredWidth = 120f;
        labelLayout.preferredHeight = 20f;

        Button button = squareObject.GetComponent<Button>();
        button.targetGraphic = squareImage;
        button.colors = CreateRopeButtonColors(MediTerraniaRuntimeUi.ButtonColor);
        button.onClick.AddListener(AddNextRope);
        return button;
    }

    private static ColorBlock CreateRopeButtonColors(Color baseColor)
    {
        return new ColorBlock
        {
            normalColor = baseColor,
            highlightedColor = MediTerraniaRuntimeUi.ButtonHoverColor,
            pressedColor = MediTerraniaRuntimeUi.AccentColor * 0.85f,
            selectedColor = baseColor,
            disabledColor = new Color(0.05f, 0.12f, 0.15f, 0.45f),
            colorMultiplier = 1f,
            fadeDuration = 0.08f
        };
    }

    private void SelectPreviousHabitat()
    {
        int nextIndex = selectedHabitatIndex - 1;
        if (nextIndex < 0)
        {
            nextIndex = habitats.Count - 1;
        }

        SelectHabitat(nextIndex);
    }

    private void SelectNextHabitat()
    {
        int nextIndex = selectedHabitatIndex + 1;
        if (nextIndex >= habitats.Count)
        {
            nextIndex = 0;
        }

        SelectHabitat(nextIndex);
    }

    private void SelectNextHabitatMaterial()
    {
        if (habitatMaterialOptions.Count == 0)
        {
            return;
        }

        selectedHabitatMaterialIndex++;
        if (selectedHabitatMaterialIndex >= habitatMaterialOptions.Count)
        {
            selectedHabitatMaterialIndex = 0;
        }

        ApplySelectedHabitatMaterial();
        UpdateUiState();
    }

    private static void DisableUnsupportedEventSystemModules()
    {
#if ENABLE_INPUT_SYSTEM
        StandaloneInputModule[] standaloneModules =
            FindObjectsByType<StandaloneInputModule>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        for (int i = 0; i < standaloneModules.Length; i++)
        {
            standaloneModules[i].enabled = false;
        }

        EventSystem[] eventSystems = FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < eventSystems.Length; i++)
        {
            if (eventSystems[i].GetComponent<InputSystemUIInputModule>() == null)
            {
                eventSystems[i].gameObject.AddComponent<InputSystemUIInputModule>();
            }
        }
#endif
    }

    private void UpdateUiState()
    {
        if (habitatNameText != null && selectedHabitatIndex >= 0 && selectedHabitatIndex < habitats.Count)
        {
            habitatNameText.text = habitats[selectedHabitatIndex].DisplayName;
        }

        bool hasMultipleHabitats = habitats.Count > 1;
        if (previousHabitatButton != null)
        {
            previousHabitatButton.interactable = hasMultipleHabitats;
        }

        if (nextHabitatButton != null)
        {
            nextHabitatButton.interactable = hasMultipleHabitats;
        }

        if (habitatMaterialButton != null)
        {
            habitatMaterialButton.interactable = habitatMaterialOptions.Count > 1;
        }

        if (habitatMaterialNameText != null && habitatMaterialOptions.Count > 0)
        {
            HabitatMaterialOption option = habitatMaterialOptions[selectedHabitatMaterialIndex];
            habitatMaterialNameText.text = option.DisplayName;
        }

        if (habitatMaterialPreview != null && habitatMaterialOptions.Count > 0)
        {
            habitatMaterialPreview.color = habitatMaterialOptions[selectedHabitatMaterialIndex].PreviewColor;
        }

        if (addRopeButton != null)
        {
            addRopeButton.interactable = visibleRopeCount < currentRopes.Count;
        }

        if (resetRopeButton != null)
        {
            resetRopeButton.interactable = visibleRopeCount > 0;
        }

        if (ropeCountText != null)
        {
            ropeCountText.text = $"{visibleRopeCount} / {currentRopes.Count} ropes visible";
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

    private readonly struct HabitatMaterialDefinition
    {
        public HabitatMaterialDefinition(string displayName, string assetPath, Color previewColor)
        {
            DisplayName = displayName;
            AssetPath = assetPath;
            PreviewColor = previewColor;
        }

        public string DisplayName { get; }
        public string AssetPath { get; }
        public Color PreviewColor { get; }
    }

    private readonly struct HabitatMaterialOption
    {
        public HabitatMaterialOption(string displayName, Material material, Color previewColor)
        {
            DisplayName = displayName;
            Material = material;
            PreviewColor = previewColor;
        }

        public string DisplayName { get; }
        public Material Material { get; }
        public Color PreviewColor { get; }
    }
}
