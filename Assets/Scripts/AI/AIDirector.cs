using UnityEngine;
using FPS.Enemy;

namespace FPS.AI
{
    /// <summary>
    /// Reads PerformanceTracker score and adjusts enemy count,
    /// ranged enemy ratio, bullet speed, and environment layout.
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
        [Tooltip("Enemies added/removed per adjustment step.")]
        public int enemyAdjustmentStep = 1;

        [Header("Ranged Enemy Ratio")]
        [Tooltip("Ratio of ranged enemies at low performance (0 = none).")]
        [Range(0f, 1f)] public float rangedRatioLow    = 0f;

        [Tooltip("Ratio of ranged enemies at normal performance.")]
        [Range(0f, 1f)] public float rangedRatioNormal = 0.25f;

        [Tooltip("Ratio of ranged enemies at high performance.")]
        [Range(0f, 1f)] public float rangedRatioHigh   = 0.6f;

        [Header("Bullet Speed")]
        [Tooltip("Bullet speed when player is struggling.")]
        public float bulletSpeedLow    = 4f;

        [Tooltip("Bullet speed at normal performance.")]
        public float bulletSpeedNormal = 8f;

        [Tooltip("Bullet speed when player is dominating.")]
        public float bulletSpeedHigh   = 14f;

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

        private void HandleHighPerformance()
        {
            Debug.Log("[AIDirector] High performance — ramping up pressure.");
            enemySpawner?.IncreaseEnemyCount(enemyAdjustmentStep);
            enemySpawner?.SetRangedRatio(rangedRatioHigh);
            enemySpawner?.SetActiveBulletSpeed(bulletSpeedHigh);
            enemySpawner?.ForceAggroAll();
            environmentManager?.IncreaseObstacles();
        }

        private void HandleNormalPerformance()
        {
            Debug.Log("[AIDirector] Normal performance — holding steady.");
            enemySpawner?.SetRangedRatio(rangedRatioNormal);
            enemySpawner?.SetActiveBulletSpeed(bulletSpeedNormal);
        }

        private void HandleLowPerformance()
        {
            Debug.Log("[AIDirector] Low performance — easing pressure.");
            enemySpawner?.DecreaseEnemyCount(enemyAdjustmentStep);
            enemySpawner?.SetRangedRatio(rangedRatioLow);
            enemySpawner?.SetActiveBulletSpeed(bulletSpeedLow);
            environmentManager?.RemoveObstacles();
        }
    }
}
