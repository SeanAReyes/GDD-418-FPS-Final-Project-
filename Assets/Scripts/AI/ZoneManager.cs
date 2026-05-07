using System.Collections.Generic;
using UnityEngine;
using FPS.Enemy;

namespace FPS.AI
{
    /// <summary>
    /// Sequences the game's zones. Each zone has its own spawn points,
    /// obstacle lists, and a checkpoint that triggers the next zone.
    /// Communicates clear time to the AIDirector for difficulty scaling.
    /// </summary>
    public class ZoneManager : MonoBehaviour
    {
        [System.Serializable]
        public class ZoneData
        {
            [Tooltip("Display name for debugging.")]
            public string zoneName = "Zone";

            [Tooltip("Spawn points active during this zone.")]
            public Transform[] spawnPoints;

            [Tooltip("Obstacles the director can remove when player is dominating.")]
            public GameObject[] removableObstacles;

            [Tooltip("The checkpoint that ends this zone. Assign the ZoneCheckpoint component.")]
            public ZoneCheckpoint checkpoint;

            [Tooltip("How many enemies must die before the checkpoint becomes crossable.")]
            public int enemiesToClear = 5;
        }

        [Header("References")]
        public EnemySpawner enemySpawner;
        public AIDirector aiDirector;
        public EnvironmentManager environmentManager;

        [Header("Zones")]
        [Tooltip("Zones in sequence. Index 0 = starting zone.")]
        public List<ZoneData> zones = new List<ZoneData>();

        // Current active zone index
        private int _currentZoneIndex = -1;
        private int _enemiesKilledInZone;
        private float _zoneStartTime;
        private bool _zoneActive;
        

        private void Start()
        {
            if (zones.Count == 0)
            {
                Debug.LogWarning("[ZoneManager] No zones configured.");
                return;
            }

            AdvanceToZone(0);
        }

        private void AdvanceToZone(int index)
        {
            if (index >= zones.Count)
            {
                Debug.Log("[ZoneManager] All zones complete.");
                return;
            }

            if (enemySpawner != null)
                enemySpawner.OnEnemySpawned -= TrackEnemyForZone;
            
            enemySpawner?.PauseSpawning();
            enemySpawner?.ClearAllEnemies();

            _currentZoneIndex    = index;
            _enemiesKilledInZone = 0;
            _zoneStartTime       = Time.time;
            _zoneActive          = true;

            ZoneData zone = zones[index];
            Debug.Log($"[ZoneManager] Starting {zone.zoneName}.");

            // Update spawner with this zone's spawn points
            enemySpawner?.SetSpawnPoints(zone.spawnPoints);

            // Update environment manager with this zone's obstacle lists
            environmentManager?.SetZoneObstacles(zone.removableObstacles);

            aiDirector?.ApplyPendingEnvironmentPreset();

            enemySpawner?.ResumeSpawning();

            // Subscribe to checkpoint
            if (zone.checkpoint != null)
                zone.checkpoint.OnPlayerCrossed += HandleCheckpointCrossed;

            // Subscribe to enemy deaths for this zone
            if (enemySpawner != null)
                enemySpawner.OnEnemySpawned += TrackEnemyForZone;
        }

        private void TrackEnemyForZone(EnemyController enemy)
        {
            if (enemy == null) return;
            int spawnedInZone = _currentZoneIndex;
            enemy.OnEnemyDied += (e) => HandleEnemyDiedInZone(e, spawnedInZone);
        }

        private void HandleEnemyDiedInZone(EnemyController enemy, int spawnedInZone)
        {
            if (!_zoneActive) return;
            if (spawnedInZone != _currentZoneIndex) return;

            _enemiesKilledInZone++;
            Debug.Log($"[ZoneManager] Enemy killed in zone — {_enemiesKilledInZone}/" +
                      $"{zones[_currentZoneIndex].enemiesToClear}");
        }

        private void HandleCheckpointCrossed(int zoneIndex)
        {
            if (!_zoneActive) return;

            if(zoneIndex != _currentZoneIndex) return;

            ZoneData zone = zones[_currentZoneIndex];

            // Require minimum kills before checkpoint counts
            if (_enemiesKilledInZone < zone.enemiesToClear)
            {
                Debug.Log($"[ZoneManager] Checkpoint crossed but only " +
                          $"{_enemiesKilledInZone}/{zone.enemiesToClear} enemies cleared. " +
                          $"Keep fighting.");
                return;
            }

            CompleteCurrentZone();
        }

        private void CompleteCurrentZone()
        {
            _zoneActive = false;
            float clearTime = Time.time - _zoneStartTime;

            Debug.Log($"[ZoneManager] Zone {zones[_currentZoneIndex].zoneName} cleared " +
                      $"in {clearTime:F1}s.");

            // Unsubscribe checkpoint
            ZoneData zone = zones[_currentZoneIndex];
            if (zone.checkpoint != null)
                zone.checkpoint.OnPlayerCrossed -= HandleCheckpointCrossed;

            // Tell the director how fast this zone was cleared
            aiDirector?.OnZoneCompleted(clearTime);

            // Move to next zone
            AdvanceToZone(_currentZoneIndex + 1);
        }

        /// <summary>Returns the index of the currently active zone.</summary>
        public int GetCurrentZoneIndex() => _currentZoneIndex;
    }
}
