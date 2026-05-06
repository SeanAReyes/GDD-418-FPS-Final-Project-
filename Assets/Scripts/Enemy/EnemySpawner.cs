using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FPS.Enemy
{
    /// <summary>
    /// Manages enemy spawning. Supports a melee and a ranged prefab.
    /// The AI Director controls how many of each type are active.
    /// </summary>
    public class EnemySpawner : MonoBehaviour
    {
        [Header("Prefabs")]
        [Tooltip("Melee enemy prefab.")]
        public GameObject meleeEnemyPrefab;

        [Tooltip("Ranged enemy prefab — spawned more at high performance.")]
        public GameObject rangedEnemyPrefab;

        [Header("Spawning")]
        [Tooltip("How many enemies are active at the start.")]
        public int initialEnemyCount = 3;

        [Tooltip("Maximum enemies alive at once.")]
        public int maxEnemyCount = 10;

        [Tooltip("Seconds between each spawn check.")]
        public float spawnInterval = 3f;

        [Header("Spawn Points")]
        public Transform[] spawnPoints;

        // 0 = all melee, 1 = all ranged. AI Director adjusts this.
        private float _rangedRatio = 0f;

        private readonly List<EnemyController> _activeEnemies = new List<EnemyController>();
        private int _targetEnemyCount;
        private bool _spawning;

        public event System.Action<EnemyController> OnEnemySpawned;

        private void Start()
        {
            if (spawnPoints == null || spawnPoints.Length == 0)
            {
                Debug.LogWarning("[EnemySpawner] No spawn points assigned.");
                return;
            }

            if (meleeEnemyPrefab == null)
            {
                Debug.LogError("[EnemySpawner] No melee enemy prefab assigned.");
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
                _activeEnemies.RemoveAll(e => e == null);

                while (_activeEnemies.Count < _targetEnemyCount)
                    SpawnOne();

                yield return new WaitForSeconds(spawnInterval);
            }
        }

        private void SpawnOne()
        {
            if (spawnPoints.Length == 0) return;

            // Decide type based on ratio — if ranged prefab isn't assigned, always spawn melee
            bool spawnRanged = rangedEnemyPrefab != null && Random.value < _rangedRatio;
            GameObject prefab = spawnRanged ? rangedEnemyPrefab : meleeEnemyPrefab;

            Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
            GameObject enemyGO   = Instantiate(prefab, spawnPoint.position, spawnPoint.rotation);
            EnemyController enemy = enemyGO.GetComponent<EnemyController>();

            if (enemy == null)
            {
                Debug.LogError("[EnemySpawner] Prefab is missing EnemyController.");
                Destroy(enemyGO);
                return;
            }

            enemy.OnEnemyDied += HandleEnemyDied;
            _activeEnemies.Add(enemy);
            OnEnemySpawned?.Invoke(enemy);

            Debug.Log($"[EnemySpawner] Spawned {(spawnRanged ? "ranged" : "melee")} enemy " +
                      $"| Active: {_activeEnemies.Count}/{_targetEnemyCount}");
        }

        private void HandleEnemyDied(EnemyController enemy)
        {
            enemy.OnEnemyDied -= HandleEnemyDied;
            _activeEnemies.Remove(enemy);
            Debug.Log($"[EnemySpawner] Enemy died | Active: {_activeEnemies.Count}/{_targetEnemyCount}");
        }

        /// <summary>Sets the ratio of ranged enemies spawned. 0 = all melee, 1 = all ranged.</summary>
        public void SetRangedRatio(float ratio)
        {
            _rangedRatio = Mathf.Clamp01(ratio);
            Debug.Log($"[EnemySpawner] Ranged ratio set to {_rangedRatio:F2}");
        }

        /// <summary>Sets bullet speed on all currently active ranged enemies.</summary>
        public void SetActiveBulletSpeed(float speed)
        {
            _activeEnemies.RemoveAll(e => e == null);
            foreach (EnemyController enemy in _activeEnemies)
                enemy.SetBulletSpeed(speed);

            Debug.Log($"[EnemySpawner] Bullet speed set to {speed} on all active enemies.");
        }

        public void IncreaseEnemyCount(int amount)
        {
            _targetEnemyCount = Mathf.Min(_targetEnemyCount + amount, maxEnemyCount);
            Debug.Log($"[EnemySpawner] Target increased to {_targetEnemyCount}.");
        }

        public void DecreaseEnemyCount(int amount)
        {
            _targetEnemyCount = Mathf.Max(_targetEnemyCount - amount, 1);
            Debug.Log($"[EnemySpawner] Target decreased to {_targetEnemyCount}.");
        }

        public void ForceAggroAll()
        {
            _activeEnemies.RemoveAll(e => e == null);
            foreach (EnemyController enemy in _activeEnemies)
                enemy.ForceAggro();

            Debug.Log("[EnemySpawner] Force aggroed all active enemies.");
        }

        public int GetActiveEnemyCount() => _activeEnemies.Count;
        public int GetTargetEnemyCount() => _targetEnemyCount;
        public IReadOnlyList<EnemyController> GetActiveEnemies() => _activeEnemies.AsReadOnly();
    }
}
