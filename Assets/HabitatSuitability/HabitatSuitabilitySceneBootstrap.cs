using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class HabitatSuitabilitySceneBootstrap : MonoBehaviour
{
    public const string SceneName = "HabitatSuitabilityTest";

    private const string BootstrapObjectName = "Habitat Suitability Scene Bootstrap";
    private HabitatSuitabilityTesterUi testerUi;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneLoadedCallback()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateForActiveScene()
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

        if (FindObjectsByType<HabitatSuitabilitySceneBootstrap>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length > 0)
        {
            return;
        }

        new GameObject(BootstrapObjectName).AddComponent<HabitatSuitabilitySceneBootstrap>();
    }

    private void Awake()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            mainCamera.clearFlags = CameraClearFlags.SolidColor;
            mainCamera.backgroundColor = new Color(0.02f, 0.1f, 0.13f, 1f);
        }

        Canvas canvas = MediTerraniaRuntimeUi.EnsureCanvas();
        testerUi = HabitatSuitabilityTesterUi.CreateFullScreen(canvas);
    }

    private void OnDestroy()
    {
        if (testerUi != null)
        {
            Destroy(testerUi.gameObject);
        }
    }
}
