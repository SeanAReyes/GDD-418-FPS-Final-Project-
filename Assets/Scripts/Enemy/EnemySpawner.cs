using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FPS.Enemy
{
    /// <summary>
    /// Manages enemy spawning at defined spawn points.
    /// The AI director calls into this to increase or decrease
    /// active enemy count based on player performance.
    /// </summary>
    public class EnemySpawner : MonoBehaviour
    {
        [Header("Spawning")]
        [Tooltip("Enemy prefab to spawn. Must have EnemyController on it.")]
        public GameObject enemyPrefab;

        [Tooltip("How many enemies are active at the start of the game.")]
        public int initialEnemyCount = 3;

        [Tooltip("Maximum enemies that can be alive at once.")]
        public int maxEnemyCount = 10;

        [Tooltip("Seconds between each spawn check.")]
        public float spawnInterval = 3f;

        [Header("Spawn Points")]
        [Tooltip("All valid positions enemies can spawn from.")]
        public Transform[] spawnPoints;

        // Tracks all currently living enemies
        private readonly List<EnemyController> _activeEnemies = new List<EnemyController>();

        // Current target enemy count — the AI director changes this value
        private int _targetEnemyCount;

        private bool _spawning;

        // Event the performance tracker listens to
        public event System.Action<EnemyController> OnEnemySpawned;

        private void Start()
        {
            if (spawnPoints == null || spawnPoints.Length == 0)
            {
                Debug.LogWarning("[EnemySpawner] No spawn points assigned.");
                return;
            }

            if (enemyPrefab == null)
            {
                Debug.LogError("[EnemySpawner] No enemy prefab assigned.");
                return;
            }

            _targetEnemyCount = initialEnemyCount;
            StartCoroutine(SpawnLoop());
        }

        private IEnumerator SpawnLoop()
        {
            _spawning = true;

            while (_spawning)
            {
                // Clean up any destroyed enemies from the list
                _activeEnemies.RemoveAll(e => e == null);

                // Spawn if below target count
                while (_activeEnemies.Count < _targetEnemyCount)
                    SpawnOne();

                yield return new WaitForSeconds(spawnInterval);
            }
        }

        private void SpawnOne()
        {
            if (spawnPoints.Length == 0) return;

            // Pick a random spawn point
            Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
            GameObject enemyGO = Instantiate(enemyPrefab, spawnPoint.position, spawnPoint.rotation);
            EnemyController enemy = enemyGO.GetComponent<EnemyController>();

            if (enemy == null)
            {
                Debug.LogError("[EnemySpawner] Enemy prefab is missing EnemyController component.");
                Destroy(enemyGO);
                return;
            }

            // Listen for this enemy's death to update the active list
            enemy.OnEnemyDied += HandleEnemyDied;
            _activeEnemies.Add(enemy);
            OnEnemySpawned?.Invoke(enemy);

            Debug.Log($"[EnemySpawner] Spawned {enemyGO.name} | Active: {_activeEnemies.Count}/{_targetEnemyCount}");
        }

        private void HandleEnemyDied(EnemyController enemy)
        {
            enemy.OnEnemyDied -= HandleEnemyDied;
            _activeEnemies.Remove(enemy);
            Debug.Log($"[EnemySpawner] Enemy died | Active: {_activeEnemies.Count}/{_targetEnemyCount}");
        }

        /// <summary>
        /// Called by the AI director to increase active enemy count.
        /// Clamped to maxEnemyCount.
        /// </summary>
        public void IncreaseEnemyCount(int amount)
        {
            _targetEnemyCount = Mathf.Min(_targetEnemyCount + amount, maxEnemyCount);
            Debug.Log($"[EnemySpawner] Target increased to {_targetEnemyCount}.");
        }

        /// <summary>
        /// Called by the AI director to decrease active enemy count.
        /// Clamped to 1 so there is always at least one enemy.
        /// </summary>
        public void DecreaseEnemyCount(int amount)
        {
            _targetEnemyCount = Mathf.Max(_targetEnemyCount - amount, 1);
            Debug.Log($"[EnemySpawner] Target decreased to {_targetEnemyCount}.");
        }

        /// <summary>
        /// Forces all active enemies to immediately aggro the player.
        /// Called by the AI director when the player is disengaging.
        /// </summary>
        public void ForceAggroAll()
        {
            _activeEnemies.RemoveAll(e => e == null);
            foreach (EnemyController enemy in _activeEnemies)
                enemy.ForceAggro();

            Debug.Log("[EnemySpawner] Force aggroed all active enemies.");
        }

        /// <summary>Returns the current number of living enemies.</summary>
        public int GetActiveEnemyCount() => _activeEnemies.Count;

        /// <summary>Returns the current target enemy count.</summary>
        public int GetTargetEnemyCount() => _targetEnemyCount;

        /// <summary>Returns a read-only view of active enemies for the AI director.</summary>
        public IReadOnlyList<EnemyController> GetActiveEnemies() => _activeEnemies.AsReadOnly();
    }
}
