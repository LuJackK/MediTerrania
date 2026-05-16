using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoidSpawnManager : MonoBehaviour
{
    [System.Serializable]
    public class BoidSpecies
    {
        public string speciesName;
        public Boid prefab;
        
        public int maxBoids = 30;

        [HideInInspector] public List<Boid> activeBoids = new List<Boid>();
    }

    [Header("Shared Settings (applied to all species)")]
    public BoidSettings settings;

    [Header("Species (max 4)")]
    public BoidSpecies[] species = new BoidSpecies[4];

    [Header("Spawn Settings")]
    public float spawnRadius = 10f;

    // ---------------------------------------------------------------
    // Public API
    // ---------------------------------------------------------------

    public void SetBoidCount(int speciesIndex, int targetCount)
    {
        if (!IsValidIndex(speciesIndex)) return;

        BoidSpecies s = species[speciesIndex];
        targetCount = Mathf.Clamp(targetCount, 0, s.maxBoids);

        int current = s.activeBoids.Count;

        if (targetCount > current)
            SpawnBoids(speciesIndex, targetCount - current);
        else if (targetCount < current)
            DespawnBoids(speciesIndex, current - targetCount);
    }

    public void SpawnBoids(int speciesIndex, int count)
    {
        if (!IsValidIndex(speciesIndex)) return;

        BoidSpecies s = species[speciesIndex];
        int canSpawn = s.maxBoids - s.activeBoids.Count;
        int toSpawn  = Mathf.Min(count, canSpawn);

        if (toSpawn <= 0)
        {
            Debug.Log($"[BoidSpawnManager] {s.speciesName} is already at max ({s.maxBoids}).");
            return;
        }

        for (int i = 0; i < toSpawn; i++)
        {
            
            
            // Vector3 pos = transform.position + Random.insideUnitSphere * spawnRadius;
            // Boid boid = Instantiate (s.prefab);
            // boid.transform.position = pos;
            // boid.transform.forward = Random.insideUnitSphere;
            //
            // boid.SetColour (Color.white);
            //
            
            
            Vector3 pos = transform.position + Random.insideUnitSphere * spawnRadius;
            Boid boid = Instantiate (s.prefab);
            boid.transform.position = pos;
            boid.transform.forward = Random.insideUnitSphere;

            boid.SetColour (Color.white);
            
            //ADDDEDDD THIS 
            boid.Initialize(settings, null); // ← add this line
            s.activeBoids.Add(boid);         // ← and this was missing too!
            //ADDDEDDD THIS 
            
        }
        Debug.Log($"[BoidSpawnManager] Spawned {toSpawn} {s.speciesName}. " +
                  $"Total: {s.activeBoids.Count}/{s.maxBoids}");
    }

    public void DespawnBoids(int speciesIndex, int count)
    {
        if (!IsValidIndex(speciesIndex)) return;

        BoidSpecies s = species[speciesIndex];
        int toRemove  = Mathf.Min(count, s.activeBoids.Count);

        for (int i = 0; i < toRemove; i++)
        {
            int last = s.activeBoids.Count - 1;
            Destroy(s.activeBoids[last].gameObject);
            s.activeBoids.RemoveAt(last);
        }

        Debug.Log($"[BoidSpawnManager] Removed {toRemove} {s.speciesName}. " +
                  $"Total: {s.activeBoids.Count}/{s.maxBoids}");
    }

    public void ClearSpecies(int speciesIndex)
    {
        if (!IsValidIndex(speciesIndex)) return;
        DespawnBoids(speciesIndex, species[speciesIndex].activeBoids.Count);
    }

    public void ClearAll()
    {
        for (int i = 0; i < species.Length; i++)
            ClearSpecies(i);
    }

    public int GetCurrentCount(int speciesIndex)
    {
        if (!IsValidIndex(speciesIndex)) return 0;
        return species[speciesIndex].activeBoids.Count;
    }

    // ---------------------------------------------------------------
    // Internal helpers
    // ---------------------------------------------------------------

    bool IsValidIndex(int index)
    {
        if (index < 0 || index >= species.Length)
        {
            Debug.LogWarning($"[BoidSpawnManager] Species index {index} is out of range.");
            return false;
        }
        if (species[index] == null)
        {
            Debug.LogWarning($"[BoidSpawnManager] Species at index {index} is not configured.");
            return false;
        }
        return true;
    }
    
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 1f, 0f, 0.2f);
        Gizmos.DrawSphere(transform.position, spawnRadius);
        Gizmos.color = new Color(1f, 1f, 0f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, spawnRadius);
    }
}