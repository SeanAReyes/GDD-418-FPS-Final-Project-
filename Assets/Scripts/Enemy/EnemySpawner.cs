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

        [Header("Spawn Points Limits")]
        [Tooltip("Maximum enemies allowed per spawn point at once.")]
        public int maxPerSpawnpoint = 2;

        private readonly Dictionary<Transform, int> _spawnPointOccpancy = new Dictionary<Transform, int>();
        private readonly Dictionary<EnemyController, Transform> _enemyOriginPoint = new Dictionary<EnemyController, Transform>();

        // 0 = all melee, 1 = all ranged. AI Director adjusts this.
        private float _rangedRatio = 0f;

        private readonly List<EnemyController> _activeEnemies = new List<EnemyController>();
        private int _targetEnemyCount;
        private bool _spawning;
        private bool _paused;

        public event System.Action<EnemyController> OnEnemySpawned;

        private void Start()
        {
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
                if (!_paused && spawnPoints != null && spawnPoints.Length > 0)
                {
                    _activeEnemies.RemoveAll(e => e == null);

                    int attempts = 0;
                    int maxAttempts = spawnPoints.Length;

                    while (_activeEnemies.Count < _targetEnemyCount && attempts < maxAttempts)
                    {
                        int countBefore = _activeEnemies.Count;
                        SpawnOne();

                        if(_activeEnemies.Count == countBefore) break;
                        attempts++;
                    }
                }

                yield return new WaitForSeconds(spawnInterval);
            }
        }

        private void SpawnOne()
        {
            List<Transform> available = new List<Transform>();
            foreach (Transform point in spawnPoints)
            {
                _spawnPointOccpancy.TryGetValue(point, out int count);
                if (count < maxPerSpawnpoint)
                    available.Add(point);
            }

            if (available.Count == 0)
            {
                Debug.LogWarning("[EnemySpawner] No spawn points available (all at max occupancy).");
                return;
            }

            bool spawnRanged = rangedEnemyPrefab != null && Random.value < _rangedRatio;
            GameObject prefab = spawnRanged ? rangedEnemyPrefab : meleeEnemyPrefab;

            Transform spawnPoint = available[Random.Range(0, available.Count)];
            GameObject enemyGO = Instantiate(prefab, spawnPoint.position, spawnPoint.rotation);
            EnemyController enemy = enemyGO.GetComponent<EnemyController>();

            if (enemy == null)
            {
                Debug.LogError("[EnemySpawner] Spawned prefab missing EnemyController component.");
                Destroy(enemyGO);
                return;
            }

            _spawnPointOccpancy.TryGetValue(spawnPoint, out int current);
            _spawnPointOccpancy[spawnPoint] = current + 1;
            _enemyOriginPoint[enemy] = spawnPoint;

            enemy.OnEnemyDied += HandleEnemyDied;
            _activeEnemies.Add(enemy);
            OnEnemySpawned?.Invoke(enemy);

            Debug.Log($"[EnemySpawner] Spawned {(spawnRanged ? "ranged" : "melee")} enemy at {spawnPoint.name} " + 
                        $"({_spawnPointOccpancy[spawnPoint]}/{maxPerSpawnpoint}) | Active: {_activeEnemies.Count}/{_targetEnemyCount}");

        }

        private void HandleEnemyDied(EnemyController enemy)
        {
            enemy.OnEnemyDied -= HandleEnemyDied;
            _activeEnemies.Remove(enemy);

            if (_enemyOriginPoint.TryGetValue(enemy, out Transform origin))
            {
                if (_spawnPointOccpancy.ContainsKey(origin))
                    _spawnPointOccpancy[origin] = Mathf.Max(0, _spawnPointOccpancy[origin] - 1);
                    _enemyOriginPoint.Remove(enemy);
            }
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

        public void SetActiveAggroRange(float range)
        {
            _activeEnemies.RemoveAll(e => e == null);
            foreach (EnemyController enemy in _activeEnemies)
                enemy.SetAggroRange(range);

            Debug.Log($"[EnemySpawner] Aggro range set to {range} on all active enemies.");
        }

        public void SetActiveAttackRange(float range)
        {
            _activeEnemies.RemoveAll(e => e == null);
            foreach (EnemyController enemy in _activeEnemies)
                enemy.SetAttackRange(range);

            Debug.Log($"[EnemySpawner] Attack range set to {range} on all active enemies.");
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

        public void SetSpawnPoints(Transform[] newSpawnPoints)
        {
            spawnPoints = newSpawnPoints;
            _spawnPointOccpancy.Clear();
            Debug.Log($"[EnemySpawner] Spawn points updated. Total: {spawnPoints.Length} points active.");
        }
        public void ClearAllEnemies()
        {
            _activeEnemies.RemoveAll(e => e == null);

            foreach (EnemyController enemy in _activeEnemies)
            {
                if (enemy == null) continue;
                enemy.OnEnemyDied -= HandleEnemyDied;
                Destroy(enemy.gameObject);
            }
            _activeEnemies.Clear();
            _spawnPointOccpancy.Clear();
            _enemyOriginPoint.Clear();
            Debug.Log("[EnemySpawner] Cleared all active enemies.");
        }
        public void PauseSpawning()
        {
            _paused = true;
            Debug.Log("[EnemySpawner] Spawning paused.");
        }
        public void ResumeSpawning()
        {
            _paused = false;
            Debug.Log("[EnemySpawner] Spawning resumed.");
        }
    }
}
