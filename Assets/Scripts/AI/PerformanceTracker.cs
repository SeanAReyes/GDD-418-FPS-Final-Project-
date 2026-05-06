using System.Collections;
using UnityEngine;
using FPS.Weapons;
using FPS.Player;
using FPS.Enemy;

namespace FPS.AI
{
    /// <summary>
    /// Collects raw combat statistics over a rolling time window and
    /// computes a normalised performance score (0 = struggling, 1 = dominating).
    /// Score factors: kill speed and per-window survival relative to current HP.
    /// </summary>
    public class PerformanceTracker : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("The player's GunController to track kills.")]
        public GunController gunController;

        [Tooltip("The player's health component to track damage taken.")]
        public PlayerHealth playerHealth;

        [Tooltip("The enemy spawner, used to subscribe to all spawned enemies.")]
        public EnemySpawner enemySpawner;

        [Header("Evaluation")]
        [Tooltip("How often (seconds) the performance score is recalculated.")]
        public float evaluationInterval = 10f;

        [Header("Kill Speed")]
        [Tooltip("Kill speed weight in the final score.")]
        [Range(0f, 1f)]
        public float killSpeedWeight = 0.5f;

        [Tooltip("Kills per interval considered 'perfect' kill speed. Scores above this are clamped to 1.")]
        public float targetKillsPerInterval = 3f;

        [Header("Survival")]
        [Tooltip("Survival weight in the final score.")]
        [Range(0f, 1f)]
        public float survivalWeight = 0.5f;

        // Raw stats accumulated since the last evaluation
        private int _kills;
        private float _damageTaken;

        // Health snapshot taken at the START of each window — survival ceiling resets each interval
        private float _healthAtWindowStart;

        // The most recently computed score — AI Director reads this
        private float _currentScore = 0.5f;

        // Event the AI Director subscribes to
        public event System.Action<float> OnScoreEvaluated;

        private void Start()
        {
            _healthAtWindowStart = playerHealth != null ? playerHealth.GetCurrentHealth() : 100f;

            SubscribeToGun();
            SubscribeToHealth();
            SubscribeToSpawner();
            StartCoroutine(EvaluationLoop());
        }

        private void SubscribeToGun()
        {
            if (gunController == null)
            {
                Debug.LogWarning("[PerformanceTracker] No GunController assigned.");
                return;
            }

            gunController.OnKill += () => _kills++;
        }

        private void SubscribeToHealth()
        {
            if (playerHealth == null)
            {
                Debug.LogWarning("[PerformanceTracker] No PlayerHealth assigned.");
                return;
            }

            playerHealth.OnDamageTaken += damage => _damageTaken += damage;
        }

        private void SubscribeToSpawner()
        {
            if (enemySpawner == null)
            {
                Debug.LogWarning("[PerformanceTracker] No EnemySpawner assigned.");
                return;
            }

            enemySpawner.OnEnemySpawned += enemy =>
            {
                if (enemy != null)
                    enemy.OnEnemyAttacked += OnEnemyAttackedPlayer;
            };
        }

        private void OnEnemyAttackedPlayer() { }

        private IEnumerator EvaluationLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(evaluationInterval);
                Evaluate();
            }
        }

        private void Evaluate()
        {
            // Kill speed: kills this window vs target kills per window, clamped to 1.
            float killSpeedScore = Mathf.Clamp01(_kills / Mathf.Max(targetKillsPerInterval, 1f));

            // Survival: damage taken this window vs health available at window start.
            // If no damage was taken, survival is perfect. Uses current window's HP ceiling.
            float windowCeiling = Mathf.Max(_healthAtWindowStart, 1f);
            float survivalScore = Mathf.Clamp01(1f - (_damageTaken / windowCeiling));

            _currentScore = (killSpeedScore * killSpeedWeight) + (survivalScore * survivalWeight);

            Debug.Log($"[PerformanceTracker] Score: {_currentScore:F2} " +
                      $"| Kills: {_kills} (speed score: {killSpeedScore:F2}) " +
                      $"| Damage taken: {_damageTaken}/{_healthAtWindowStart:F0} HP " +
                      $"(survival score: {survivalScore:F2})");

            OnScoreEvaluated?.Invoke(_currentScore);
            ResetStats();
        }

        private void ResetStats()
        {
            _kills       = 0;
            _damageTaken = 0f;

            // Snapshot current HP as the new ceiling for the next window
            _healthAtWindowStart = playerHealth != null ? playerHealth.GetCurrentHealth() : 100f;
        }

        /// <summary>Returns the most recent performance score (0–1).</summary>
        public float GetCurrentScore() => _currentScore;
    }
}
