using System.Collections.Generic;
using UnityEngine;

public class WaveRoundSystem : MonoBehaviour
{
    [Header("Prefab Settings")]
    [SerializeField] private GameObject enemyPrefab;
    
    [Header("Spawn Settings")]
    [SerializeField] private int prefabsPerWave = 5;
    [SerializeField] private float spawnRadius = 10f;
    [SerializeField] private Vector3 spawnCenter = Vector3.zero;
    
    [Header("Round Settings")]
    [SerializeField] private int currentRound = 1;
    [SerializeField] private int maxRounds = 20;
    
    [Header("Input")]
    [SerializeField] private KeyCode skipRoundKey = KeyCode.Space;
    
    private List<GameObject> activeEnemies = new List<GameObject>();
    private int enemiesDestroyedThisRound = 0;

    void Start()
    {
        SpawnWave();
    }

    void Update()
    {
        // Check for skip round input
        if (Input.GetKeyDown(skipRoundKey))
        {
            Debug.Log("Space key pressed!");
            SkipRound();
        }
        
        // Check if all enemies are destroyed
        if (activeEnemies.Count == 0 && enemiesDestroyedThisRound > 0)
        {
            NextRound();
        }
    }

    void SpawnWave()
    {
        if (currentRound > maxRounds)
        {
            Debug.Log("Max rounds reached!");
            return;
        }
        
        Debug.Log($"Starting Round {currentRound}");
        enemiesDestroyedThisRound = 0;
        
        for (int i = 0; i < prefabsPerWave; i++)
        {
            // Spawn at random position within radius (2D)
            Vector2 randomOffset = Random.insideUnitCircle * spawnRadius;
            Vector3 randomPosition = new Vector3(
                spawnCenter.x + randomOffset.x,
                spawnCenter.y + randomOffset.y,
                spawnCenter.z
            );
            
            GameObject enemy = Instantiate(enemyPrefab, randomPosition, Quaternion.identity);
            activeEnemies.Add(enemy);
            
            // Subscribe to destruction event if the prefab has a health/destroyable component
            EnemyHealth enemyHealth = enemy.GetComponent<EnemyHealth>();
            if (enemyHealth != null)
            {
                enemyHealth.OnDestroyed += HandleEnemyDestroyed;
            }
        }
    }

    void HandleEnemyDestroyed(GameObject enemy)
    {
        if (activeEnemies.Contains(enemy))
        {
            activeEnemies.Remove(enemy);
            enemiesDestroyedThisRound++;
            Debug.Log($"Enemy destroyed. Remaining: {activeEnemies.Count}");
        }
    }

    void NextRound()
    {
        if (currentRound < maxRounds)
        {
            currentRound++;
            SpawnWave();
        }
        else
        {
            Debug.Log("All rounds complete!");
        }
    }

    void SkipRound()
    {
        Debug.Log($"Skipping Round {currentRound} - Active enemies: {activeEnemies.Count}");
        
        // Destroy all active enemies
        int destroyedCount = 0;
        foreach (GameObject enemy in activeEnemies)
        {
            if (enemy != null)
            {
                Destroy(enemy);
                destroyedCount++;
            }
        }
        
        Debug.Log($"Destroyed {destroyedCount} enemies");
        activeEnemies.Clear();
        
        // Increment round
        if (currentRound < maxRounds)
        {
            currentRound++;
            SpawnWave();
        }
        else
        {
            Debug.Log("Already at max rounds!");
        }
    }

    // Alternative: Manual tracking without health component
    public void RegisterEnemyDestruction(GameObject enemy)
    {
        HandleEnemyDestroyed(enemy);
    }

    // Public getters
    public int GetCurrentRound() => currentRound;
    public int GetRemainingEnemies() => activeEnemies.Count;
}