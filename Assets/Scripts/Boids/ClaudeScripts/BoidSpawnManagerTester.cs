using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class BoidSpawnManagerTester : MonoBehaviour
{
    [Header("Reference")]
    public BoidSpawnManager spawnManager;

    [Header("Test Controls")]
    [Range(0, 3)]
    public int speciesIndex = 0;

    public int spawnCount = 10;
    public int despawnCount = 5;
    public int setToCount = 20;

    [Header("Read Only — Live Counts")]
    [SerializeField] private int species0Count;
    [SerializeField] private int species1Count;
    [SerializeField] private int species2Count;
    [SerializeField] private int species3Count;

    // Called by buttons in the custom editor below
    public void TestSpawn()
    {
        spawnManager.SpawnBoids(speciesIndex, spawnCount);
        RefreshCounts();
    }

    public void TestDespawn()
    {
        spawnManager.DespawnBoids(speciesIndex, despawnCount);
        RefreshCounts();
    }

    public void TestSetCount()
    {
        spawnManager.SetBoidCount(speciesIndex, setToCount);
        RefreshCounts();
    }

    public void TestClearSpecies()
    {
        spawnManager.ClearSpecies(speciesIndex);
        RefreshCounts();
    }

    public void TestClearAll()
    {
        spawnManager.ClearAll();
        RefreshCounts();
    }

    public void RefreshCounts()
    {
        if (spawnManager == null) return;
        species0Count = spawnManager.GetCurrentCount(0);
        species1Count = spawnManager.GetCurrentCount(1);
        species2Count = spawnManager.GetCurrentCount(2);
        species3Count = spawnManager.GetCurrentCount(3);
    }
}

// ---------------------------------------------------------------
// Custom Inspector
// ---------------------------------------------------------------
#if UNITY_EDITOR
[CustomEditor(typeof(BoidSpawnManagerTester))]
public class BoidSpawnManagerTesterEditor : Editor
{
    public override void OnInspectorGUI()
    {
        BoidSpawnManagerTester tester = (BoidSpawnManagerTester)target;

        DrawDefaultInspector();

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("— Actions —", EditorStyles.boldLabel);

        // Only enable buttons in Play mode
        GUI.enabled = Application.isPlaying;

        if (GUILayout.Button($"Spawn {tester.spawnCount} boids (species {tester.speciesIndex})"))
            tester.TestSpawn();

        if (GUILayout.Button($"Despawn {tester.despawnCount} boids (species {tester.speciesIndex})"))
            tester.TestDespawn();

        if (GUILayout.Button($"Set count to {tester.setToCount} (species {tester.speciesIndex})"))
            tester.TestSetCount();

        EditorGUILayout.Space(5);

        if (GUILayout.Button($"Clear species {tester.speciesIndex}"))
            tester.TestClearSpecies();

        GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
        if (GUILayout.Button("Clear ALL species"))
            tester.TestClearAll();
        GUI.backgroundColor = Color.white;

        EditorGUILayout.Space(5);

        GUI.backgroundColor = new Color(0.6f, 0.9f, 0.6f);
        if (GUILayout.Button("Refresh Counts"))
            tester.RefreshCounts();
        GUI.backgroundColor = Color.white;

        GUI.enabled = true;

        if (!Application.isPlaying)
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.HelpBox("Enter Play Mode to use the buttons.", MessageType.Info);
        }
    }
}
#endif