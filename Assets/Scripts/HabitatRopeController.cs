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
    [SerializeField] private List<GameObject> habitatPrefabs = new();
    [SerializeField] private bool createRuntimeUi = true;
    [SerializeField] private float habitatBottomClearance = 0.08f;

    private const string ControllerObjectName = "Habitat Rope Controller";
    private const string EditorRopeMaterialAssetPath = "Assets/Materials/Rope001_1K-PNG/Rope.mat";

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
    private RectTransform runtimePanel;
    private readonly List<Button> habitatButtons = new();
    private Button addRopeButton;
    private Button resetRopeButton;
    private TMP_Text ropeCountText;

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
        int visibleHabitatCount = Mathf.Min(3, habitats.Count);
        float panelHeight = visibleHabitatCount * 38f + 44f;
        GameObject panelObject = new("Habitat Controls", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
        panelObject.transform.SetParent(MediTerraniaRuntimeUi.EnsureLeftColumn(canvas), false);
        runtimePanel = panelObject.GetComponent<RectTransform>();
        MediTerraniaRuntimeUi.AddLayoutElement(panelObject, panelHeight, preferredWidth: 300f);

        VerticalLayoutGroup layout = runtimePanel.GetComponent<VerticalLayoutGroup>();
        if (layout != null)
        {
            layout.spacing = 6f;
            layout.padding = new RectOffset(0, 0, 0, 0);
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
        }

        habitatButtons.Clear();

        for (int i = 0; i < visibleHabitatCount; i++)
        {
            int habitatIndex = i;
            Button habitatButton = MediTerraniaRuntimeUi.CreateButton(
                runtimePanel,
                $"Habitat {habitatIndex + 1}",
                $"Habitat {habitatIndex + 1}",
                () => SelectHabitat(habitatIndex));
            habitatButtons.Add(habitatButton);
        }

        addRopeButton = MediTerraniaRuntimeUi.CreateButton(runtimePanel, "Add Rope", "Add rope", AddNextRope);
    }

    private void RefreshHabitatButtons()
    {
        for (int i = 0; i < habitatButtons.Count; i++)
        {
            MediTerraniaRuntimeUi.SetButtonSelected(habitatButtons[i], i == selectedHabitatIndex);
        }
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
        RefreshHabitatButtons();

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
}
