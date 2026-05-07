using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using FPS.Player;
using FPS.Enemy;

namespace FPS
{
    /// <summary>
    /// Handles game-level events: player death, scene restart.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [Header("References")]
        public PlayerHealth playerHealth;
        public EnemySpawner enemySpawner;

        [Tooltip("Seconds to wait after death before reloading the scene.")]
        public float restartDelay = 3f;

        private void OnEnable()
        {
            if (playerHealth != null)
                playerHealth.OnPlayerDied += HandlePlayerDied;
        }

        private void OnDisable()
        {
            if (playerHealth != null)
                playerHealth.OnPlayerDied -= HandlePlayerDied;
        }

        private void HandlePlayerDied()
        {
            Debug.Log("[GameManager] Player died — stopping spawner and restarting.");

            // Stop enemies from spawning and clear existing ones immediately
            enemySpawner?.PauseSpawning();
            enemySpawner?.ClearAllEnemies();

            StartCoroutine(RestartAfterDelay());
        }

        private IEnumerator RestartAfterDelay()
        {
            yield return new WaitForSeconds(restartDelay);
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
