using UnityEngine;
using FPS.Enemy;

namespace FPS.AI
{
    /// <summary>
    /// Reads PerformanceTracker score and zone clear times to adjust
    /// enemy count, ranged ratio, bullet speed, and environment layout.
    /// </summary>
    public class AIDirector : MonoBehaviour
    {
        [Header("References")]
        public PerformanceTracker performanceTracker;
        public EnemySpawner enemySpawner;
        public EnvironmentManager environmentManager;

        [Header("Thresholds")]
        [Range(0f, 1f)] public float highPerformanceThreshold = 0.7f;
        [Range(0f, 1f)] public float lowPerformanceThreshold  = 0.35f;

        [Header("Enemy Count")]
        public int enemyAdjustmentStep = 1;

        [Header("Ranged Enemy Ratio")]
        [Range(0f, 1f)] public float rangedRatioLow    = 0f;
        [Range(0f, 1f)] public float rangedRatioNormal = 0.25f;
        [Range(0f, 1f)] public float rangedRatioHigh   = 0.6f;

        [Header("Bullet Speed")]
        public float bulletSpeedLow    = 4f;
        public float bulletSpeedNormal = 8f;
        public float bulletSpeedHigh   = 14f;

        [Header("Enemy Aggro Range")]
        public float aggroRangeNormal = 20f;
        public float aggroRangeHigh = 35f;

        [Header("Enemy Attack Range (Ranged)")]
        public float attackRangeNormal = 15f;
        public float attackRangeHigh = 25f;

        [Header("Obstacles")]
        [Tooltip("Max obstacles left active in high performance mode.")]
        public int obstaclesHighMode = 3;

        [Header("Zone Difficulty")]
        [Tooltip("If a zone is cleared faster than this (seconds), next zone is harder.")]
        public float fastClearThreshold = 60f;

        [Tooltip("If a zone takes longer than this (seconds), next zone is easier.")]
        public float slowClearThreshold = 120f;

        [Tooltip("Enemy count added to next zone if cleared fast.")]
        public int zoneFastClearBonus = 2;

        [Tooltip("Enemy count removed from next zone if cleared slow.")]
        public int zoneSlowClearReduction = 1;

        private enum EnvironmentAction { None, Remove, Add }
        private EnvironmentAction _pendingEnvironmentAction = EnvironmentAction.None;

        private void OnEnable()
        {
            if (performanceTracker != null)
                performanceTracker.OnScoreEvaluated += OnScoreEvaluated;
        }

        private void OnDisable()
        {
            if (performanceTracker != null)
                performanceTracker.OnScoreEvaluated -= OnScoreEvaluated;
        }

        private void OnScoreEvaluated(float score)
        {
            Debug.Log($"[AIDirector] Score received: {score:F2}");

            if (score >= highPerformanceThreshold)
                HandleHighPerformance();
            else if (score <= lowPerformanceThreshold)
                HandleLowPerformance();
            else
                HandleNormalPerformance();
        }

        /// <summary>
        /// Called by ZoneManager when a zone is completed.
        /// Adjusts the next zone's difficulty based on clear speed.
        /// </summary>
        public void OnZoneCompleted(float clearTimeSeconds)
        {
            Debug.Log($"[AIDirector] Zone cleared in {clearTimeSeconds:F1}s.");

            if (clearTimeSeconds <= fastClearThreshold)
            {
                Debug.Log("[AIDirector] Fast clear — next zone is harder.");
                enemySpawner?.IncreaseEnemyCount(zoneFastClearBonus);
                _pendingEnvironmentAction = EnvironmentAction.Remove;
            }
            else if (clearTimeSeconds >= slowClearThreshold)
            {
                Debug.Log("[AIDirector] Slow clear — next zone is easier.");
                enemySpawner?.DecreaseEnemyCount(zoneSlowClearReduction);
                _pendingEnvironmentAction = EnvironmentAction.Add;
            }
            else
            {
                Debug.Log("[AIDirector] Normal clear — no zone difficulty change.");
                _pendingEnvironmentAction = EnvironmentAction.None;
            }
        }
        
        public void ApplyPendingEnvironmentPreset()
        {
            switch (_pendingEnvironmentAction)
            {
                case EnvironmentAction.Remove:
                    environmentManager?.RemoveObstaclesToLimit(obstaclesHighMode);
                    break;
                case EnvironmentAction.Add:
                    environmentManager?.IncreaseObstacles();
                    break;
            }
            _pendingEnvironmentAction = EnvironmentAction.None; // Reset after applying
        }

        private void HandleHighPerformance()
        {
            Debug.Log("[AIDirector] High performance — ramping up pressure.");
            enemySpawner?.IncreaseEnemyCount(enemyAdjustmentStep);
            enemySpawner?.SetRangedRatio(rangedRatioHigh);
            enemySpawner?.SetActiveBulletSpeed(bulletSpeedHigh);
            enemySpawner?.SetActiveAggroRange(aggroRangeHigh);
            enemySpawner?.SetActiveAttackRange(attackRangeHigh);
            enemySpawner?.ForceAggroAll();
            environmentManager?.RemoveObstaclesToLimit(obstaclesHighMode);
        }

        private void HandleNormalPerformance()
        {
            Debug.Log("[AIDirector] Normal performance — holding steady.");
            enemySpawner?.SetRangedRatio(rangedRatioNormal);
            enemySpawner?.SetActiveBulletSpeed(bulletSpeedNormal);
            enemySpawner?.SetActiveAggroRange(aggroRangeNormal);
            enemySpawner?.SetActiveAttackRange(attackRangeNormal);
        }

        private void HandleLowPerformance()
        {
            Debug.Log("[AIDirector] Low performance — easing pressure.");
            enemySpawner?.DecreaseEnemyCount(enemyAdjustmentStep);
            enemySpawner?.SetRangedRatio(rangedRatioLow);
            enemySpawner?.SetActiveBulletSpeed(bulletSpeedLow);
        }
    }
}
