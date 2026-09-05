using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// Manages the entire wave system:
/// - Starts waves
/// - Spawns enemies
/// - Detects when a wave is finished
/// - Shows the card selection UI
/// - Starts the next wave after a card is chosen
/// - Gates transition waves behind player arrival in the next area (Flow A)
/// </summary>
public class WaveRoundSystem : MonoBehaviour
{
    [Header("Wave Settings")]
    [SerializeField] private WaveDefinition[] waves;

    [Header("Spawn Settings")]
    [SerializeField] private float delayBetweenSpawns = 0.2f;

    [Header("Area Transitions")]
    [Tooltip("Wave numbers (1-based) after which the player must reach the next area before the following wave spawns.")]
    [SerializeField] private int[] transitionAfterWaves;

    [Header("Wave Clear Debug")]
    [Tooltip("Toggles on (T) key to clear wave ")]
    [SerializeField] private bool clearWaveDebug = false;

    private readonly List<GameObject> activeEnemies = new List<GameObject>();

    private int currentWaveIndex = -1;

    /// <summary>
    /// Public accessor for the current wave number (1-based). Returns 0 when no wave has started.
    /// </summary>
    public int CurrentWaveNumber
    {
        get { return currentWaveIndex >= 0 ? currentWaveIndex + 1 : 0; }
    }

    // Raw zero-based index (kept for internal use)
    public int CurrentWave => currentWaveIndex;

    private bool waitingForCardSelection = false;
    private bool waveInProgress = false;
    private bool awaitingAreaArrival = false;

    [SerializeField] private CardSelectionUI cardSelectionUI;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI waveText;

    /// <summary>
    /// Called by WaveStartTrigger when the player enters the trigger.
    /// Starts the very first wave of the game.
    /// </summary>
    public void StartFirstWave()
    {
        if (waveInProgress || currentWaveIndex >= 0) return;

        currentWaveIndex = 0;
        StartWave(currentWaveIndex);
    }

    // check if wave is over and show card selection
    private void Update()
    {
        //kill all enemies in the wave to test card selection
        if (clearWaveDebug && Input.GetKeyDown(KeyCode.T))
        {
            KillAllEnemiesInWave();
            Debug.Log("Skill issue detected");
        }

        if (waveInProgress &&
            !waitingForCardSelection &&
            activeEnemies.Count == 0)
        {
            waitingForCardSelection = true;
            waveInProgress = false;

            if (currentWaveIndex == waves.Length - 1)
            {
                //Debug.Log("Last wave completed");
                UnityEngine.SceneManagement.SceneManager.LoadScene("Victory");
                return;
            }

            if (cardSelectionUI != null)
                cardSelectionUI.ShowCardSelectionFromWave();
        }
    }

    /// <summary>
    /// Starts a wave by reading its WaveDefinition and spawning all enemy groups.
    /// </summary>
    private void StartWave(int waveIndex)
    {
        //if (waveIndex < 0 || waveIndex >= waves.Length)
        //{
        //    Debug.LogError("Invalid wave index!");
        //    return;
        //}
        WaveDefinition wave = waves[waveIndex];
        activeEnemies.Clear();
        waitingForCardSelection = false;
        waveInProgress = true;

        Debug.Log($"Starting Wave {waveIndex + 1} with {wave.enemyGroups.Length} enemy groups.");

        StartCoroutine(SpawnWaveEnemies(wave));

        UpdateWaveText();
    }

    /// <summary>
    /// Spawns all enemy groups in the wave with a delay between each enemy.
    /// </summary>
    private IEnumerator SpawnWaveEnemies(WaveDefinition wave)
    {
        foreach (EnemyGroup group in wave.enemyGroups)
        {
            for (int i = 0; i < group.count; i++)
            {
                SpawnEnemy(group.enemyPrefab, wave.spawnZones);
                yield return new WaitForSeconds(delayBetweenSpawns);
            }
        }
    }

    /// <summary>
    /// Spawns a single enemy at a random spawn zone and registers its death callback.
    /// </summary>
    private void SpawnEnemy(GameObject enemyPrefab, Transform[] spawnZones)
    {
        //if (enemyPrefab == null || spawnZones == null || spawnZones.Length == 0)
        //{
        //    Debug.LogWarning("Missing prefab or spawn zones on wave.");
        //    return;
        //}

        Transform zone = spawnZones[Random.Range(0, spawnZones.Length)];
        GameObject enemy = Instantiate(enemyPrefab, zone.position, Quaternion.identity);
        activeEnemies.Add(enemy);

        Health hp = enemy.GetComponent<Health>();
        if (hp != null)
            hp.OnDestroyed += (deadEnemy) => HandleEnemyDeath(deadEnemy);
    }

    /// <summary>
    /// Removes an enemy from the active list when it dies.
    /// </summary>
    private void HandleEnemyDeath(GameObject enemy)
    {
        if (activeEnemies.Contains(enemy))
            activeEnemies.Remove(enemy);
    }

    /// <summary>
    /// Called by CardSelectionUI after the player chooses a card.
    /// Starts the next wave — OR holds it if the just-finished wave is a transition wave.
    /// </summary>
    public void StartNextWaveAfterCard()
    {
        currentWaveIndex++;

        //if (currentWaveIndex >= waves.Length)
        //{
        //    Debug.Log("All waves completed after card selection!");
        //    return;
        //}

        // At this point currentWaveIndex has been incremented, so it equals the
        // 1-based number of the wave that was JUST finished. If that wave is a
        // transition wave, hold the spawn until the player reaches the next area.
        if (transitionAfterWaves != null &&
            System.Array.IndexOf(transitionAfterWaves, currentWaveIndex) >= 0)
        {
            awaitingAreaArrival = true;
            //Debug.Log($"[WaveRoundSystem] Wave {currentWaveIndex} cleared — holding wave {currentWaveIndex + 1} until player reaches next area.");
            return;   // do NOT StartWave yet
        }

        StartWave(currentWaveIndex);
    }

    /// <summary>
    /// Called by DoorSystem when the player has passed into the next area.
    /// Releases a held transition-wave spawn.
    /// </summary>
    public void OnPlayerReachedNextArea()
    {
        if (!awaitingAreaArrival) return;

        awaitingAreaArrival = false;
        //Debug.Log($"[WaveRoundSystem] Player reached next area — spawning wave {currentWaveIndex + 1}.");
        StartWave(currentWaveIndex);
    }

    /// <summary>
    /// Debug tool: instantly kills all enemies in the current wave.
    /// </summary>
    private void KillAllEnemiesInWave()
    {
        foreach (GameObject enemy in new List<GameObject>(activeEnemies))
        {
            if (enemy != null)
            {
                Health hp = enemy.GetComponent<Health>();
                if (hp != null)
                {
                    hp.TakeDmg(999999f);
                }
                else
                {
                    Destroy(enemy);
                }
            }
        }

        activeEnemies.Clear();
    }

    public void KillAllEnemiesInWaveDebug()
    {
        KillAllEnemiesInWave();
    }

    public void SkipToWave(int waveNumber) //Skips waves for debug purposes.
    {
        Debug.Log($"Skipping to wave {waveNumber}");
        if (waveNumber < 1 || waveNumber > waves.Length)
        {
            Debug.LogError($"Invalid Wave Number: {waveNumber}");
            return;
        }
        currentWaveIndex = waveNumber - 1;

        Debug.Log($"Setting currentWaveIndex to {currentWaveIndex}");

        KillAllEnemiesInWave();

        StopAllCoroutines();

        activeEnemies.Clear();
        waitingForCardSelection = false;
        waveInProgress = false;
        awaitingAreaArrival = false;   // clear any pending hold when skipping

        StartWave(currentWaveIndex);
    }

    /// <summary>
    /// Updates the UI text to show the current wave number.
    /// </summary>
    private void UpdateWaveText()
    {
        if (waveText != null)
        {
            waveText.text = $"Wave {currentWaveIndex + 1}";
        }
    }
}